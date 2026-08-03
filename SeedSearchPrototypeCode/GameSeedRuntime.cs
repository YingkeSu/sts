using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

namespace SeedSearchPrototype;

/// <summary>
/// A safe, single-seed adapter over the game's own run-start models. It is used
/// by Inspect, not by the bulk search worker: STS2's model graph is main-thread
/// owned and must not be constructed from Task.Run.
/// </summary>
public static class GameSeedRuntimeBackend
{
    public static bool TryInspect(
        string seed,
        SeedBranch branch,
        RunCharacter character,
        int ascension,
        string context,
        out SeedSnapshot snapshot,
        out string reason)
    {
        snapshot = null!;
        reason = string.Empty;
        try
        {
            var reference = new SeedSearchEngine().Inspect(seed, branch, context);
            var unlocks = GetAllUnlocks();
            var characterModel = PickCharacter(character);
            var player = Player.CreateForNewRun(characterModel, unlocks, 1);
            var acts = ActModel.GetDefaultList().Select(act => act.ToMutable()).ToList();
            var run = RunState.CreateForNewRun(
                new[] { player },
                acts,
                Array.Empty<ModifierModel>(),
                GameMode.Standard,
                ascension,
                seed);

            run.Act.GenerateRooms(run.Rng.UpFront, unlocks, false);
            var map = StandardActMap.CreateFor(run, false);
            var points = map.GetAllMapPoints().ToArray();
            var counts = points
                .Select(ReadPointType)
                .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
            var boss = NormalizeModelId(ReadModelId(run.Act.BossEncounter));
            var mapId = ReadMapId(run.Act);
            var eliteCount = GetCount(counts, "Elite");
            var shopCount = GetCount(counts, "Shop");
            var restSiteCount = GetCount(counts, "RestSite");
            var mapSummary = $"{(mapId == "1" ? "Underdocks" : "Overgrowth")} · {points.Length} nodes · " +
                             $"{eliteCount}E / {shopCount}$ / {restSiteCount}R";
            var ancient = "Neow";
            snapshot = reference with
            {
                Backend = "game-runtime",
                Act1Map = mapSummary,
                Act1MapId = mapId,
                Bosses = boss,
                Boss1Id = boss,
                Ancients = ancient,
                EliteCount = eliteCount,
                ShopCount = shopCount,
                RestSiteCount = restSiteCount,
            };
            return true;
        }
        catch (Exception exception)
        {
            reason = exception.GetBaseException().Message;
            return false;
        }
    }

    private static CharacterModel PickCharacter(RunCharacter character)
    {
        var wanted = character.ToString().ToLowerInvariant();
        return ModelDb.AllCharacters.FirstOrDefault(model =>
                   NormalizeModelId(model.Id.ToString()).Equals(wanted, StringComparison.OrdinalIgnoreCase)) ??
               ModelDb.AllCharacters.First();
    }

    private static UnlockState GetAllUnlocks()
    {
        var field = typeof(UnlockState).GetField("all", BindingFlags.NonPublic | BindingFlags.Static);
        return field?.GetValue(null) as UnlockState ??
            new UnlockState(Array.Empty<string>(), Array.Empty<ModelId>(), 999);
    }

    private static string ReadPointType(object point)
    {
        var value = ReadMember(point, "PointType") ?? ReadMember(point, "Type");
        return value?.ToString() ?? "Unknown";
    }

    private static string ReadMapId(object act)
    {
        var path = ReadMember(act, "MapTopBgPath")?.ToString() ??
                   ReadMember(act, "MapMidBgPath")?.ToString() ??
                   string.Empty;
        return path.Contains("underdock", StringComparison.OrdinalIgnoreCase) ? "1" : "0";
    }

    private static string ReadModelId(object? model)
    {
        if (model == null)
        {
            return string.Empty;
        }

        return ReadMember(model, "Id")?.ToString() ?? model.ToString() ?? string.Empty;
    }

    private static object? ReadMember(object target, string name)
    {
        var type = target.GetType();
        var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property != null)
        {
            return property.GetValue(target);
        }

        var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(target);
    }

    private static string NormalizeModelId(string value)
    {
        var normalized = value.ToLowerInvariant();
        foreach (var prefix in new[] { "encounter.", "event.", "character.", "boss" })
        {
            normalized = normalized.Replace(prefix, string.Empty, StringComparison.Ordinal);
        }

        return normalized.Replace("_boss", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);
    }

    private static int GetCount(IReadOnlyDictionary<string, int> counts, string type) =>
        counts.TryGetValue(type, out var count) ? count : 0;
}
