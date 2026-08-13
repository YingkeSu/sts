using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace SeedSearchPrototype;

/// <summary>
/// Resolves card and character artwork through the game's own model and
/// resource APIs, so previews stay in sync with the installed resource pack
/// instead of shipping duplicate images inside the mod.
/// </summary>
public static class GameArtPreview
{
    private static readonly Dictionary<string, CardModel?> CardLookup = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, RelicModel?> RelicLookup = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, EncounterModel?> EncounterLookup = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, AncientEventModel?> AncientLookup = new(StringComparer.Ordinal);
    private static IReadOnlyList<CardModel>? _cards;
    private static IReadOnlyList<CharacterModel>? _characters;
    private static IReadOnlyList<RelicModel>? _relics;
    private static IReadOnlyList<EncounterModel>? _encounters;
    private static IReadOnlyList<AncientEventModel>? _ancients;

    public static Texture2D? OptionArt(SearchTheSpireOption option) =>
        OptionArt(option.Id, option.OwnerCharacter, option.Section);

    public static Texture2D? OptionArt(string optionId, string? ownerCharacter, string section) =>
        OptionArtRouter.For(section) switch
        {
            OptionArtKind.Card => CardPortrait(optionId, ownerCharacter, section),
            OptionArtKind.Relic => RelicIcon(optionId),
            OptionArtKind.Boss => BossIcon(optionId),
            _ => null,
        };

    public static Texture2D? CardPortrait(string optionId, string? ownerCharacter, string section)
    {
        if (!section.Contains("card", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var card = FindCard(optionId, ownerCharacter, section);
        if (card == null)
        {
            return null;
        }

        try
        {
            return card.Portrait;
        }
        catch (Exception exception)
        {
            MainFile.Logger.Warn($"Could not load card portrait for {optionId}: {exception.Message}");
            return null;
        }
    }

    public static Texture2D? CharacterPortrait(RunCharacter character)
    {
        var model = FindCharacter(character);
        if (model == null)
        {
            return null;
        }

        try
        {
            return model.CharacterSelectIcon;
        }
        catch (Exception exception)
        {
            MainFile.Logger.Warn($"Could not load character portrait for {character}: {exception.Message}");
            return null;
        }
    }

    public static Texture2D? CharacterIcon(RunCharacter character)
    {
        var model = FindCharacter(character);
        if (model == null)
        {
            return null;
        }

        try
        {
            return model.IconTexture;
        }
        catch (Exception exception)
        {
            MainFile.Logger.Warn($"Could not load character icon for {character}: {exception.Message}");
            return null;
        }
    }

    public static Texture2D? RelicIcon(string optionId)
    {
        var relic = FindRelic(optionId);
        if (relic == null)
        {
            return null;
        }

        try
        {
            var big = relic.BigIcon;
            if (big != null)
            {
                return big;
            }
        }
        catch (Exception exception)
        {
            MainFile.Logger.Warn($"Could not load big relic icon for {optionId}: {exception.Message}");
        }

        try
        {
            return relic.Icon;
        }
        catch (Exception exception)
        {
            MainFile.Logger.Warn($"Could not load relic icon for {optionId}: {exception.Message}");
            return null;
        }
    }

    public static Texture2D? BossIcon(string optionId)
    {
        var encounter = FindBossEncounter(optionId);
        if (encounter == null)
        {
            return null;
        }

        try
        {
            var path = ImageHelper.GetRoomIconPath(MapPointType.Boss, RoomType.Boss, encounter.Id);
            if (string.IsNullOrWhiteSpace(path) || !ResourceLoader.Exists(path))
            {
                return null;
            }

            return ResourceLoader.Load<Texture2D>(path);
        }
        catch (Exception exception)
        {
            MainFile.Logger.Warn($"Could not load boss icon for {optionId}: {exception.Message}");
            return null;
        }
    }

    /// <summary>
    /// The game's own Act 1 map node icons. Regular points use
    /// NNormalMapPoint.IconName paths; boss and ancient nodes use the rolled
    /// EncounterModel.BossNodePath / AncientEventModel.MapIcon assets exactly
    /// like NBossMapPoint/NAncientMapPoint do in-game.
    /// </summary>
    public static Texture2D? MapNodeIcon(string kind, string? bossId = "", string? ancientId = "")
    {
        if (kind == "boss")
        {
            return BossMapNodeIcon(bossId);
        }

        if (kind == "ancient")
        {
            return AncientMapNodeIcon(ancientId);
        }

        var name = kind switch
        {
            "monster" => "map_monster",
            "elite" => "map_elite",
            "rest" => "map_rest",
            "shop" => "map_shop",
            "treasure" => "map_chest",
            "unknown" => "map_unknown",
            _ => null,
        };
        return name == null ? null : LoadMapIcon(name);
    }

    private static Texture2D? BossMapNodeIcon(string? bossId)
    {
        if (string.IsNullOrWhiteSpace(bossId))
        {
            return null;
        }

        var encounter = FindBossEncounter(bossId);
        if (encounter == null)
        {
            return null;
        }

        try
        {
            // NBossMapPoint uses the BossNodePath asset: the skeleton resource
            // when it exists, otherwise the static node PNG placeholder.
            if (encounter.BossNodeSpineResource != null)
            {
                return BossIcon(bossId);
            }

            var path = encounter.BossNodePath + ".png";
            return LoadTexture(path) ?? BossIcon(bossId);
        }
        catch (Exception exception)
        {
            MainFile.Logger.Warn($"Could not load boss map node icon for {bossId}: {exception.Message}");
            return BossIcon(bossId);
        }
    }

    private static Texture2D? AncientMapNodeIcon(string? ancientId)
    {
        if (string.IsNullOrWhiteSpace(ancientId))
        {
            return null;
        }

        var ancient = FindAncient(ancientId);
        if (ancient == null)
        {
            return null;
        }

        try
        {
            var path = ImageHelper.GetImagePath(
                "packed/map/ancients/ancient_node_" +
                ancient.Id.Entry.ToLowerInvariant() +
                ".png");
            return LoadTexture(path) ?? AncientIcon(ancient);
        }
        catch (Exception exception)
        {
            MainFile.Logger.Warn($"Could not load ancient map node icon for {ancientId}: {exception.Message}");
            return AncientIcon(ancient);
        }
    }

    private static Texture2D? AncientIcon(AncientEventModel ancient)
    {
        try
        {
            var path = ImageHelper.GetRoomIconPath(
                MapPointType.Ancient,
                RoomType.Event,
                ancient.Id);
            if (string.IsNullOrWhiteSpace(path) || !ResourceLoader.Exists(path))
            {
                return null;
            }

            return ResourceLoader.Load<Texture2D>(path);
        }
        catch (Exception exception)
        {
            MainFile.Logger.Warn($"Could not load ancient icon for {ancient.Id.Entry}: {exception.Message}");
            return null;
        }
    }

    private static Texture2D? LoadMapIcon(string name)
    {
        var path = $"res://images/atlases/ui_atlas.sprites/map/icons/{name}.tres";
        return LoadTexture(path);
    }

    private static Texture2D? LoadTexture(string path)
    {
        return ResourceLoader.Exists(path)
            ? ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse)
            : null;
    }

    private static CardModel? FindCard(string optionId, string? ownerCharacter, string section)
    {
        var optionKey = Normalize(optionId);
        var ownerKey = Normalize(OwnerName(ownerCharacter, section));
        var cacheKey = $"{optionKey}|{ownerKey}";
        if (CardLookup.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        CardModel? match = null;
        var candidates = new List<CardModel>();
        foreach (var card in AllCards())
        {
            var entry = Normalize(card.Id.Entry);
            if (entry == optionKey ||
                (ownerKey.Length > 0 && (entry == ownerKey + optionKey || entry == optionKey + ownerKey)))
            {
                candidates.Add(card);
            }
        }

        if (candidates.Count > 0)
        {
            match = PreferPoolOwner(candidates, ownerKey);
        }

        CardLookup[cacheKey] = match;
        return match;
    }

    private static RelicModel? FindRelic(string optionId)
    {
        var optionKey = Normalize(optionId);
        if (RelicLookup.TryGetValue(optionKey, out var cached))
        {
            return cached;
        }

        RelicModel? match = null;
        foreach (var relic in AllRelics())
        {
            if (Normalize(relic.Id.Entry) == optionKey)
            {
                match = relic;
                break;
            }
        }

        RelicLookup[optionKey] = match;
        return match;
    }

    private static EncounterModel? FindBossEncounter(string optionId)
    {
        var optionKey = Normalize(optionId);
        if (EncounterLookup.TryGetValue(optionKey, out var cached))
        {
            return cached;
        }

        EncounterModel? match = null;
        foreach (var encounter in AllEncounters())
        {
            if (encounter.RoomType != RoomType.Boss)
            {
                continue;
            }

            var entry = Normalize(encounter.Id.Entry);
            if (entry == optionKey)
            {
                match = encounter;
                break;
            }

            if (entry == optionKey + "boss")
            {
                match ??= encounter;
            }
        }

        EncounterLookup[optionKey] = match;
        return match;
    }

    private static AncientEventModel? FindAncient(string optionId)
    {
        var optionKey = Normalize(optionId);
        if (AncientLookup.TryGetValue(optionKey, out var cached))
        {
            return cached;
        }

        AncientEventModel? match = null;
        foreach (var ancient in AllAncients())
        {
            if (Normalize(ancient.Id.Entry) == optionKey)
            {
                match = ancient;
                break;
            }
        }

        AncientLookup[optionKey] = match;
        return match;
    }

    private static CardModel? PreferPoolOwner(IReadOnlyList<CardModel> candidates, string ownerKey)
    {
        if (ownerKey.Length == 0)
        {
            return candidates[0];
        }

        foreach (var candidate in candidates)
        {
            try
            {
                if (Normalize(candidate.Pool.Title) == ownerKey)
                {
                    return candidate;
                }
            }
            catch (Exception)
            {
                // A model without a readable pool should not break the preview.
            }
        }

        return candidates[0];
    }

    private static CharacterModel? FindCharacter(RunCharacter character)
    {
        if (character == RunCharacter.Any)
        {
            return null;
        }

        var wanted = Normalize(character.ToString());
        return Characters().FirstOrDefault(model => Normalize(model.Id.Entry) == wanted);
    }

    private static string OwnerName(string? ownerCharacter, string section)
    {
        if (!string.IsNullOrWhiteSpace(ownerCharacter))
        {
            return ownerCharacter;
        }

        foreach (var character in Enum.GetNames<RunCharacter>())
        {
            if (section.Contains(character, StringComparison.OrdinalIgnoreCase))
            {
                return character;
            }
        }

        return string.Empty;
    }

    private static IReadOnlyList<CardModel> AllCards()
    {
        if (_cards == null)
        {
            try
            {
                _cards = ModelDb.AllCards.Distinct().ToList();
            }
            catch (Exception exception)
            {
                MainFile.Logger.Warn($"Card preview catalog unavailable: {exception.Message}");
                _cards = Array.Empty<CardModel>();
            }
        }

        return _cards;
    }

    private static IReadOnlyList<RelicModel> AllRelics()
    {
        if (_relics == null)
        {
            try
            {
                _relics = ModelDb.AllRelics.Distinct().ToList();
            }
            catch (Exception exception)
            {
                MainFile.Logger.Warn($"Relic preview catalog unavailable: {exception.Message}");
                _relics = Array.Empty<RelicModel>();
            }
        }

        return _relics;
    }

    private static IReadOnlyList<EncounterModel> AllEncounters()
    {
        if (_encounters == null)
        {
            try
            {
                _encounters = ModelDb.AllEncounters.Distinct().ToList();
            }
            catch (Exception exception)
            {
                MainFile.Logger.Warn($"Encounter preview catalog unavailable: {exception.Message}");
                _encounters = Array.Empty<EncounterModel>();
            }
        }

        return _encounters;
    }

    private static IReadOnlyList<AncientEventModel> AllAncients()
    {
        if (_ancients == null)
        {
            try
            {
                _ancients = ModelDb.AllAncients.Distinct().ToList();
            }
            catch (Exception exception)
            {
                MainFile.Logger.Warn($"Ancient preview catalog unavailable: {exception.Message}");
                _ancients = Array.Empty<AncientEventModel>();
            }
        }

        return _ancients;
    }

    private static IReadOnlyList<CharacterModel> Characters()
    {
        if (_characters == null)
        {
            try
            {
                _characters = ModelDb.AllCharacters.Distinct().ToList();
            }
            catch (Exception exception)
            {
                MainFile.Logger.Warn($"Character preview catalog unavailable: {exception.Message}");
                _characters = Array.Empty<CharacterModel>();
            }
        }

        return _characters;
    }

    private static string Normalize(string value)
    {
        var buffer = new char[value.Length];
        var length = 0;
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                buffer[length++] = char.ToLowerInvariant(character);
            }
        }

        return new string(buffer, 0, length);
    }
}
