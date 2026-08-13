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
            var boss = NormalizeModelId(ReadModelId(run.Act.BossEncounter));
            var ancient = NormalizeModelId(ReadModelId(run.Act.Ancient));
            var mapId = ReadMapId(run.Act);
            var layout = BuildMapLayout(map, boss, ancient);
            var eliteCount = layout.Nodes.Count(node => node.Kind == "elite");
            var shopCount = layout.Nodes.Count(node => node.Kind == "shop");
            var restSiteCount = layout.Nodes.Count(node => node.Kind == "rest");
            var mapSummary = $"{(mapId == "1" ? "Underdocks" : "Overgrowth")} · {layout.Nodes.Count} nodes · " +
                             $"{eliteCount}E / {shopCount}$ / {restSiteCount}R";
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
                Map = layout,
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

    private static MapLayout BuildMapLayout(StandardActMap map, string bossId, string ancientId)
    {
        var nodes = new List<MapNode>();
        var indices = new Dictionary<MapPoint, int>();
        var allPoints = new List<MapPoint>();

        void AddPoint(MapPoint point)
        {
            if (indices.ContainsKey(point))
            {
                return;
            }

            indices[point] = allPoints.Count;
            allPoints.Add(point);
            nodes.Add(new MapNode(point.coord.col, point.coord.row, MapKind(point.PointType)));
        }

        // StandardActMap keeps Ancient/Boss outside the grid. SearchTheSpire's
        // preview includes both, so the layout is the grid plus those two.
        AddPoint(map.StartingMapPoint);
        foreach (var point in map.GetAllMapPoints())
        {
            AddPoint(point);
        }

        AddPoint(map.BossMapPoint);
        if (map.SecondBossMapPoint != null)
        {
            AddPoint(map.SecondBossMapPoint);
        }

        var edges = new List<MapEdge>();
        foreach (var point in allPoints)
        {
            foreach (var child in point.Children)
            {
                if (indices.TryGetValue(child, out var childIndex))
                {
                    edges.Add(new MapEdge(indices[point], childIndex));
                }
            }
        }

        edges.Sort((first, second) => first.From != second.From
            ? first.From.CompareTo(second.From)
            : first.To.CompareTo(second.To));
        return new MapLayout(nodes, edges, bossId, ancientId);
    }

    private static string MapKind(MapPointType type) => type switch
    {
        MapPointType.Monster => "monster",
        MapPointType.Elite => "elite",
        MapPointType.RestSite => "rest",
        MapPointType.Shop => "shop",
        MapPointType.Treasure => "treasure",
        MapPointType.Boss => "boss",
        MapPointType.Ancient => "ancient",
        _ => "unknown",
    };

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

}
