namespace SeedSearchPrototype;

/// <summary>
/// The small, serialisation-free part of the SearchTheSpire board model that
/// the in-game UI needs.  A slot is deliberately separate from its picker:
/// selecting a parent can reveal a different set of child slots, while the
/// picker remains a reusable searchable surface.
/// </summary>
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
    bool RequiresCharacter = false);

public sealed record SearchTheSpireOption(
    string Id,
    string Title,
    string Section,
    string? Description = null,
    bool Blocked = false,
    string? BlockReason = null);

public sealed record SearchTheSpireSlotView(
    SearchTheSpireSlot Slot,
    IReadOnlyList<SearchTheSpireOption> Options,
    bool RequiresCharacter);

/// <summary>
/// Immutable board state.  Values intentionally survive parent gating, just
/// like SearchTheSpire's board: clearing a parent relaxes the query without
/// destroying a detail pin that may become active again later.
/// </summary>
public sealed class SearchTheSpireBoardState
{
    private readonly IReadOnlyDictionary<string, string?> _values;

    private SearchTheSpireBoardState(
        RunCharacter character,
        IReadOnlyDictionary<string, string?> values)
    {
        Character = character;
        _values = values;
    }

    public static SearchTheSpireBoardState Empty { get; } =
        new(RunCharacter.Any, new Dictionary<string, string?>());

    public RunCharacter Character { get; }

    public string? Selected(string slotId) =>
        _values.TryGetValue(slotId, out var value) ? value : null;

    public SearchTheSpireBoardState WithCharacter(RunCharacter character) =>
        new(character, new Dictionary<string, string?>(_values));

    public SearchTheSpireBoardState Select(string slotId, string? value)
    {
        if (!SearchTheSpireCatalog.ContainsSlot(slotId))
        {
            throw new ArgumentException($"Unknown SearchTheSpire slot '{slotId}'.", nameof(slotId));
        }

        var next = new Dictionary<string, string?>(_values)
        {
            [slotId] = string.IsNullOrWhiteSpace(value) ? null : value,
        };
        return new SearchTheSpireBoardState(Character, next);
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
    /// A compact, shareable representation for the extended board state.  It
    /// is intentionally a local query fragment rather than a game seed or a
    /// claim that this prototype's search engine is the game's RNG.
    /// </summary>
    public string ToSpec()
    {
        var fragments = new List<string>();
        if (Character != RunCharacter.Any)
        {
            fragments.Add($"char={Character.ToString().ToLowerInvariant()}");
        }

        foreach (var pair in _values
                     .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
                     .OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            fragments.Add($"{pair.Key}={pair.Value}");
        }

        return string.Join(',', fragments);
    }
}

/// <summary>
/// SearchTheSpire's picker is section-aware and keeps blocked options visible
/// with an explanation.  These helpers keep that behaviour independent of
/// Godot so it can be covered by the console test target as well.
/// </summary>
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
    private static readonly SearchTheSpireSlot[] SlotTable =
    {
        new("neowOffer", "Neow offer"),

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
        new("newleafCard", "transforming a Basic gives", "neowOffer", "New Leaf", RequiresCharacter: true),
        new("scrollboxCard1", "a bundle contains", "neowOffer", "Scroll Boxes", RequiresCharacter: true),
        new("scrollboxCard2", "and contains", "neowOffer", "Scroll Boxes", RequiresCharacter: true),
        new("scrollboxCard3", "and contains", "neowOffer", "Scroll Boxes", RequiresCharacter: true),
        new("phialPotionA", "grants potion", "neowOffer", "Phial Holster", RequiresCharacter: true),
        new("phialPotionB", "and potion", "neowOffer", "Phial Holster", RequiresCharacter: true),
        new("capsuleRelic", "grants relic", "neowOffer", "Small Capsule"),
    };

    private static readonly SearchTheSpireOption[] NeowOffers =
    {
        new("heftytablet", "Hefty Tablet", "offers if you qualify", "A rare card offer."),
        new("arcanescroll", "Arcane Scroll", "offers if you qualify", "A rare card offer."),
        new("leadpaperweight", "Lead Paperweight", "offers if you qualify", "A colorless card offer."),
        new("lostcoffer", "Lost Coffer", "offers if you qualify", "A card and potion offer."),
        new("kaleidoscope", "Kaleidoscope", "offers if you qualify", "Two reward card packages."),
        new("leafypoultice", "Leafy Poultice", "offers if you qualify", "Two transformation results."),
        new("newleaf", "New Leaf", "offers if you qualify", "A Basic card transformation."),
        new("scrollboxes", "Scroll Boxes", "offers if you qualify", "A bundled card package."),
        new("phialholster", "Phial Holster", "offers if you qualify", "Two potion pulls."),
        new("largecapsule", "Large Capsule", "offers if you qualify", "Two relic pulls."),
        new("smallcapsule", "Small Capsule", "offers if you qualify", "A relic pull."),
        new("neowsbones", "Neow's Bones", "offers on any run", "Two relic grants with follow-up details."),
        new("touchoforobas", "Touch of Orobas", "offers on any run"),
        new("archaictooth", "Archaic Tooth", "offers on any run"),
        new("nutritioussoup", "Nutritious Soup", "offers on any run"),
        new("triboomerang", "Triboome-rang", "offers if you qualify"),
        new("beautifulbracelet", "Beautiful Bracelet", "offers if you qualify"),
        new("paelsclaw", "Pael's Claw", "offers if you qualify"),
        new("paelstooth", "Pael's Tooth", "offers if you qualify"),
        new("paelslegion", "Pael's Legion", "offers if you qualify"),
    };

    private static readonly SearchTheSpireOption[] GrantRelics =
    {
        new("heftytablet", "Hefty Tablet", "relic grants"),
        new("arcanescroll", "Arcane Scroll", "relic grants"),
        new("leadpaperweight", "Lead Paperweight", "relic grants"),
        new("lostcoffer", "Lost Coffer", "relic grants"),
        new("kaleidoscope", "Kaleidoscope", "relic grants"),
        new("leafypoultice", "Leafy Poultice", "relic grants"),
        new("newleaf", "New Leaf", "relic grants"),
        new("scrollboxes", "Scroll Boxes", "relic grants"),
        new("phialholster", "Phial Holster", "relic grants"),
        new("largecapsule", "Large Capsule", "relic grants"),
        new("smallcapsule", "Small Capsule", "relic grants"),
        new("touchoforobas", "Touch of Orobas", "relic grants"),
        new("archaictooth", "Archaic Tooth", "relic grants"),
    };

    private static readonly SearchTheSpireOption[] Curses =
    {
        new("regret", "Regret", "curse"),
        new("doubt", "Doubt", "curse"),
        new("shame", "Shame", "curse"),
        new("writhe", "Writhe", "curse"),
    };

    private static readonly SearchTheSpireOption[] Cards =
    {
        new("strike", "Strike", "cards"),
        new("defend", "Defend", "cards"),
        new("bash", "Bash", "Ironclad cards"),
        new("ironwave", "Iron Wave", "Ironclad cards"),
        new("neutralize", "Neutralize", "Silent cards"),
        new("backflip", "Backflip", "Silent cards"),
        new("survivor", "Survivor", "Silent cards"),
        new("seer", "Seer", "Regent cards"),
        new("orbit", "Orbit", "Regent cards"),
        new("dismantle", "Dismantle", "Defect cards"),
        new("zap", "Zap", "Defect cards"),
        new("deadly_disease", "Deadly Disease", "Necrobinder cards"),
        new("soul_fire", "Soul Fire", "Necrobinder cards"),
        new("colorless_insight", "Insight", "colorless cards"),
    };

    private static readonly SearchTheSpireOption[] Potions =
    {
        new("fire_potion", "Fire Potion", "potions"),
        new("swift_potion", "Swift Potion", "potions"),
        new("energy_potion", "Energy Potion", "potions"),
        new("focus_potion", "Focus Potion", "potions"),
        new("steroid_potion", "Steroid Potion", "potions"),
    };

    public static IReadOnlyList<SearchTheSpireSlot> Slots => SlotTable;

    public static bool ContainsSlot(string slotId) => SlotTable.Any(slot => slot.Id == slotId);

    public static SearchTheSpireSlot GetSlot(string slotId) =>
        SlotTable.FirstOrDefault(slot => slot.Id == slotId) ??
        throw new ArgumentException($"Unknown SearchTheSpire slot '{slotId}'.", nameof(slotId));

    public static bool IsEnabled(SearchTheSpireSlot slot, SearchTheSpireBoardState state)
    {
        var offer = state.Selected("neowOffer");
        if (slot.ParentId != "neowOffer")
        {
            return true;
        }

        return slot.Id switch
        {
            "bonesGrantA" or "bonesGrantB" or "bonesCurse" => offer == "neowsbones",
            "bonesTabletCard" => offer == "neowsbones" && HasGrant(state, "heftytablet"),
            "bonesArcaneCard" => offer == "neowsbones" && HasGrant(state, "arcanescroll"),
            "bonesPaperweightCard" => offer == "neowsbones" && HasGrant(state, "leadpaperweight"),
            "bonesCofferCard" or "bonesCofferPotion" => offer == "neowsbones" && HasGrant(state, "lostcoffer"),
            "bonesCapsuleSet1" or "bonesCapsuleSet2" or "bonesCapsuleSet3" =>
                offer == "neowsbones" && HasAnyGrant(state, "largecapsule", "smallcapsule"),
            "bonesKaleidoCard1" or "bonesKaleidoCard2" => offer == "neowsbones" && HasGrant(state, "kaleidoscope"),
            "bonesScrollboxCard1" or "bonesScrollboxCard2" or "bonesScrollboxCard3" =>
                offer == "neowsbones" && HasGrant(state, "scrollboxes"),
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

    public static IReadOnlyList<SearchTheSpireOption> OptionsFor(
        SearchTheSpireSlot slot,
        SearchTheSpireBoardState state)
    {
        if (slot.Id == "neowOffer")
        {
            return NeowOffers;
        }

        if (slot.Id is "bonesGrantA" or "bonesGrantB")
        {
            var sibling = slot.Id == "bonesGrantA" ? "bonesGrantB" : "bonesGrantA";
            var siblingValue = state.Selected(sibling);
            return GrantRelics
                .Select(option => siblingValue == option.Id
                    ? option with { Blocked = true, BlockReason = "the two grants must be distinct" }
                    : option)
                .ToArray();
        }

        if (slot.Id == "bonesCurse")
        {
            return Curses;
        }

        var source = slot.Id.Contains("Potion", StringComparison.OrdinalIgnoreCase) ||
                     slot.Id.Contains("Potion", StringComparison.Ordinal)
            ? Potions
            : slot.Id is "largeRelicA" or "largeRelicB" or "capsuleRelic" or "bonesCapsuleSet1" or "bonesCapsuleSet2" or "bonesCapsuleSet3"
                ? GrantRelics
                : Cards;

        if (!slot.RequiresCharacter || state.Character != RunCharacter.Any)
        {
            return source;
        }

        return source
            .Select(option => option with
            {
                Blocked = true,
                BlockReason = "choose a character before narrowing card results",
            })
            .ToArray();
    }

    private static bool HasGrant(SearchTheSpireBoardState state, string id) =>
        state.Selected("bonesGrantA") == id || state.Selected("bonesGrantB") == id;

    private static bool HasAnyGrant(SearchTheSpireBoardState state, params string[] ids) =>
        ids.Any(id => HasGrant(state, id));
}
