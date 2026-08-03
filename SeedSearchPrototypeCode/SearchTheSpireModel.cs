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
    string? BlockReason = null);

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
        Ascension = Math.Clamp(ascension, 0, 20);
        _values = values;
    }

    public static SearchTheSpireBoardState Empty { get; } =
        new(RunCharacter.Any, 0, new Dictionary<string, string?>());

    public RunCharacter Character { get; }

    public int Ascension { get; }

    public string? Selected(string slotId) =>
        _values.TryGetValue(slotId, out var value) ? value : null;

    public SearchTheSpireBoardState WithCharacter(RunCharacter character) =>
        new(character, Ascension, new Dictionary<string, string?>(_values));

    public SearchTheSpireBoardState WithAscension(int ascension) =>
        new(Character, ascension, new Dictionary<string, string?>(_values));

    public SearchTheSpireBoardState Select(string slotId, string? value)
    {
        if (!SearchTheSpireCatalog.ContainsSlot(slotId))
        {
            throw new ArgumentException($"Unknown SearchTheSpire slot '{slotId}'.", nameof(slotId));
        }

        var next = new Dictionary<string, string?>(_values)
        {
            [slotId] = string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant(),
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
        AddJointFragment(fragments, "reward_cards", "rewardPick1", "rewardPick2", "rewardPick3", "rewardPick4", "rewardPick5", "rewardPick6");

        foreach (var slotId in new[] { "rewardWithin", "shopWithin", "bagWithin" })
        {
            if (Selected(slotId) is { } value)
            {
                fragments.Add($"{SpecKeyFor(slotId)}={value}");
            }
        }

        foreach (var slot in SearchTheSpireCatalog.Slots
                     .Where(slot => !string.IsNullOrWhiteSpace(Selected(slot.Id)))
                     .OrderBy(slot => slot.Id, StringComparer.Ordinal))
        {
            var value = Selected(slot.Id)!;
            if (slot.Id is "neowOffer" or "bonesGrantA" or "bonesGrantB" or
                "rewardWithin" or "rewardPick1" or "rewardPick2" or "rewardPick3" or
                "rewardPick4" or "rewardPick5" or "rewardPick6" or
                "shopWithin" or "bagWithin")
            {
                continue;
            }

            if (slot.Id.StartsWith("eventPick", StringComparison.Ordinal))
            {
                fragments.Add($"event_in{Selected("eventWithin") ?? "1"}={value}");
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

            fragments.Add($"{SpecKeyFor(slot.Id)}={value}");
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
        var values = slotIds.Select(Selected).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        if (values.Length > 0)
        {
            fragments.Add($"{key}={string.Join('+', values)}");
        }
    }

    private static string SpecKeyFor(string slotId) => slotId switch
    {
        "rewardWithin" => "reward_within",
        "shopWithin" => "shop_within",
        "bagWithin" => "bag_within",
        "eventWithin" => "event_within",
        "bonesCurse" => "bones_curse",
        "ancient2Offers" => "ancient2_offers",
        "ancient3Offers" => "ancient3_offers",
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
        new("bagPick1", "relic rewards have", Cluster: "bag"),
        new("bagPick2", "relic rewards have", Cluster: "bag"),
        new("bagPick3", "relic rewards have", Cluster: "bag"),

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
        new[] { "dollroom", "selfhelpbook", "trashheap", "thisorthat", "abyssalbath", "teabmaster", "slipperybridge", "brainleech", "punch_off", "symbiote" }
            .Select(id => new SearchTheSpireOption(id, Humanize(id), "act 1 events"))
            .ToArray();

    public static IReadOnlyList<SearchTheSpireSlot> Slots => SlotTable;

    public static bool ContainsSlot(string slotId) => SlotTable.Any(slot => slot.Id == slotId);

    public static SearchTheSpireSlot GetSlot(string slotId) =>
        SlotTable.FirstOrDefault(slot => slot.Id == slotId) ??
        throw new ArgumentException($"Unknown SearchTheSpire slot '{slotId}'.", nameof(slotId));

    public static bool IsEnabled(SearchTheSpireSlot slot, SearchTheSpireBoardState state)
    {
        if (state.Ascension < slot.RequiresAscension)
        {
            return false;
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
                "bonesCapsuleSet1" or "bonesCapsuleSet2" or "bonesCapsuleSet3" => offer == "neowsbones" && HasAnyGrant(state, "largecapsule", "smallcapsule"),
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
            return ActMaps.Select(option => BlockForBossMap(option, state.Selected("boss1"))).ToArray();
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

        if (slot.Id is "ancient2Offers" or "ancient3Offers") return AncientOffers;
        if (slot.Id is "rewardWithin" or "shopWithin" or "bagWithin" or "eventWithin") return WithinOptions(slot.Id);
        if (slot.Id.StartsWith("eventPick", StringComparison.Ordinal))
        {
            var taken = SearchTheSpireCatalog.Slots.Where(candidate => candidate.Id.StartsWith("eventPick", StringComparison.Ordinal) && candidate.Id != slot.Id)
                .Select(candidate => state.Selected(candidate.Id)).Where(value => value != null).ToHashSet(StringComparer.Ordinal);
            return Events.Select(option => taken.Contains(option.Id)
                ? option with { Blocked = true, BlockReason = "event picks must be distinct" }
                : option).ToArray();
        }

        if (slot.Id.StartsWith("shopPick", StringComparison.Ordinal) || slot.Id.StartsWith("bagPick", StringComparison.Ordinal))
        {
            var prefix = slot.Id.StartsWith("shop", StringComparison.Ordinal) ? "shopPick" : "bagPick";
            var taken = SearchTheSpireCatalog.Slots.Where(candidate => candidate.Id.StartsWith(prefix, StringComparison.Ordinal) && candidate.Id != slot.Id)
                .Select(candidate => state.Selected(candidate.Id)).Where(value => value != null).ToHashSet(StringComparer.Ordinal);
            return Relics.Select(option => taken.Contains(option.Id)
                ? option with { Blocked = true, BlockReason = "repeated relic picks must be distinct" }
                : option).ToArray();
        }

        var source = slot.Id.Contains("Potion", StringComparison.OrdinalIgnoreCase) ||
                     slot.Id.Contains("potion", StringComparison.OrdinalIgnoreCase) ||
                     slot.Id == "cofferPotion" || slot.Id == "phialPotionA" || slot.Id == "phialPotionB"
            ? Potions
            : slot.Id.Contains("Relic", StringComparison.OrdinalIgnoreCase) || slot.Id.Contains("Capsule", StringComparison.OrdinalIgnoreCase)
                ? Relics
                : Cards;

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

    private static SearchTheSpireOption BlockForBossMap(SearchTheSpireOption option, string? act)
    {
        var isOvergrowth = option.Section.Contains("Overgrowth", StringComparison.Ordinal);
        var isUnderdocks = option.Section.Contains("Underdocks", StringComparison.Ordinal);
        var blocked = (act == "0" && isUnderdocks) || (act == "1" && isOvergrowth);
        return blocked ? option with { Blocked = true, BlockReason = "this boss belongs to the other Act 1 map" } : option;
    }

    private static IReadOnlyList<SearchTheSpireOption> BlockDuplicates(IEnumerable<SearchTheSpireOption> options, string? duplicate, string reason) =>
        options.Select(option => option.Id == duplicate ? option with { Blocked = true, BlockReason = reason } : option).ToArray();

    private static IReadOnlyList<SearchTheSpireOption> WithinOptions(string slotId)
    {
        var max = slotId == "eventWithin" ? 5 : 6;
        return Enumerable.Range(1, max).Select(value => new SearchTheSpireOption(value.ToString(), $"within {value}", slotId)).ToArray();
    }

    private static bool HasGrant(SearchTheSpireBoardState state, string id) =>
        state.Selected("bonesGrantA") == id || state.Selected("bonesGrantB") == id;

    private static bool HasAnyGrant(SearchTheSpireBoardState state, params string[] ids) =>
        ids.Any(id => HasGrant(state, id));

    private static string Humanize(string id) =>
        string.Join(' ', id.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Length == 0 ? part : char.ToUpperInvariant(part[0]) + part[1..]))
            .Replace("dollroom", "Doll Room", StringComparison.OrdinalIgnoreCase);
}
