namespace SeedSearchPrototype;

public enum SearchTheSpireSlotKind
{
    Picker,
    Select,
}

public sealed record SearchTheSpireSlot(
    string Id,
    string Label,
    string? ParentId = null,
    string? Cluster = null,
    SearchTheSpireSlotKind Kind = SearchTheSpireSlotKind.Picker,
    bool RequiresCharacter = false,
    int RequiresAscension = 0);

public sealed record SearchTheSpireOption(
    string Id,
    string Title,
    string Section,
    string? Description = null,
    bool Blocked = false,
    string? BlockReason = null,
    string? SpecKey = null,
    bool IsConditional = false,
    string? OwnerCharacter = null,
    string? Rarity = null);

public sealed record SearchTheSpireSlotView(
    SearchTheSpireSlot Slot,
    IReadOnlyList<SearchTheSpireOption> Options,
    bool RequiresCharacter);

/// <summary>
/// The board state is kept separate from Godot. That makes hidden child slots,
/// blocked picker entries, and share-spec compilation testable without loading
/// the game.
/// </summary>
public sealed class SearchTheSpireBoardState
{
    private readonly IReadOnlyDictionary<string, string?> _values;

    private SearchTheSpireBoardState(
        RunCharacter character,
        int ascension,
        IReadOnlyDictionary<string, string?> values)
    {
        Character = character;
        Ascension = Math.Clamp(ascension, 0, SearchTheSpireUiLayout.MaxAscension);
        _values = values;
    }

    public static SearchTheSpireBoardState Empty { get; } =
        new(RunCharacter.Any, 0, new Dictionary<string, string?>());

    /// <summary>
    /// Restore the board portion of a saved SearchTheSpire query. The public
    /// query keeps character/ascension beside the comma-separated extended
    /// spec, so callers pass those two values explicitly and this method only
    /// decodes the board selections.
    /// </summary>
    public static SearchTheSpireBoardState FromSpec(
        RunCharacter character,
        int ascension,
        string? spec)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        var counters = new Dictionary<string, int>(StringComparer.Ordinal);

        void Put(string slotId, string? value)
        {
            if (!SearchTheSpireCatalog.ContainsSlot(slotId) || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            values[slotId] = value.Trim().ToLowerInvariant();
        }

        void PutGroup(string prefix, string raw, int count)
        {
            var parts = raw.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (var index = 0; index < parts.Length && index < count; index++)
            {
                Put($"{prefix}{index + 1}", parts[index]);
            }
        }

        void PutRepeated(string prefix, string value, int limit)
        {
            var index = counters.TryGetValue(prefix, out var current) ? current : 0;
            if (index < limit)
            {
                Put($"{prefix}{index + 1}", value);
                counters[prefix] = index + 1;
            }
        }

        void PutNamedGroup(IReadOnlyList<string> slotIds, string raw)
        {
            var parts = raw.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (var index = 0; index < parts.Length && index < slotIds.Count; index++)
            {
                Put(slotIds[index], parts[index]);
            }
        }

        if (!string.IsNullOrWhiteSpace(spec))
        {
            foreach (var fragment in spec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var separator = fragment.IndexOf('=');
                var key = (separator >= 0 ? fragment[..separator] : fragment).Trim().ToLowerInvariant();
                var value = separator >= 0 ? fragment[(separator + 1)..].Trim() : string.Empty;
                if (key.Length == 0)
                {
                    continue;
                }

                switch (key)
                {
                    case "char":
                    case "scarcity":
                        // These are represented by SeedQuery.Character and
                        // SeedQuery.Ascension, not by a child picker.
                        break;
                    case "neow":
                        if (int.TryParse(value, out var cursedIndex) &&
                            cursedIndex >= 0 && cursedIndex < SearchTheSpireCatalog.CursedOffers.Length)
                        {
                            Put("neowOffer", SearchTheSpireCatalog.CursedOffers[cursedIndex]);
                        }

                        break;
                    case "bonus":
                        Put("neowOffer", value);
                        break;
                    case "bones_relic":
                        PutNamedGroup(new[] { "bonesGrantA", "bonesGrantB" }, value);
                        break;
                    case "reward_within":
                    case "shop_within":
                    case "bag_within":
                    case "event_within":
                        Put(ToBoardSlotId(key), value);
                        break;
                    case "reward_cards":
                        PutGroup("rewardPick", value, SearchTheSpireCatalog.RewardPickIds.Count);
                        break;
                    case "reward_ordered":
                        Put("rewardOrdered", "true");
                        break;
                    case "bones_capsule_set":
                        PutGroup("bonesCapsuleSet", value, 3);
                        break;
                    case "bones_kaleido_distinct":
                        PutGroup("bonesKaleidoCard", value, 2);
                        break;
                    case "bones_scrollbox_contains":
                        PutGroup("bonesScrollboxCard", value, 3);
                        break;
                    case "bones_poultice_set":
                        PutGroup("bonesPoulticeCard", value, 2);
                        break;
                    case "large_relic":
                        PutNamedGroup(new[] { "largeRelicA", "largeRelicB" }, value);
                        break;
                    case "poultice_set":
                        PutGroup("poulticeCard", value, 2);
                        break;
                    case "kaleido_distinct":
                        PutGroup("kaleidoCard", value, 2);
                        break;
                    case "scrollbox_contains":
                        PutGroup("scrollboxCard", value, 3);
                        break;
                    case "phial_potion":
                        PutNamedGroup(new[] { "phialPotionA", "phialPotionB" }, value);
                        break;
                    case "shop_relic":
                        PutRepeated("shopPick", value, SearchTheSpireCatalog.ShopPickIds.Count);
                        break;
                    case "bag_relic":
                        PutRepeated("bagPick", value, SearchTheSpireCatalog.BagPickIds.Count);
                        break;
                    default:
                        if (key.StartsWith("event_in", StringComparison.Ordinal) &&
                            int.TryParse(key["event_in".Length..], out var eventWindow))
                        {
                            Put("eventWithin", eventWindow.ToString());
                            PutRepeated("eventPick", value, SearchTheSpireCatalog.EventPickIds.Count);
                        }
                        else if (key.StartsWith("reward", StringComparison.Ordinal) && key.EndsWith("_card", StringComparison.Ordinal) &&
                                 int.TryParse(key["reward".Length..^"_card".Length], out var rewardIndex) &&
                                 rewardIndex is >= 1 and <= 3)
                        {
                            Put($"reward{rewardIndex}", value);
                        }
                        else if (key is "ancient2_offers" or "ancient2_offers_if")
                        {
                            Put("ancient2Offers", value);
                        }
                        else if (key is "ancient3_offers" or "ancient3_offers_if")
                        {
                            Put("ancient3Offers", value);
                        }
                        else if (key is "tablet_card" or "arcane_card" or "coffer_card" or "coffer_potion" or
                                 "newleaf_card" or "paperweight_card" or "bones_tablet_card" or "bones_arcane_card" or
                                 "bones_coffer_card" or "bones_coffer_potion" or "bones_newleaf_card" or "bones_paperweight_card" or
                                 "bones_curse" or "capsule_relic")
                        {
                            Put(ToBoardSlotId(key), value);
                        }
                        else if (SearchTheSpireCatalog.ContainsSlot(key))
                        {
                            Put(key, value);
                        }

                        break;
                }
            }
        }

        return new SearchTheSpireBoardState(character, ascension, values);
    }

    private static string ToBoardSlotId(string specKey) => specKey switch
    {
        "reward_within" => "rewardWithin",
        "shop_within" => "shopWithin",
        "bag_within" => "bagWithin",
        "event_within" => "eventWithin",
        "large_relic" => "largeRelicA",
        "tablet_card" => "tabletCard",
        "arcane_card" => "arcaneCard",
        "coffer_card" => "cofferCard",
        "coffer_potion" => "cofferPotion",
        "newleaf_card" => "newleafCard",
        "paperweight_card" => "paperweightCard",
        "bones_tablet_card" => "bonesTabletCard",
        "bones_arcane_card" => "bonesArcaneCard",
        "bones_coffer_card" => "bonesCofferCard",
        "bones_coffer_potion" => "bonesCofferPotion",
        "bones_newleaf_card" => "bonesNewleafCard",
        "bones_paperweight_card" => "bonesPaperweightCard",
        "bones_curse" => "bonesCurse",
        "capsule_relic" => "capsuleRelic",
        _ => specKey,
    };

    public RunCharacter Character { get; }

    public int Ascension { get; }

    public string? Selected(string slotId) =>
        _values.TryGetValue(slotId, out var value) ? value : null;

    public SearchTheSpireBoardState WithCharacter(RunCharacter character)
    {
        var nextValues = new Dictionary<string, string?>(_values);
        var next = new SearchTheSpireBoardState(character, Ascension, nextValues);

        foreach (var slot in SearchTheSpireCatalog.Slots.Where(SearchTheSpireCatalog.IsCharacterScoped))
        {
            if (!nextValues.TryGetValue(slot.Id, out var value) || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            // A character-specific value is not a valid constraint when the
            // character is cleared. For a named character, validate against
            // the new pool so switching Ironclad → Silent cannot leave an
            // impossible relic/card pin in the share spec.
            var valid = character == RunCharacter.Any
                ? SearchTheSpireCatalog.IsSharedCharacterlessValue(slot, value)
                : SearchTheSpireCatalog.OptionsFor(slot, next)
                    .Any(option => option.Id == value && !option.Blocked);
            if (!valid)
            {
                nextValues.Remove(slot.Id);
            }
        }

        return next;
    }

    public SearchTheSpireBoardState WithAscension(int ascension) =>
        new(Character, ascension, new Dictionary<string, string?>(_values));

    public SearchTheSpireBoardState Select(string slotId, string? value)
    {
        if (!SearchTheSpireCatalog.ContainsSlot(slotId))
        {
            throw new ArgumentException($"Unknown SearchTheSpire slot '{slotId}'.", nameof(slotId));
        }

        var slot = SearchTheSpireCatalog.GetSlot(slotId);
        var next = new Dictionary<string, string?>(_values)
        {
            [slotId] = string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant(),
        };
        var state = new SearchTheSpireBoardState(Character, Ascension, next);
        if (state.Character == RunCharacter.Any &&
            SearchTheSpireCatalog.IsCharacterScoped(slot) &&
            SearchTheSpireCatalog.TryOwnerCharacter(slot, state.Selected(slotId)) is { } owner)
        {
            state = state.WithCharacter(owner);
            // WithCharacter validates existing character-scoped values before
            // the direct selection is visible to it; restore the just-picked
            // value after inferring the owner.
            var inferredValues = new Dictionary<string, string?>(_values)
            {
                [slotId] = state.Selected(slotId),
            };
            state = new SearchTheSpireBoardState(owner, Ascension, inferredValues);
        }

        return state.RaisePackageWindow(slotId);
    }

    private SearchTheSpireBoardState RaisePackageWindow(string changedSlot)
    {
        var windowKey = changedSlot.StartsWith("rewardPick", StringComparison.Ordinal)
            ? "rewardWithin"
            : changedSlot.StartsWith("shopPick", StringComparison.Ordinal)
                ? "shopWithin"
                : changedSlot.StartsWith("eventPick", StringComparison.Ordinal)
                    ? "eventWithin"
                    : changedSlot.StartsWith("bagPick", StringComparison.Ordinal)
                        ? "bagWithin"
                        : null;
        if (windowKey == null || Selected(changedSlot) == null)
        {
            return this;
        }

        var current = int.TryParse(Selected(windowKey), out var parsed) ? parsed : 1;
        var floor = windowKey switch
        {
            "rewardWithin" => SearchTheSpireCatalog.RewardPackageFloor(this),
            "shopWithin" => SearchTheSpireCatalog.ShopFloor(this),
            "eventWithin" => SearchTheSpireCatalog.EventWindowTarget(this),
            "bagWithin" => SearchTheSpireCatalog.BagFloor(this),
            _ => 1,
        };
        if (current >= floor)
        {
            return this;
        }

        var next = new Dictionary<string, string?>(_values)
        {
            [windowKey] = floor.ToString(),
        };
        return new SearchTheSpireBoardState(Character, Ascension, next);
    }

    public IReadOnlyList<SearchTheSpireSlot> VisibleChildren(string parentId) =>
        SearchTheSpireCatalog.Slots
            .Where(slot => slot.ParentId == parentId && SearchTheSpireCatalog.IsEnabled(slot, this))
            .ToArray();

    public SearchTheSpireSlotView DescribeSlot(string slotId)
    {
        var slot = SearchTheSpireCatalog.GetSlot(slotId);
        return new SearchTheSpireSlotView(
            slot,
            SearchTheSpireCatalog.OptionsFor(slot, this),
            slot.RequiresCharacter && Character == RunCharacter.Any);
    }

    public IReadOnlyList<SearchTheSpireOption> OptionsFor(string slotId) =>
        DescribeSlot(slotId).Options;

    /// <summary>
    /// Compile the same public key vocabulary used by SearchTheSpire. Joint
    /// packages deliberately become one fragment, while repeated shop/bag/event
    /// picks retain one fragment per selected slot.
    /// </summary>
    public string ToSpec()
    {
        var fragments = new List<string>();
        if (Character != RunCharacter.Any)
        {
            fragments.Add($"char={Character.ToString().ToLowerInvariant()}");
        }

        AddNeowFragment(fragments);
        AddJointFragment(fragments, "bones_relic", "bonesGrantA", "bonesGrantB");
        AddRewardPackageFragment(fragments);
        AddJointFragment(fragments, "bones_capsule_set", "bonesCapsuleSet1", "bonesCapsuleSet2", "bonesCapsuleSet3");
        AddJointFragment(fragments, "bones_kaleido_distinct", "bonesKaleidoCard1", "bonesKaleidoCard2");
        AddJointFragment(fragments, "bones_scrollbox_contains", "bonesScrollboxCard1", "bonesScrollboxCard2", "bonesScrollboxCard3");
        AddJointFragment(fragments, "bones_poultice_set", "bonesPoulticeCard1", "bonesPoulticeCard2");
        AddJointFragment(fragments, "large_relic", "largeRelicA", "largeRelicB");
        AddJointFragment(fragments, "poultice_set", "poulticeCard1", "poulticeCard2");
        AddJointFragment(fragments, "kaleido_distinct", "kaleidoCard1", "kaleidoCard2");
        AddJointFragment(fragments, "scrollbox_contains", "scrollboxCard1", "scrollboxCard2", "scrollboxCard3");
        AddJointFragment(fragments, "phial_potion", "phialPotionA", "phialPotionB");

        var shopPickCount = SearchTheSpireCatalog.ShopPickIds.Count(id => Selected(id) != null);
        if (shopPickCount > 0)
        {
            fragments.Add($"shop_within={Math.Max(ReadWindow("shopWithin"), SearchTheSpireCatalog.ShopFloor(this))}");
        }

        var bagPickCount = SearchTheSpireCatalog.BagPickIds.Count(id => Selected(id) != null);
        if (bagPickCount > 0 && Character != RunCharacter.Any)
        {
            fragments.Add($"bag_within={Math.Max(ReadWindow("bagWithin"), SearchTheSpireCatalog.BagFloor(this))}");
        }

        if (ReadWindow("rares") > 0 && SearchTheSpireCatalog.RaresEnabled(this))
        {
            fragments.Add($"rares={ReadWindow("rares")}");
        }

        if (Ascension >= 7)
        {
            fragments.Add("scarcity");
        }

        foreach (var slot in SearchTheSpireCatalog.Slots
                     .Where(slot => !string.IsNullOrWhiteSpace(Selected(slot.Id)))
                     .OrderBy(slot => slot.Id, StringComparer.Ordinal))
        {
            var value = Selected(slot.Id)!;
            if (slot.Id is "neowOffer" or "bonesGrantA" or "bonesGrantB" or
                "rewardWithin" or "rewardOrdered" or "rares" or "rewardPick1" or "rewardPick2" or "rewardPick3" or
                "rewardPick4" or "rewardPick5" or "rewardPick6" or
                "shopWithin" or "bagWithin" or
                "bonesCapsuleSet1" or "bonesCapsuleSet2" or "bonesCapsuleSet3" or
                "bonesKaleidoCard1" or "bonesKaleidoCard2" or
                "bonesScrollboxCard1" or "bonesScrollboxCard2" or "bonesScrollboxCard3" or
                "bonesPoulticeCard1" or "bonesPoulticeCard2" or
                "largeRelicA" or "largeRelicB" or "poulticeCard1" or "poulticeCard2" or
                "kaleidoCard1" or "kaleidoCard2" or "scrollboxCard1" or "scrollboxCard2" or "scrollboxCard3" or
                "phialPotionA" or "phialPotionB")
            {
                continue;
            }

            if (slot.Id.StartsWith("eventPick", StringComparison.Ordinal))
            {
                fragments.Add($"event_in{Math.Max(ReadWindow("eventWithin"), SearchTheSpireCatalog.EventFloor(this))}={value}");
                continue;
            }

            if (slot.Id is "shopPick1" or "shopPick2" or "shopPick3" or "shopPick4" or "shopPick5" or "shopPick6")
            {
                fragments.Add($"shop_relic={value}");
                continue;
            }

            if (slot.Id is "bagPick1" or "bagPick2" or "bagPick3")
            {
                fragments.Add($"bag_relic={value}");
                continue;
            }

            if (!SearchTheSpireCatalog.IsEnabled(slot, this))
            {
                continue;
            }

            fragments.Add($"{SpecKeyFor(slot.Id, value)}={value}");
        }

        return string.Join(',', fragments.Distinct(StringComparer.Ordinal));
    }

    private void AddNeowFragment(List<string> fragments)
    {
        var offer = Selected("neowOffer");
        if (offer == null)
        {
            return;
        }

        var cursedIndex = SearchTheSpireCatalog.CursedOffers
            .Select((id, index) => (id, index))
            .FirstOrDefault(pair => pair.id == offer).index;
        if (SearchTheSpireCatalog.CursedOffers.Contains(offer, StringComparer.Ordinal))
        {
            fragments.Add($"neow={cursedIndex}");
        }
        else
        {
            fragments.Add($"bonus={offer}");
        }
    }

    private void AddJointFragment(List<string> fragments, string key, params string[] slotIds)
    {
        var values = slotIds
            .Where(slotId => SearchTheSpireCatalog.IsEnabled(SearchTheSpireCatalog.GetSlot(slotId), this))
            .Select(Selected)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
        if (values.Length > 0)
        {
            fragments.Add($"{key}={string.Join('+', values)}");
        }
    }

    private void AddRewardPackageFragment(List<string> fragments)
    {
        var values = SearchTheSpireCatalog.RewardPickIds.Select(Selected)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
        if (values.Length == 0)
        {
            return;
        }

        var ordered = string.Equals(Selected("rewardOrdered"), "true", StringComparison.OrdinalIgnoreCase)
            ? ",reward_ordered"
            : string.Empty;
        fragments.Add($"reward_within={Math.Max(ReadWindow("rewardWithin"), SearchTheSpireCatalog.RewardPackageFloor(this))},reward_cards={string.Join('+', values)}{ordered}");
    }

    private int ReadWindow(string slotId) =>
        int.TryParse(Selected(slotId), out var value) ? Math.Max(0, value) : 0;

    private static string SpecKeyFor(string slotId, string? value = null) => slotId switch
    {
        "rewardWithin" => "reward_within",
        "shopWithin" => "shop_within",
        "bagWithin" => "bag_within",
        "eventWithin" => "event_within",
        "bonesCurse" => "bones_curse",
        "ancient2Offers" => value != null && SearchTheSpireCatalog.IsConditionalAncientOffer(2, value) ? "ancient2_offers_if" : "ancient2_offers",
        "ancient3Offers" => value != null && SearchTheSpireCatalog.IsConditionalAncientOffer(3, value) ? "ancient3_offers_if" : "ancient3_offers",
        "bonesTabletCard" => "bones_tablet_card",
        "bonesArcaneCard" => "bones_arcane_card",
        "bonesPaperweightCard" => "bones_paperweight_card",
        "bonesCofferCard" => "bones_coffer_card",
        "bonesCofferPotion" => "bones_coffer_potion",
        "bonesNewleafCard" => "bones_newleaf_card",
        "bonesCapsuleSet1" or "bonesCapsuleSet2" or "bonesCapsuleSet3" => "bones_capsule_set",
        "bonesKaleidoCard1" or "bonesKaleidoCard2" => "bones_kaleido_distinct",
        "bonesScrollboxCard1" or "bonesScrollboxCard2" or "bonesScrollboxCard3" => "bones_scrollbox_contains",
        "bonesPoulticeCard1" or "bonesPoulticeCard2" => "bones_poultice_set",
        "tabletCard" => "tablet_card",
        "poulticeCard1" or "poulticeCard2" => "poultice_set",
        "largeRelicA" or "largeRelicB" => "large_relic",
        "paperweightCard" => "paperweight_card",
        "arcaneCard" => "arcane_card",
        "cofferCard" => "coffer_card",
        "cofferPotion" => "coffer_potion",
        "kaleidoCard1" or "kaleidoCard2" => "kaleido_distinct",
        "newleafCard" => "newleaf_card",
        "scrollboxCard1" or "scrollboxCard2" or "scrollboxCard3" => "scrollbox_contains",
        "phialPotionA" or "phialPotionB" => "phial_potion",
        "capsuleRelic" => "capsule_relic",
        "reward1" => "reward1_card",
        "reward2" => "reward2_card",
        "reward3" => "reward3_card",
        _ => slotId,
    };
}

public static class SearchTheSpirePicker
{
    public static IReadOnlyList<SearchTheSpireOption> Filter(
        IEnumerable<SearchTheSpireOption> options,
        string? search)
    {
        var needle = search?.Trim() ?? string.Empty;
        if (needle.Length == 0)
        {
            return options.ToArray();
        }

        return options
            .Where(option => option.Title.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                            option.Id.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                            option.Section.Contains(needle, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<SearchTheSpireOption>> GroupBySection(
        IEnumerable<SearchTheSpireOption> options)
    {
        return options
            .GroupBy(option => option.Section, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<SearchTheSpireOption>)group.ToArray(),
                StringComparer.Ordinal);
    }
}

public static class SearchTheSpireCatalog
{
    private static readonly IReadOnlyDictionary<string, string> DisplayNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["neowsbones"] = "Neow's Bones",
            ["neowssacrifice"] = "Neow's Sacrifice",
            ["neowstorment"] = "Neow's Torment",
            ["neowstalisman"] = "Neow's Talisman",
            ["largecapsule"] = "Large Capsule",
            ["smallcapsule"] = "Small Capsule",
            ["leafypoultice"] = "Leafy Poultice",
            ["heftytablet"] = "Hefty Tablet",
            ["precariousshears"] = "Precarious Shears",
            ["silvercrucible"] = "Silver Crucible",
            ["cursedpearl"] = "Cursed Pearl",
            ["dowsingrod"] = "Dowsing Rod",
            ["arcanescroll"] = "Arcane Scroll",
            ["boomingconch"] = "Booming Conch",
            ["fishingrod"] = "Fishing Rod",
            ["goldenpearl"] = "Golden Pearl",
            ["kaleidoscope"] = "Kaleidoscope",
            ["leadpaperweight"] = "Lead Paperweight",
            ["lostcoffer"] = "Lost Coffer",
            ["newleaf"] = "New Leaf",
            ["phialholster"] = "Phial Holster",
            ["precisescissors"] = "Precise Scissors",
            ["scrollboxes"] = "Scroll Boxes",
            ["wingedboots"] = "Winged Boots",
            ["lavarock"] = "Lava Rock",
            ["nutritiousoyster"] = "Nutritious Oyster",
            ["stonehumidifier"] = "Stone Humidifier",
            ["pomander"] = "Pomander",
            ["ruinedhelmet"] = "Ruined Helmet",
            ["beatingremnant"] = "Beating Remnant",
            ["bagofpreparation"] = "Bag of Preparation",
            ["unsettlinglamp"] = "Unsettling Lamp",
            ["silkentress"] = "Silken Tress",
            ["bloodvial"] = "Blood Vial",
            ["akabeko"] = "Akabeko",
            ["ceremonialbeast"] = "Ceremonial Beast",
            ["thekin"] = "The Kin",
            ["vantom"] = "Vantom",
            ["lagavulinmatriarch"] = "Lagavulin Matriarch",
            ["soulfysh"] = "Soul Fysh",
            ["waterfallgiant"] = "Waterfall Giant",
            ["kaisercrab"] = "Kaiser Crab",
            ["knowledgedemon"] = "Knowledge Demon",
            ["theinsatiable"] = "The Insatiable",
            ["aeonglass"] = "Aeonglass",
            ["queen"] = "Queen",
            ["testsubject"] = "Test Subject",
            ["orobas"] = "Orobas",
            ["pael"] = "Pael",
            ["tezcatara"] = "Tezcatara",
            ["darv"] = "Darv",
            ["nonupeipe"] = "Nonupeipe",
            ["tanx"] = "Tanx",
            ["vakuu"] = "Vakuu",
            ["strike"] = "Strike",
            ["defend"] = "Defend",
            ["bash"] = "Bash",
            ["ironwave"] = "Ironwave",
            ["neutralize"] = "Neutralize",
            ["backflip"] = "Backflip",
            ["survivor"] = "Survivor",
            ["seer"] = "Seer",
            ["orbit"] = "Orbit",
            ["dismantle"] = "Dismantle",
            ["zap"] = "Zap",
            ["deadly_disease"] = "Deadly Disease",
            ["soul_fire"] = "Soul Fire",
            ["colorless_insight"] = "Colorless Insight",
            ["dominate"] = "Dominate",
            ["tearasunder"] = "Tear Asunder",
            ["pyre"] = "Pyre",
            ["bludgeon"] = "Bludgeon",
            ["pommelstrike"] = "Pommel Strike",
            ["automation"] = "Automation",
            ["equilibrium"] = "Equilibrium",
            ["thinkingahead"] = "Thinking Ahead",
            ["darkshackles"] = "Dark Shackles",
            ["fire_potion"] = "Fire Potion",
            ["swift_potion"] = "Swift Potion",
            ["energy_potion"] = "Energy Potion",
            ["focus_potion"] = "Focus Potion",
            ["steroid_potion"] = "Steroid Potion",
            ["attackpotion"] = "Attack Potion",
            ["bloodpotion"] = "Blood Potion",
            ["clumsy"] = "Clumsy",
            ["debt"] = "Debt",
            ["decay"] = "Decay",
            ["doubt"] = "Doubt",
            ["guilty"] = "Guilty",
            ["injury"] = "Injury",
            ["normality"] = "Normality",
            ["regret"] = "Regret",
            ["shame"] = "Shame",
            ["writhe"] = "Writhe",
            ["aromaofchaos"] = "Aroma Of Chaos",
            ["byrdonisnest"] = "Byrdonis Nest",
            ["densevegetation"] = "Dense Vegetation",
            ["junglemazeadventure"] = "Jungle Maze Adventure",
            ["luminouschoir"] = "Luminous Choir",
            ["morphicgrove"] = "Morphic Grove",
            ["sapphireseed"] = "Sapphire Seed",
            ["tabletoftruth"] = "Tablet Of Truth",
            ["unrestsite"] = "Unrest Site",
            ["wellspring"] = "Wellspring",
            ["whisperinghollow"] = "Whispering Hollow",
            ["woodcarvings"] = "Wood Carvings",
            ["abyssalbaths"] = "Abyssal Baths",
            ["doorsoflightanddark"] = "Doors Of Light And Dark",
            ["drowningbeacon"] = "Drowning Beacon",
            ["endlessconveyor"] = "Endless Conveyor",
            ["punchoff"] = "Punch Off",
            ["spiralingwhirlpool"] = "Spiraling Whirlpool",
            ["sunkentreasury"] = "Sunken Treasury",
            ["trashheap"] = "Trash Heap",
            ["waterloggedscriptorium"] = "Waterlogged Scriptorium",
            ["sunkenstatue"] = "Sunken Statue",
            ["brainleech"] = "Brain Leech",
            ["roomfullofcheese"] = "Room Full Of Cheese",
            ["selfhelpbook"] = "Self Help Book",
            ["slipperybridge"] = "Slippery Bridge",
            ["teamaster"] = "Tea Master",
            ["thefutureofpotions"] = "The Future Of Potions",
            ["thelegendsweretrue"] = "The Legends Were True",
            ["thisorthat"] = "This Or That",
        };

    internal static readonly string[] CursedOffers =
    {
        "cursedpearl", "dowsingrod", "heftytablet", "largecapsule", "leafypoultice",
        "neowsbones", "neowssacrifice", "precariousshears", "silkentress", "silvercrucible",
    };

    private static readonly SearchTheSpireSlot[] SlotTable =
    {
        new("act", "Act 1 map", Cluster: "act", Kind: SearchTheSpireSlotKind.Select),
        new("neowOffer", "Neow offer", Cluster: "neow"),

        new("bonesGrantA", "grants", "neowOffer", "Neow's Bones"),
        new("bonesGrantB", "and grants", "neowOffer", "Neow's Bones"),
        new("bonesCurse", "curse", "neowOffer", "Neow's Bones"),
        new("bonesTabletCard", "offers rare", "neowOffer", "Hefty Tablet", RequiresCharacter: true),
        new("bonesArcaneCard", "the rare is", "neowOffer", "Arcane Scroll", RequiresCharacter: true),
        new("bonesPaperweightCard", "offers", "neowOffer", "Lead Paperweight"),
        new("bonesCofferCard", "offers card", "neowOffer", "Lost Coffer", RequiresCharacter: true),
        new("bonesCofferPotion", "potion", "neowOffer", "Lost Coffer"),
        new("bonesCapsuleSet1", "capsule pulls include", "neowOffer", "Capsules", RequiresCharacter: true),
        new("bonesCapsuleSet2", "and includes", "neowOffer", "Capsules", RequiresCharacter: true),
        new("bonesCapsuleSet3", "and includes", "neowOffer", "Capsules", RequiresCharacter: true),
        new("bonesKaleidoCard1", "offers card", "neowOffer", "Kaleidoscope", RequiresCharacter: true),
        new("bonesKaleidoCard2", "and offers card", "neowOffer", "Kaleidoscope", RequiresCharacter: true),
        new("bonesScrollboxCard1", "a bundle contains", "neowOffer", "Scroll Boxes", RequiresCharacter: true),
        new("bonesScrollboxCard2", "and contains", "neowOffer", "Scroll Boxes", RequiresCharacter: true),
        new("bonesScrollboxCard3", "and contains", "neowOffer", "Scroll Boxes", RequiresCharacter: true),
        new("bonesNewleafCard", "transforming a Basic gives", "neowOffer", "New Leaf", RequiresCharacter: true),
        new("bonesPoulticeCard1", "poultice gives", "neowOffer", "Leafy Poultice", RequiresCharacter: true),
        new("bonesPoulticeCard2", "and also gives", "neowOffer", "Leafy Poultice", RequiresCharacter: true),

        new("tabletCard", "offers rare", "neowOffer", RequiresCharacter: true),
        new("poulticeCard1", "poultice gives", "neowOffer", "Leafy Poultice", RequiresCharacter: true),
        new("poulticeCard2", "and also gives", "neowOffer", "Leafy Poultice", RequiresCharacter: true),
        new("largeRelicA", "pulls", "neowOffer", "Large Capsule"),
        new("largeRelicB", "and pulls", "neowOffer", "Large Capsule"),
        new("paperweightCard", "offers", "neowOffer", "Lead Paperweight"),
        new("arcaneCard", "the rare is", "neowOffer", RequiresCharacter: true),
        new("cofferCard", "offers card", "neowOffer", "Lost Coffer", RequiresCharacter: true),
        new("cofferPotion", "potion", "neowOffer", "Lost Coffer"),
        new("kaleidoCard1", "offers card", "neowOffer", "Kaleidoscope", RequiresCharacter: true),
        new("kaleidoCard2", "and offers card", "neowOffer", "Kaleidoscope", RequiresCharacter: true),
        new("newleafCard", "transforming a Basic gives", "neowOffer", RequiresCharacter: true),
        new("scrollboxCard1", "a bundle contains", "neowOffer", "Scroll Boxes", RequiresCharacter: true),
        new("scrollboxCard2", "and contains", "neowOffer", "Scroll Boxes", RequiresCharacter: true),
        new("scrollboxCard3", "and contains", "neowOffer", "Scroll Boxes", RequiresCharacter: true),
        new("phialPotionA", "grants potion", "neowOffer", "Phial Holster"),
        new("phialPotionB", "and potion", "neowOffer", "Phial Holster"),
        new("capsuleRelic", "grants relic", "neowOffer", "Small Capsule"),

        new("boss1", "Act 1 boss", Cluster: "bosses"),
        new("boss2", "Act 2 boss", Cluster: "bosses"),
        new("boss3", "Act 3 boss", Cluster: "bosses"),
        new("boss3b", "Act 3 second boss (A10)", Cluster: "bosses", RequiresAscension: 10),
        new("ancient2", "Act 2 ancient", Cluster: "ancients"),
        new("ancient2Offers", "offers", "ancient2", "Act 2 ancient", RequiresCharacter: false),
        new("ancient3", "Act 3 ancient", Cluster: "ancients"),
        new("ancient3Offers", "offers", "ancient3", "Act 3 ancient", RequiresCharacter: false),

        new("rewardWithin", "reward window", Cluster: "rewards", Kind: SearchTheSpireSlotKind.Select),
        new("rewardOrdered", "reward order", Cluster: "rewards", Kind: SearchTheSpireSlotKind.Select),
        new("rares", "fresh reward rares", Cluster: "rewards", Kind: SearchTheSpireSlotKind.Select),
        new("rewardPick1", "rewards have", Cluster: "rewards", RequiresCharacter: true),
        new("rewardPick2", "and", Cluster: "rewards", RequiresCharacter: true),
        new("rewardPick3", "and", Cluster: "rewards", RequiresCharacter: true),
        new("rewardPick4", "and", Cluster: "rewards", RequiresCharacter: true),
        new("rewardPick5", "and", Cluster: "rewards", RequiresCharacter: true),
        new("rewardPick6", "and", Cluster: "rewards", RequiresCharacter: true),
        new("reward1", "fight 1 reward has", Cluster: "rewards", RequiresCharacter: true),
        new("reward2", "fight 2 reward has", Cluster: "rewards", RequiresCharacter: true),
        new("reward3", "fight 3 reward has", Cluster: "rewards", RequiresCharacter: true),

        new("shopWithin", "shop window", Cluster: "shops", Kind: SearchTheSpireSlotKind.Select),
        new("shopPick1", "shop relic slot has", Cluster: "shops"),
        new("shopPick2", "shop relic slot has", Cluster: "shops"),
        new("shopPick3", "shop relic slot has", Cluster: "shops"),
        new("shopPick4", "shop relic slot has", Cluster: "shops"),
        new("shopPick5", "shop relic slot has", Cluster: "shops"),
        new("shopPick6", "shop relic slot has", Cluster: "shops"),

        new("bagWithin", "relic reward window", Cluster: "bag", Kind: SearchTheSpireSlotKind.Select),
        new("bagPick1", "relic rewards have", Cluster: "bag", RequiresCharacter: true),
        new("bagPick2", "relic rewards have", Cluster: "bag", RequiresCharacter: true),
        new("bagPick3", "relic rewards have", Cluster: "bag", RequiresCharacter: true),

        new("eventWithin", "event window", Cluster: "events", Kind: SearchTheSpireSlotKind.Select),
        new("eventPick1", "act 1 event", Cluster: "events", Kind: SearchTheSpireSlotKind.Select),
        new("eventPick2", "act 1 event", Cluster: "events", Kind: SearchTheSpireSlotKind.Select),
        new("eventPick3", "act 1 event", Cluster: "events", Kind: SearchTheSpireSlotKind.Select),
        new("eventPick4", "act 1 event", Cluster: "events", Kind: SearchTheSpireSlotKind.Select),
        new("eventPick5", "act 1 event", Cluster: "events", Kind: SearchTheSpireSlotKind.Select),
    };

    private static readonly SearchTheSpireOption[] ActMaps =
    {
        new("0", "Overgrowth", "Act 1 map"),
        new("1", "Underdocks", "Act 1 map"),
    };

    private static readonly SearchTheSpireOption[] NeowOffers =
    {
        new("cursedpearl", "Cursed Pearl", "cursed offer"),
        new("dowsingrod", "Dowsing Rod", "cursed offer"),
        new("heftytablet", "Hefty Tablet", "cursed offer"),
        new("largecapsule", "Large Capsule", "cursed offer"),
        new("leafypoultice", "Leafy Poultice", "cursed offer"),
        new("neowsbones", "Neow's Bones", "cursed offer", "Two relic grants with follow-up details."),
        new("neowssacrifice", "Neow's Sacrifice", "cursed offer"),
        new("precariousshears", "Precarious Shears", "cursed offer"),
        new("silkentress", "Silken Tress", "cursed offer"),
        new("silvercrucible", "Silver Crucible", "cursed offer"),
        new("arcanescroll", "Arcane Scroll", "bonus offer"),
        new("boomingconch", "Booming Conch", "bonus offer"),
        new("fishingrod", "Fishing Rod", "bonus offer"),
        new("goldenpearl", "Golden Pearl", "bonus offer"),
        new("kaleidoscope", "Kaleidoscope", "bonus offer"),
        new("leadpaperweight", "Lead Paperweight", "bonus offer"),
        new("lostcoffer", "Lost Coffer", "bonus offer"),
        new("neowstorment", "Neow's Torment", "bonus offer"),
        new("newleaf", "New Leaf", "bonus offer"),
        new("phialholster", "Phial Holster", "bonus offer"),
        new("precisescissors", "Precise Scissors", "bonus offer"),
        new("scrollboxes", "Scroll Boxes", "bonus offer"),
        new("wingedboots", "Winged Boots", "bonus offer"),
        new("lavarock", "Lava Rock", "bonus offer"),
        new("smallcapsule", "Small Capsule", "bonus offer"),
        new("nutritiousoyster", "Nutritious Oyster", "bonus offer"),
        new("stonehumidifier", "Stone Humidifier", "bonus offer"),
        new("neowstalisman", "Neow's Talisman", "bonus offer"),
        new("pomander", "Pomander", "bonus offer"),
    };

    private static readonly SearchTheSpireOption[] GrantRelics =
        CursedOffers.Concat(new[] { "arcanescroll", "boomingconch", "fishingrod", "goldenpearl", "kaleidoscope", "leadpaperweight", "lostcoffer", "neowstorment", "newleaf", "phialholster", "precisescissors", "scrollboxes", "wingedboots", "lavarock", "neowstalisman", "nutritiousoyster", "pomander", "smallcapsule", "stonehumidifier" })
            .Where(id => id != "neowsbones")
            .Distinct(StringComparer.Ordinal)
            .Select(id => new SearchTheSpireOption(id, Humanize(id), "relic grants"))
            .ToArray();

    private static readonly SearchTheSpireOption[] Curses =
        new[] { "clumsy", "debt", "decay", "doubt", "guilty", "injury", "normality", "regret", "shame", "writhe" }
            .Select(id => new SearchTheSpireOption(id, Humanize(id), "curse"))
            .ToArray();

    private static readonly SearchTheSpireOption[] Bosses =
    {
        new("ceremonialbeast", "Ceremonial Beast", "Overgrowth bosses"),
        new("thekin", "The Kin", "Overgrowth bosses"),
        new("vantom", "Vantom", "Overgrowth bosses"),
        new("lagavulinmatriarch", "Lagavulin Matriarch", "Underdocks bosses"),
        new("soulfysh", "Soul Fysh", "Underdocks bosses"),
        new("waterfallgiant", "Waterfall Giant", "Underdocks bosses"),
        new("kaisercrab", "Kaiser Crab", "Act 2 bosses"),
        new("knowledgedemon", "Knowledge Demon", "Act 2 bosses"),
        new("theinsatiable", "The Insatiable", "Act 2 bosses"),
        new("aeonglass", "Aeonglass", "Act 3 bosses"),
        new("queen", "Queen", "Act 3 bosses"),
        new("testsubject", "Test Subject", "Act 3 bosses"),
    };

    private static readonly SearchTheSpireOption[] Ancients =
    {
        new("orobas", "Orobas", "Act 2 ancients"),
        new("pael", "Pael", "Act 2 ancients"),
        new("tezcatara", "Tezcatara", "Act 2 ancients"),
        new("darv", "Darv", "Act 2 ancients"),
        new("nonupeipe", "Nonupeipe", "Act 3 ancients"),
        new("tanx", "Tanx", "Act 3 ancients"),
        new("vakuu", "Vakuu", "Act 3 ancients"),
        new("darv", "Darv", "Act 3 ancients"),
    };

    private static readonly SearchTheSpireOption[] AncientOffers =
        new[] { "electricshrymp", "glasseye", "prismaticgem", "seaglass", "alchemicalcoffer", "driftwood", "radiantpearl", "sandcastle", "paelsflesh", "paelshorn", "paelstears", "paelswing", "paelsgrowth", "paelseye", "paelsblood", "veryhotcocoa", "yummycookie", "biiighug", "storybook", "toastymittens", "goldencompass", "pumpkincandle", "toybox", "sealofgold", "claws", "crossbow", "ironclub", "meatcleaver", "sai", "spikedgauntlets", "tanxswhistle", "throwingaxe", "warhammer", "bloodsoakedrose", "whisperingearring", "fiddle", "preservedfog", "seretalon", "distinguishedcape", "choicesparadox", "musicbox", "lordsparasol", "jeweledmask", "blessedantler", "brilliantscarf", "delicatefrond", "diamonddiadem", "furcoat", "glitter", "jewelrybox", "loomingfruit", "signetring" }
            .Distinct(StringComparer.Ordinal)
            .Select(id => new SearchTheSpireOption(id, Humanize(id), "ancient offers"))
            .ToArray();

    private static readonly SearchTheSpireOption[] Cards =
        new[] { "strike", "defend", "bash", "ironwave", "neutralize", "backflip", "survivor", "seer", "orbit", "dismantle", "zap", "deadly_disease", "soul_fire", "colorless_insight", "dominate", "tearasunder", "pyre", "bludgeon", "pommelstrike", "automation", "equilibrium", "thinkingahead", "darkshackles" }
            .Select(id => new SearchTheSpireOption(id, Humanize(id), id switch
            {
                "bash" or "ironwave" or "dominate" or "bludgeon" or "pommelstrike" => "Ironclad cards",
                "neutralize" or "backflip" or "survivor" => "Silent cards",
                "seer" or "orbit" => "Regent cards",
                "dismantle" or "zap" or "automation" or "equilibrium" => "Defect cards",
                "deadly_disease" or "soul_fire" => "Necrobinder cards",
                _ => "cards",
            }))
            .ToArray();

    private static readonly SearchTheSpireOption[] Potions =
        new[] { "fire_potion", "swift_potion", "energy_potion", "focus_potion", "steroid_potion", "attackpotion", "bloodpotion" }
            .Select(id => new SearchTheSpireOption(id, Humanize(id), "potions"))
            .ToArray();

    private static readonly SearchTheSpireOption[] Relics =
        new[] { "anchor", "akabeko", "bagofpreparation", "beatingremnant", "bloodvial", "lantern", "kunai", "mango", "oldcoin", "anchor", "unsettlinglamp", "wingedboots", "pomander" }
            .Distinct(StringComparer.Ordinal)
            .Select(id => new SearchTheSpireOption(id, Humanize(id), "relic rewards"))
            .ToArray();

    private static readonly SearchTheSpireOption[] Events =
        new[]
        {
            ("aromaofchaos", "Aroma Of Chaos", 0), ("byrdonisnest", "Byrdonis Nest", 0),
            ("densevegetation", "Dense Vegetation", 0), ("junglemazeadventure", "Jungle Maze Adventure", 0),
            ("luminouschoir", "Luminous Choir", 0), ("morphicgrove", "Morphic Grove", 0),
            ("sapphireseed", "Sapphire Seed", 0), ("tabletoftruth", "Tablet Of Truth", 0),
            ("unrestsite", "Unrest Site", 0), ("wellspring", "Wellspring", 0),
            ("whisperinghollow", "Whispering Hollow", 0), ("woodcarvings", "Wood Carvings", 0),
            ("abyssalbaths", "Abyssal Baths", 1), ("doorsoflightanddark", "Doors Of Light And Dark", 1),
            ("drowningbeacon", "Drowning Beacon", 1), ("endlessconveyor", "Endless Conveyor", 1),
            ("punchoff", "Punch Off", 1), ("spiralingwhirlpool", "Spiraling Whirlpool", 1),
            ("sunkentreasury", "Sunken Treasury", 1), ("trashheap", "Trash Heap", 1),
            ("waterloggedscriptorium", "Waterlogged Scriptorium", 1),
            ("sunkenstatue", "Sunken Statue", -1), ("brainleech", "Brain Leech", -1),
            ("roomfullofcheese", "Room Full Of Cheese", -1), ("selfhelpbook", "Self Help Book", -1),
            ("slipperybridge", "Slippery Bridge", -1), ("teamaster", "Tea Master", -1),
            ("thefutureofpotions", "The Future Of Potions", -1),
            ("thelegendsweretrue", "The Legends Were True", -1), ("thisorthat", "This Or That", -1),
        }
            .Select(item => new SearchTheSpireOption(item.Item1, item.Item2, "act 1 events", EventCondition(item.Item1)))
            .ToArray();

    private static readonly IReadOnlyDictionary<string, string[]> GuaranteedAncientOffers =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["orobas"] = new[] { "electricshrymp", "glasseye", "prismaticgem", "seaglass", "alchemicalcoffer", "driftwood", "radiantpearl", "sandcastle" },
            ["pael"] = new[] { "paelsflesh", "paelshorn", "paelstears", "paelswing", "paelsgrowth", "paelseye", "paelsblood" },
            ["tezcatara"] = new[] { "veryhotcocoa", "yummycookie", "biiighug", "storybook", "toastymittens", "goldencompass", "pumpkincandle", "toybox", "sealofgold" },
            ["tanx"] = new[] { "claws", "crossbow", "ironclub", "meatcleaver", "sai", "spikedgauntlets", "tanxswhistle", "throwingaxe", "warhammer" },
            ["vakuu"] = new[] { "bloodsoakedrose", "whisperingearring", "fiddle", "preservedfog", "seretalon", "distinguishedcape", "choicesparadox", "musicbox", "lordsparasol", "jeweledmask" },
            ["nonupeipe"] = new[] { "blessedantler", "brilliantscarf", "delicatefrond", "diamonddiadem", "furcoat", "glitter", "jewelrybox", "loomingfruit", "signetring" },
        };

    private static readonly IReadOnlyDictionary<string, string[]> ConditionalAncientOffers =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["orobas"] = new[] { "touchoforobas", "archaictooth" },
            ["pael"] = new[] { "paelsclaw", "paelstooth", "paelslegion" },
            ["tezcatara"] = new[] { "nutritioussoup" },
            ["tanx"] = new[] { "triboomerang" },
            ["nonupeipe"] = new[] { "beautifulbracelet" },
        };

    private static readonly IReadOnlyDictionary<int, string[]> DarvOffers =
        new Dictionary<int, string[]>
        {
            [2] = new[] { "astrolabe", "blackstar", "callingbell", "emptycage", "pandorasbox", "runicpyramid", "sneckoeye", "ectoplasm", "sozu", "philosophersstone", "velvetchoker", "dustytome" },
            [3] = new[] { "astrolabe", "blackstar", "callingbell", "emptycage", "pandorasbox", "runicpyramid", "sneckoeye", "philosophersstone", "velvetchoker", "dustytome" },
        };

    private static readonly IReadOnlyDictionary<string, string> AncientConditions =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["touchoforobas"] = "you still have your starter relic",
            ["archaictooth"] = "the starter card Archaic Tooth transforms is still in your deck",
            ["paelsclaw"] = "≥3 Goopy-enchantable cards in your deck",
            ["paelstooth"] = "≥5 removable cards in your deck",
            ["paelslegion"] = "you have no event pet",
            ["nutritioussoup"] = "a Basic Strike remains in your deck",
            ["triboomerang"] = "≥3 Instinct-enchantable cards in your deck",
            ["beautifulbracelet"] = "≥4 Swift-enchantable cards in your deck",
        };

    private static readonly IReadOnlyDictionary<string, string> EventConditions =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["byrdonisnest"] = "you don't already have a pet",
            ["endlessconveyor"] = "120+ gold",
            ["luminouschoir"] = "enough gold and at least one undiscovered relic",
            ["morphicgrove"] = "100+ gold and 2+ transformable cards",
            ["punchoff"] = "floor 6+",
            ["slipperybridge"] = "floor 7+ and a removable card",
            ["spiralingwhirlpool"] = "a card that can take the Spiral enchantment",
            ["teamaster"] = "150+ gold",
            ["thefutureofpotions"] = "2+ potions held",
            ["thelegendsweretrue"] = "10+ HP",
            ["trashheap"] = "more than 5 HP",
            ["unrestsite"] = "HP at or below 70% of max",
            ["waterloggedscriptorium"] = "55+ gold",
            ["whisperinghollow"] = "44+ gold",
            ["woodcarvings"] = "a removable basic card",
        };

    private static readonly IReadOnlyDictionary<string, string> RareCards =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["dominate"] = "Ironclad",
            ["pyre"] = "Ironclad",
            ["tearasunder"] = "Ironclad",
            ["bludgeon"] = "Ironclad",
            ["pommelstrike"] = "Ironclad",
        };

    public static IReadOnlyList<SearchTheSpireSlot> Slots => SlotTable;

    public static IReadOnlyList<string> RewardPickIds { get; } =
        Enumerable.Range(1, 6).Select(index => $"rewardPick{index}").ToArray();

    public static IReadOnlyList<string> ShopPickIds { get; } =
        Enumerable.Range(1, 6).Select(index => $"shopPick{index}").ToArray();

    public static IReadOnlyList<string> BagPickIds { get; } =
        Enumerable.Range(1, 3).Select(index => $"bagPick{index}").ToArray();

    public static IReadOnlyList<string> EventPickIds { get; } =
        Enumerable.Range(1, 5).Select(index => $"eventPick{index}").ToArray();

    public static bool ContainsSlot(string slotId) => SlotTable.Any(slot => slot.Id == slotId);

    internal static bool IsCharacterScoped(SearchTheSpireSlot slot) =>
        slot.RequiresCharacter ||
        slot.Id.StartsWith("shopPick", StringComparison.Ordinal) ||
        slot.Id.StartsWith("bagPick", StringComparison.Ordinal) ||
        slot.Id is "largeRelicA" or "largeRelicB" or "capsuleRelic";

    internal static bool IsSharedCharacterlessValue(SearchTheSpireSlot slot, string value) =>
        slot.Id.StartsWith("shopPick", StringComparison.Ordinal)
            ? SearchTheSpirePoolData.SharedShopRelics.Contains(value, StringComparer.Ordinal)
            : slot.Id.StartsWith("bagPick", StringComparison.Ordinal)
                ? SearchTheSpirePoolData.SharedCapsuleRelics.Contains(value, StringComparer.Ordinal)
                : false;

    internal static RunCharacter? TryOwnerCharacter(SearchTheSpireSlot slot, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var owner = slot.Id.StartsWith("shopPick", StringComparison.Ordinal)
            ? SearchTheSpirePoolData.CharacterShopRelics
            : slot.Id.StartsWith("bagPick", StringComparison.Ordinal) ||
              slot.Id is "largeRelicA" or "largeRelicB" or "capsuleRelic"
                ? SearchTheSpirePoolData.CharacterCapsuleRelics
                : null;
        if (owner == null)
        {
            return null;
        }

        foreach (var pair in owner)
        {
            if (pair.Value.Contains(value, StringComparer.Ordinal))
            {
                return pair.Key;
            }
        }

        return null;
    }

    public static SearchTheSpireSlot GetSlot(string slotId) =>
        SlotTable.FirstOrDefault(slot => slot.Id == slotId) ??
        throw new ArgumentException($"Unknown SearchTheSpire slot '{slotId}'.", nameof(slotId));

    public static int RewardPackageFloor(SearchTheSpireBoardState state)
    {
        var picks = RewardPickIds.Select(state.Selected).Where(value => value != null).ToArray();
        if (picks.Length == 0)
        {
            return 1;
        }

        var floor = picks.Length;
        if (state.Character != RunCharacter.Any)
        {
            var rareCount = picks.Count(value => IsRareCard(value!, state.Character));
            if (rareCount > 0)
            {
                // v0.110.1's shared rare pity offset. Scarcity widens the
                // first three rare positions; the fourth is outside the
                // board's six-fight picker and is deliberately left raw.
                var floors = state.Ascension >= 7 ? new[] { 3, 6, 9, 12, 15, 18 } : new[] { 2, 3, 4, 6, 7, 8 };
                floor = Math.Max(floor, rareCount <= floors.Length ? floors[rareCount - 1] : int.MaxValue);
            }
        }

        return floor;
    }

    public static int ShopFloor(SearchTheSpireBoardState state) =>
        ShopPickIds.Count(id => state.Selected(id) != null);

    public static int EventFloor(SearchTheSpireBoardState state) =>
        EventPickIds.Count(id => state.Selected(id) != null);

    public static int EventWindowTarget(SearchTheSpireBoardState state)
    {
        var floor = EventFloor(state);
        return floor switch
        {
            0 => 1,
            1 => 1,
            _ => Math.Min(floor + 1, 5),
        };
    }

    public static int BagFloor(SearchTheSpireBoardState state)
    {
        var counts = BagPickIds.Select(state.Selected)
            .Where(value => value != null)
            .GroupBy(value => RelicRarity(value!), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Count())
            .DefaultIfEmpty(0);
        return counts.Max();
    }

    public static bool RaresEnabled(SearchTheSpireBoardState state) =>
        state.Selected("neowOffer") is "kaleidoscope" or "lostcoffer" or "leadpaperweight";

    public static bool IsConditionalAncientOffer(int act, string offer) =>
        ConditionalAncientOffers.Values.Any(values => values.Contains(offer, StringComparer.Ordinal));

    internal static IReadOnlyList<string> AncientOfferIdsFor(string ancient, int act) =>
        (ancient == "darv" ? DarvOffers.GetValueOrDefault(act, Array.Empty<string>())
            : GuaranteedAncientOffers.GetValueOrDefault(ancient, Array.Empty<string>()))
        .Concat(ConditionalAncientOffers.GetValueOrDefault(ancient, Array.Empty<string>()))
        .Distinct(StringComparer.Ordinal)
        .ToArray();

    private static bool IsRareCard(string id, RunCharacter character) =>
        SearchTheSpirePoolData.RareCards.TryGetValue(character, out var rareCards) &&
        rareCards.Contains(id, StringComparer.Ordinal);

    private static string RelicRarity(string id) =>
        SearchTheSpirePoolData.RelicRarity.GetValueOrDefault(id, "Common");

    private static string? EventCondition(string id) => id switch
    {
        "byrdonisnest" => "if you don't already have a pet",
        "endlessconveyor" => "if 120+ gold",
        "luminouschoir" => "if enough gold and at least one undiscovered relic",
        "morphicgrove" => "if 100+ gold and 2+ transformable cards",
        "punchoff" => "if floor 6+",
        "slipperybridge" => "if floor 7+ and a removable card",
        "spiralingwhirlpool" => "if a card that can take the Spiral enchantment",
        "teamaster" => "if 150+ gold",
        "thefutureofpotions" => "if 2+ potions held",
        "thelegendsweretrue" => "if 10+ HP",
        "trashheap" => "if more than 5 HP",
        "unrestsite" => "if HP at or below 70% of max",
        "waterloggedscriptorium" => "if 55+ gold",
        "whisperinghollow" => "if 44+ gold",
        "woodcarvings" => "if a removable basic card",
        _ => null,
    };

    public static bool IsEnabled(SearchTheSpireSlot slot, SearchTheSpireBoardState state)
    {
        if (state.Ascension < slot.RequiresAscension)
        {
            return false;
        }

        if (slot.Id == "rares")
        {
            return RaresEnabled(state);
        }

        if (slot.Id == "rewardOrdered")
        {
            return RewardPickIds.Any(id => state.Selected(id) != null);
        }

        if (slot.ParentId == null)
        {
            return true;
        }

        var offer = state.Selected("neowOffer");
        if (slot.ParentId == "neowOffer")
        {
            return slot.Id switch
            {
                "bonesGrantA" or "bonesGrantB" or "bonesCurse" => offer == "neowsbones",
                "bonesTabletCard" => offer == "neowsbones" && HasGrant(state, "heftytablet"),
                "bonesArcaneCard" => offer == "neowsbones" && HasGrant(state, "arcanescroll"),
                "bonesPaperweightCard" => offer == "neowsbones" && HasGrant(state, "leadpaperweight"),
                "bonesCofferCard" or "bonesCofferPotion" => offer == "neowsbones" && HasGrant(state, "lostcoffer"),
                "bonesCapsuleSet1" => offer == "neowsbones" && CapsulePulls(state) >= 1,
                "bonesCapsuleSet2" => offer == "neowsbones" && CapsulePulls(state) >= 2,
                "bonesCapsuleSet3" => offer == "neowsbones" && CapsulePulls(state) >= 3,
                "bonesKaleidoCard1" or "bonesKaleidoCard2" => offer == "neowsbones" && HasGrant(state, "kaleidoscope"),
                "bonesScrollboxCard1" or "bonesScrollboxCard2" or "bonesScrollboxCard3" => offer == "neowsbones" && HasGrant(state, "scrollboxes"),
                "bonesNewleafCard" => offer == "neowsbones" && HasGrant(state, "newleaf"),
                "bonesPoulticeCard1" or "bonesPoulticeCard2" => offer == "neowsbones" && HasGrant(state, "leafypoultice"),
                "tabletCard" => offer == "heftytablet",
                "poulticeCard1" or "poulticeCard2" => offer == "leafypoultice",
                "largeRelicA" or "largeRelicB" => offer == "largecapsule",
                "paperweightCard" => offer == "leadpaperweight",
                "arcaneCard" => offer == "arcanescroll",
                "cofferCard" or "cofferPotion" => offer == "lostcoffer",
                "kaleidoCard1" or "kaleidoCard2" => offer == "kaleidoscope",
                "newleafCard" => offer == "newleaf",
                "scrollboxCard1" or "scrollboxCard2" or "scrollboxCard3" => offer == "scrollboxes",
                "phialPotionA" or "phialPotionB" => offer == "phialholster",
                "capsuleRelic" => offer == "smallcapsule",
                _ => false,
            };
        }

        return slot.ParentId switch
        {
            "ancient2" => state.Selected("ancient2") != null,
            "ancient3" => state.Selected("ancient3") != null,
            _ => true,
        };
    }

    public static IReadOnlyList<SearchTheSpireOption> OptionsFor(
        SearchTheSpireSlot slot,
        SearchTheSpireBoardState state)
    {
        if (slot.Id == "act")
        {
            return ActMaps.Select(option => BlockForStateMap(option, state)).ToArray();
        }

        if (slot.Id == "neowOffer") return NeowOffers;
        if (slot.Id is "bonesGrantA" or "bonesGrantB")
        {
            var sibling = slot.Id == "bonesGrantA" ? "bonesGrantB" : "bonesGrantA";
            return BlockDuplicates(GrantRelics, state.Selected(sibling), "the two grants must be distinct");
        }

        if (slot.Id == "bonesCurse") return Curses;
        if (slot.Id == "boss1")
        {
            var act = state.Selected("act");
            return Bosses.Where(option => option.Section is "Overgrowth bosses" or "Underdocks bosses")
                .Select(option => BlockForBossMap(option, act)).ToArray();
        }

        if (slot.Id == "boss2") return Bosses.Where(option => option.Section == "Act 2 bosses").ToArray();
        if (slot.Id is "boss3" or "boss3b")
        {
            var sibling = slot.Id == "boss3" ? "boss3b" : "boss3";
            return BlockDuplicates(Bosses.Where(option => option.Section == "Act 3 bosses"), state.Selected(sibling), "the two Act 3 bosses must be distinct");
        }

        if (slot.Id is "ancient2" or "ancient3")
        {
            var section = slot.Id == "ancient2" ? "Act 2 ancients" : "Act 3 ancients";
            return Ancients.Where(option => option.Section == section).ToArray();
        }

        if (slot.Id is "ancient2Offers" or "ancient3Offers")
        {
            var act = slot.Id == "ancient2Offers" ? 2 : 3;
            var ancient = state.Selected($"ancient{act}");
            return AncientOptions(ancient, act);
        }

        if (slot.Id == "rewardOrdered")
        {
            return new[]
            {
                new SearchTheSpireOption("false", "any order", "reward order"),
                new SearchTheSpireOption("true", "in this order", "reward order"),
            };
        }

        if (slot.Id == "rares")
        {
            return state.Selected("neowOffer") == "kaleidoscope"
                ? new[]
                {
                    new SearchTheSpireOption("0", "any", "fresh reward rares"),
                    new SearchTheSpireOption("3", "reward 1 all rare", "fresh reward rares"),
                    new SearchTheSpireOption("6", "both all rare", "fresh reward rares"),
                }
                : Enumerable.Range(0, 5)
                    .Select(value => new SearchTheSpireOption(value.ToString(), value == 0 ? "any" : $"first {value} roll rare", "fresh reward rares"))
                    .ToArray();
        }

        if (slot.Id is "rewardWithin" or "shopWithin" or "bagWithin" or "eventWithin") return WithinOptions(slot.Id);
        if (slot.Id.StartsWith("eventPick", StringComparison.Ordinal))
        {
            var taken = SearchTheSpireCatalog.Slots.Where(candidate => candidate.Id.StartsWith("eventPick", StringComparison.Ordinal) && candidate.Id != slot.Id)
                .Select(candidate => state.Selected(candidate.Id)).Where(value => value != null).ToHashSet(StringComparer.Ordinal);
            var map = EffectiveEventMap(state);
            return Events
                .Where(option => map == null || EventMap(option.Id) is null || EventMap(option.Id) == map)
                .Select(option => map == null && EventMap(option.Id) is { } eventMap
                    ? option with { Title = $"{option.Title} ({(eventMap == 0 ? "Overgrowth" : "Underdocks")})" }
                    : option)
                .Select(option => taken.Contains(option.Id)
                ? option with { Blocked = true, BlockReason = "event picks must be distinct" }
                : option).ToArray();
        }

        if (slot.Id.StartsWith("shopPick", StringComparison.Ordinal) || slot.Id.StartsWith("bagPick", StringComparison.Ordinal))
        {
            var prefix = slot.Id.StartsWith("shop", StringComparison.Ordinal) ? "shopPick" : "bagPick";
            var taken = SearchTheSpireCatalog.Slots.Where(candidate => candidate.Id.StartsWith(prefix, StringComparison.Ordinal) && candidate.Id != slot.Id)
                .Select(candidate => state.Selected(candidate.Id)).Where(value => value != null).ToHashSet(StringComparer.Ordinal);
            var pool = slot.Id.StartsWith("shop", StringComparison.Ordinal)
                ? ShopRelicOptions(state)
                : CapsuleRelicOptions(state);
            return pool.Select(option => taken.Contains(option.Id)
                ? option with { Blocked = true, BlockReason = "repeated relic picks must be distinct" }
                : option).ToArray();
        }

        var source = slot.Id.Contains("Potion", StringComparison.OrdinalIgnoreCase) ||
                     slot.Id.Contains("potion", StringComparison.OrdinalIgnoreCase) ||
                     slot.Id == "cofferPotion" || slot.Id == "phialPotionA" || slot.Id == "phialPotionB"
            ? PotionOptions(state)
            : slot.Id.Contains("Relic", StringComparison.OrdinalIgnoreCase) || slot.Id.Contains("Capsule", StringComparison.OrdinalIgnoreCase)
                ? CapsuleRelicOptions(state)
                : CardOptions(state, slot.Id);

        source = ApplyGroupedConstraints(slot, state, source);

        if (!slot.RequiresCharacter || state.Character != RunCharacter.Any)
        {
            return source;
        }

        return source.Select(option => option with
        {
            Blocked = true,
            BlockReason = "choose a character before narrowing card results",
        }).ToArray();
    }

    private static IReadOnlyList<SearchTheSpireOption> CardOptions(SearchTheSpireBoardState state, string slotId)
    {
        var otherCharacterPool = slotId.Contains("Kaleido", StringComparison.OrdinalIgnoreCase);
        var characters = state.Character == RunCharacter.Any
            ? Array.Empty<RunCharacter>()
            : new[] { state.Character };
        var ids = characters.Length == 0
            ? Cards.Select(option => option.Id)
            : characters.SelectMany(character =>
            {
                var pool = SearchTheSpirePoolData.CardPools.TryGetValue(character, out var values)
                    ? values
                    : Array.Empty<string>();
                var includesLegacy = pool.Concat(Cards.Select(option => option.Id));
                if (otherCharacterPool)
                {
                    includesLegacy = SearchTheSpirePoolData.CardPools
                        .Where(pair => pair.Key != character)
                        .SelectMany(pair => pair.Value);
                }

                return includesLegacy;
            });

        var selectedCharacter = state.Character == RunCharacter.Any
            ? null
            : otherCharacterPool ? "other character" : CharacterName(state.Character);
        return ids.Distinct(StringComparer.Ordinal)
            .Select(id =>
            {
                var rarity = state.Character == RunCharacter.Any
                    ? null
                    : otherCharacterPool ? OtherCharacterCardRarity(state.Character, id) : CardRarity(state.Character, id);
                return new SearchTheSpireOption(
                    id,
                    Humanize(id),
                    selectedCharacter == null ? "cards" : $"{selectedCharacter} cards · {rarity?.ToLowerInvariant() ?? "common"}",
                    Rarity: rarity,
                    OwnerCharacter: selectedCharacter);
            })
            .ToArray();
    }

    private static IReadOnlyList<SearchTheSpireOption> PotionOptions(SearchTheSpireBoardState state)
    {
        if (state.Character == RunCharacter.Any)
        {
            return Potions;
        }

        var characterPotions = SearchTheSpirePoolData.CharacterPotions.TryGetValue(state.Character, out var values)
            ? values
            : Array.Empty<string>();
        return characterPotions.Concat(SearchTheSpirePoolData.SharedPotions)
            .Distinct(StringComparer.Ordinal)
            .Select(id => new SearchTheSpireOption(
                id,
                Humanize(id),
                characterPotions.Contains(id, StringComparer.Ordinal) ? $"{CharacterName(state.Character)} potions" : "shared potions",
                OwnerCharacter: CharacterName(state.Character)))
            .ToArray();
    }

    private static IReadOnlyList<SearchTheSpireOption> ShopRelicOptions(SearchTheSpireBoardState state)
    {
        var shared = SearchTheSpirePoolData.SharedShopRelics
            .Select(id => new SearchTheSpireOption(id, Humanize(id), "any character", Rarity: "Shop"));
        if (state.Character != RunCharacter.Any)
        {
            var own = SearchTheSpirePoolData.CharacterShopRelics.TryGetValue(state.Character, out var values)
                ? values
                : Array.Empty<string>();
            return shared.Concat(own.Select(id => new SearchTheSpireOption(
                    id,
                    Humanize(id),
                    $"{CharacterName(state.Character)} — picks the character",
                    OwnerCharacter: CharacterName(state.Character),
                    Rarity: "Shop")))
                .ToArray();
        }

        return shared.Concat(SearchTheSpirePoolData.CharacterShopRelics.SelectMany(pair =>
                pair.Value.Select(id => new SearchTheSpireOption(
                    id,
                    Humanize(id),
                    $"{CharacterName(pair.Key)} — picks the character",
                    OwnerCharacter: CharacterName(pair.Key),
                    Rarity: "Shop"))))
            .ToArray();
    }

    private static IReadOnlyList<SearchTheSpireOption> CapsuleRelicOptions(SearchTheSpireBoardState state)
    {
        var shared = SearchTheSpirePoolData.SharedCapsuleRelics
            .Select(id => new SearchTheSpireOption(
                id,
                Humanize(id),
                state.Character == RunCharacter.Any ? "any character" : "relic rewards",
                Rarity: SearchTheSpirePoolData.RelicRarity.GetValueOrDefault(id, "Common")));
        if (state.Character == RunCharacter.Any)
        {
            return shared.Concat(SearchTheSpirePoolData.CharacterCapsuleRelics.SelectMany(pair =>
                    pair.Value.Select(id => new SearchTheSpireOption(
                        id,
                        Humanize(id),
                        $"{CharacterName(pair.Key)} — picks the character",
                        OwnerCharacter: CharacterName(pair.Key),
                        Rarity: SearchTheSpirePoolData.RelicRarity.GetValueOrDefault(id, "Rare")))))
                .ToArray();
        }

        var own = SearchTheSpirePoolData.CharacterCapsuleRelics.TryGetValue(state.Character, out var values)
            ? values
            : Array.Empty<string>();
        return shared.Concat(own.Select(id => new SearchTheSpireOption(
                id,
                Humanize(id),
                $"{CharacterName(state.Character)} relics · {SearchTheSpirePoolData.RelicRarity.GetValueOrDefault(id, "Rare").ToLowerInvariant()}",
                OwnerCharacter: CharacterName(state.Character),
                Rarity: SearchTheSpirePoolData.RelicRarity.GetValueOrDefault(id, "Rare"))))
            .ToArray();
    }

    private static string CharacterName(RunCharacter character) => character.ToString();

    private static string CardRarity(RunCharacter character, string id)
    {
        if (SearchTheSpirePoolData.RareCards.TryGetValue(character, out var rare) && rare.Contains(id, StringComparer.Ordinal))
        {
            return "Rare";
        }

        if (SearchTheSpirePoolData.UncommonCards.TryGetValue(character, out var uncommon) && uncommon.Contains(id, StringComparer.Ordinal))
        {
            return "Uncommon";
        }

        return "Common";
    }

    private static string OtherCharacterCardRarity(RunCharacter currentCharacter, string id)
    {
        foreach (var pair in SearchTheSpirePoolData.RareCards)
        {
            if (pair.Key != currentCharacter && pair.Value.Contains(id, StringComparer.Ordinal))
            {
                return "Rare";
            }
        }

        foreach (var pair in SearchTheSpirePoolData.UncommonCards)
        {
            if (pair.Key != currentCharacter && pair.Value.Contains(id, StringComparer.Ordinal))
            {
                return "Uncommon";
            }
        }

        return "Common";
    }

    private static SearchTheSpireOption BlockForBossMap(SearchTheSpireOption option, string? act)
    {
        var isOvergrowth = option.Section.Contains("Overgrowth", StringComparison.Ordinal);
        var isUnderdocks = option.Section.Contains("Underdocks", StringComparison.Ordinal);
        var blocked = (act == "0" && isUnderdocks) || (act == "1" && isOvergrowth);
        return blocked ? option with { Blocked = true, BlockReason = "this boss belongs to the other Act 1 map" } : option;
    }

    private static SearchTheSpireOption BlockForStateMap(SearchTheSpireOption option, SearchTheSpireBoardState state)
    {
        var implied = state.Selected("act") ?? BossMap(state.Selected("boss1"))?.ToString() ?? EffectiveEventMap(state)?.ToString();
        return implied != null && option.Id != implied
            ? option with { Blocked = true, BlockReason = "this map contradicts a pinned boss or event" }
            : option;
    }

    private static int? BossMap(string? boss) =>
        boss is "ceremonialbeast" or "thekin" or "vantom" ? 0 :
        boss is "lagavulinmatriarch" or "soulfysh" or "waterfallgiant" ? 1 : null;

    public static int? EffectiveEventMap(SearchTheSpireBoardState state)
    {
        var pinned = state.Selected("act") != null
            ? int.Parse(state.Selected("act")!)
            : BossMap(state.Selected("boss1"));
        if (pinned != null)
        {
            return pinned;
        }

        foreach (var id in EventPickIds.Select(state.Selected).Where(value => value != null))
        {
            if (EventMap(id!) is { } map)
            {
                return map;
            }
        }

        return null;
    }

    private static int? EventMap(string id) => id switch
    {
        "aromaofchaos" or "byrdonisnest" or "densevegetation" or "junglemazeadventure" or
        "luminouschoir" or "morphicgrove" or "sapphireseed" or "tabletoftruth" or "unrestsite" or
        "wellspring" or "whisperinghollow" or "woodcarvings" => 0,
        "abyssalbaths" or "doorsoflightanddark" or "drowningbeacon" or "endlessconveyor" or
        "punchoff" or "spiralingwhirlpool" or "sunkentreasury" or "trashheap" or
        "waterloggedscriptorium" => 1,
        _ => null,
    };

    private static int CapsulePulls(SearchTheSpireBoardState state) =>
        (HasGrant(state, "smallcapsule") ? 1 : 0) + (HasGrant(state, "largecapsule") ? 2 : 0);

    private static IReadOnlyList<SearchTheSpireOption> AncientOptions(string? ancient, int act)
    {
        if (ancient == null)
        {
            return Array.Empty<SearchTheSpireOption>();
        }

        return SearchTheSpireCatalog.AncientOfferIdsFor(ancient, act)
            .Select(id =>
            {
                var conditional = IsConditionalAncientOffer(act, id);
                var description = conditional && AncientConditions.TryGetValue(id, out var condition)
                    ? $"if {condition}"
                    : null;
                return new SearchTheSpireOption(
                    id,
                    Humanize(id),
                    conditional ? "offers if you qualify" : "offers on any run",
                    description,
                    SpecKey: conditional ? $"ancient{act}_offers_if" : $"ancient{act}_offers",
                    IsConditional: conditional);
            })
            .ToArray();
    }

    private static IReadOnlyList<SearchTheSpireOption> BlockDuplicates(IEnumerable<SearchTheSpireOption> options, string? duplicate, string reason) =>
        options.Select(option => option.Id == duplicate ? option with { Blocked = true, BlockReason = reason } : option).ToArray();

    private static IReadOnlyList<SearchTheSpireOption> ApplyGroupedConstraints(
        SearchTheSpireSlot slot,
        SearchTheSpireBoardState state,
        IReadOnlyList<SearchTheSpireOption> options)
    {
        var distinctGroup = slot.Id switch
        {
            "largeRelicA" or "largeRelicB" => new[] { "largeRelicA", "largeRelicB" },
            "kaleidoCard1" or "kaleidoCard2" => new[] { "kaleidoCard1", "kaleidoCard2" },
            "bonesKaleidoCard1" or "bonesKaleidoCard2" => new[] { "bonesKaleidoCard1", "bonesKaleidoCard2" },
            "bonesCapsuleSet1" or "bonesCapsuleSet2" or "bonesCapsuleSet3" => new[] { "bonesCapsuleSet1", "bonesCapsuleSet2", "bonesCapsuleSet3" },
            _ => Array.Empty<string>(),
        };
        if (distinctGroup.Length > 0)
        {
            var taken = distinctGroup
                .Where(id => id != slot.Id)
                .Select(state.Selected)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToHashSet(StringComparer.Ordinal);
            options = options.Select(option => taken.Contains(option.Id)
                    ? option with { Blocked = true, BlockReason = "these grouped picks must be distinct" }
                    : option)
                .ToArray();
        }

        if (slot.Id is "scrollboxCard1" or "scrollboxCard2" or "scrollboxCard3" or
            "bonesScrollboxCard1" or "bonesScrollboxCard2" or "bonesScrollboxCard3")
        {
            var group = slot.Id.StartsWith("bones", StringComparison.Ordinal)
                ? new[] { "bonesScrollboxCard1", "bonesScrollboxCard2", "bonesScrollboxCard3" }
                : new[] { "scrollboxCard1", "scrollboxCard2", "scrollboxCard3" };
            var taken = group
                .Where(id => id != slot.Id)
                .Select(state.Selected)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToHashSet(StringComparer.Ordinal);
            var commonCount = taken.Count(value => IsCommonCard(value, state));
            var hasUncommon = taken.Any(value => IsUncommonCard(value, state));
            options = options
                .Where(option => IsCommonCard(option.Id, state) || IsUncommonCard(option.Id, state))
                .Where(option => !taken.Contains(option.Id))
                .Where(option => !(commonCount >= 2 && IsCommonCard(option.Id, state)))
                .Where(option => !(hasUncommon && IsUncommonCard(option.Id, state)))
                .ToArray();
        }

        return options;
    }

    private static bool IsCommonCard(string id, SearchTheSpireBoardState state) =>
        state.Character != RunCharacter.Any &&
        SearchTheSpirePoolData.CommonCards.TryGetValue(state.Character, out var cards) &&
        cards.Contains(id, StringComparer.Ordinal);

    private static bool IsUncommonCard(string id, SearchTheSpireBoardState state) =>
        state.Character != RunCharacter.Any &&
        SearchTheSpirePoolData.UncommonCards.TryGetValue(state.Character, out var cards) &&
        cards.Contains(id, StringComparer.Ordinal);

    private static IReadOnlyList<SearchTheSpireOption> WithinOptions(string slotId)
    {
        var max = slotId == "eventWithin" ? 5 : 6;
        return Enumerable.Range(1, max).Select(value => new SearchTheSpireOption(value.ToString(), $"within {value}", slotId)).ToArray();
    }

    private static bool HasGrant(SearchTheSpireBoardState state, string id) =>
        state.Selected("bonesGrantA") == id || state.Selected("bonesGrantB") == id;

    private static bool HasAnyGrant(SearchTheSpireBoardState state, params string[] ids) =>
        ids.Any(id => HasGrant(state, id));

    public static string DisplayName(string id)
    {
        if (DisplayNames.TryGetValue(id, out var name))
        {
            return name;
        }

        return string.Join(' ', id.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Length == 0 ? part : char.ToUpperInvariant(part[0]) + part[1..]))
            .Replace("dollroom", "Doll Room", StringComparison.OrdinalIgnoreCase);
    }

    private static string Humanize(string id) => DisplayName(id);
}
