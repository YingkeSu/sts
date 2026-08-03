using System.Text;

namespace SeedSearchPrototype;

/// <summary>
/// Deterministic reference engine for the public-beta seed search surface.
///
/// The hash and four-word RNG are the community-decompiled STS2 primitives.
/// Content pools remain version data, so the engine records its backend in every
/// snapshot instead of silently presenting a modded game pool as SearchTheSpire.
/// </summary>
public sealed class SeedSearchEngine
{
    private const string BetaAlphabet = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const int BetaSeedLength = 12;
    private static readonly string[] CursedOffers =
    {
        "cursedpearl", "dowsingrod", "heftytablet", "largecapsule", "leafypoultice",
        "neowsbones", "neowssacrifice", "precariousshears", "silkentress", "silvercrucible",
    };

    private static readonly string[] BonusOffers =
    {
        "arcanescroll", "boomingconch", "fishingrod", "goldenpearl", "kaleidoscope",
        "leadpaperweight", "lostcoffer", "neowstorment", "newleaf", "phialholster",
        "precisescissors", "scrollboxes", "wingedboots", "lavarock", "smallcapsule",
        "nutritiousoyster", "stonehumidifier", "neowstalisman", "pomander",
    };

    private static readonly string[] OvergrowthBosses = { "ceremonialbeast", "thekin", "vantom" };
    private static readonly string[] UnderdocksBosses = { "lagavulinmatriarch", "soulfysh", "waterfallgiant" };
    private static readonly string[] Act2Bosses = { "kaisercrab", "knowledgedemon", "theinsatiable" };
    private static readonly string[] Act3Bosses = { "aeonglass", "queen", "testsubject" };
    private static readonly string[] Act2Ancients = { "orobas", "pael", "tezcatara", "darv" };
    private static readonly string[] Act3Ancients = { "nonupeipe", "tanx", "vakuu", "darv" };
    private static readonly string[] AncientOfferIds =
    {
        "electricshrymp", "glasseye", "prismaticgem", "seaglass", "alchemicalcoffer", "driftwood",
        "radiantpearl", "sandcastle", "paelsflesh", "paelshorn", "paelstears", "paelswing",
        "paelsgrowth", "paelseye", "paelsblood", "veryhotcocoa", "yummycookie", "biiighug",
        "storybook", "toastymittens", "goldencompass", "pumpkincandle", "toybox", "sealofgold",
        "claws", "crossbow", "ironclub", "meatcleaver", "sai", "spikedgauntlets", "tanxswhistle",
        "throwingaxe", "warhammer", "bloodsoakedrose", "whisperingearring", "fiddle", "preservedfog",
        "seretalon", "distinguishedcape", "choicesparadox", "musicbox", "lordsparasol", "jeweledmask",
        "blessedantler", "brilliantscarf", "delicatefrond", "diamonddiadem", "furcoat", "glitter",
        "jewelrybox", "loomingfruit", "signetring",
    };
    private static readonly string[] Cards =
    {
        "strike", "defend", "bash", "ironwave", "neutralize", "backflip", "survivor", "seer",
        "orbit", "dismantle", "zap", "deadly_disease", "soul_fire", "colorless_insight", "dominate",
        "tearasunder", "pyre", "bludgeon", "pommelstrike", "automation", "equilibrium", "thinkingahead",
        "darkshackles",
    };
    private static readonly string[] RelicIds =
    {
        "anchor", "akabeko", "bagofpreparation", "beatingremnant", "bloodvial", "lantern", "kunai",
        "mango", "oldcoin", "unsettlinglamp", "wingedboots", "pomander",
    };
    private static readonly string[] EventIds =
    {
        "aromaofchaos", "byrdonisnest", "densevegetation", "junglemazeadventure", "luminouschoir",
        "morphicgrove", "sapphireseed", "tabletoftruth", "unrestsite", "wellspring", "whisperinghollow",
        "woodcarvings", "abyssalbaths", "doorsoflightanddark", "drowningbeacon", "endlessconveyor",
        "punchoff", "spiralingwhirlpool", "sunkentreasury", "trashheap", "waterloggedscriptorium",
        "sunkenstatue", "brainleech", "roomfullofcheese", "selfhelpbook", "slipperybridge", "teamaster",
        "thefutureofpotions", "thelegendsweretrue", "thisorthat",     };
    private static readonly string[] OvergrowthEvents =
    {
        "aromaofchaos", "byrdonisnest", "densevegetation", "junglemazeadventure", "luminouschoir",
        "morphicgrove", "sapphireseed", "tabletoftruth", "unrestsite", "wellspring", "whisperinghollow",
        "woodcarvings", "sunkenstatue", "brainleech", "roomfullofcheese", "selfhelpbook", "slipperybridge",
        "teamaster", "thefutureofpotions", "thelegendsweretrue", "thisorthat",
    };
    private static readonly string[] UnderdocksEvents =
    {
        "abyssalbaths", "doorsoflightanddark", "drowningbeacon", "endlessconveyor", "punchoff",
        "spiralingwhirlpool", "sunkentreasury", "trashheap", "waterloggedscriptorium", "sunkenstatue",
        "brainleech", "roomfullofcheese", "selfhelpbook", "slipperybridge", "teamaster", "thefutureofpotions",
        "thelegendsweretrue", "thisorthat",
    };
    private static readonly string[] Curses =
    {
        "clumsy", "debt", "decay", "doubt", "guilty", "injury", "normality", "regret", "shame", "writhe",
    };
    private static readonly string[] Potions =
    {
        "fire_potion", "swift_potion", "energy_potion", "focus_potion", "steroid_potion", "attackpotion", "bloodpotion",
    };

    public static int HashCode(string seed) => Sts2ReferenceRng.HashCode(seed);

    public static ulong HashCode64(string seed) => Sts2ReferenceRng.HashCode64(seed);

    public static IReadOnlyList<ulong> NextOutputs(int preseed, int count)
    {
        var state = Sts2ReferenceRng.Create(unchecked((ulong)(uint)preseed));
        var outputs = new ulong[Math.Max(0, count)];
        for (var index = 0; index < outputs.Length; index++)
        {
            outputs[index] = Sts2ReferenceRng.Next(ref state);
        }

        return outputs;
    }

    private static readonly string[] GrantRelics =
    {
        "cursedpearl", "dowsingrod", "heftytablet", "largecapsule", "leafypoultice",
        "neowssacrifice", "precariousshears", "silkentress", "silvercrucible",
        "arcanescroll", "boomingconch", "fishingrod", "goldenpearl", "kaleidoscope",
        "leadpaperweight", "lostcoffer", "neowstorment", "newleaf", "phialholster",
        "precisescissors", "scrollboxes", "wingedboots", "lavarock", "neowstalisman",
        "nutritiousoyster", "pomander", "smallcapsule", "stonehumidifier",
    };

    public static string CreateSeed(SeedBranch branch, long index) => SeedCodec.FromIndex(branch, index);

    public IReadOnlyList<SeedMatch> Search(
        SeedQuery query,
        CancellationToken cancellationToken,
        Action<SearchProgress>? progress = null)
    {
        var matches = new List<SeedMatch>();
        var stopAfter = Math.Clamp(query.StopAfter, 1, 1000);
        var budget = Math.Clamp(query.MaxCandidates, 1, 10_000_000);
        var start = Math.Max(0, query.StartOffset);

        long checkedCount = 0;
        for (long offset = 0; offset < budget && matches.Count < stopAfter; offset++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var seed = SeedCodec.FromIndex(query.Branch, start + offset);
            var snapshot = Inspect(seed, query.Branch, BuildContext(query));
            checkedCount = offset + 1;
            if (Matches(query, snapshot))
            {
                matches.Add(new SeedMatch(seed, snapshot));
            }

            if (offset % 512 == 0 || matches.Count == stopAfter)
            {
                progress?.Invoke(new SearchProgress(offset + 1, budget, matches.Count));
            }
        }

        progress?.Invoke(new SearchProgress(checkedCount, budget, matches.Count));
        return matches;
    }

    public SeedSnapshot Inspect(string rawSeed, SeedBranch branch, string context = "")
    {
        var seed = string.IsNullOrWhiteSpace(rawSeed)
            ? SeedCodec.FromIndex(branch, 0)
            : rawSeed.Trim().ToUpperInvariant();
        var character = ParseCharacter(context);

        var baseSeed = branch == SeedBranch.PublicBeta
            ? Sts2ReferenceRng.HashCode64(seed)
            : unchecked((ulong)(uint)Sts2ReferenceRng.HashCode(seed));
        var mapRng = Sts2ReferenceRng.Create(baseSeed);
        var act1MapId = Sts2ReferenceRng.NextInt(ref mapRng, 2).ToString();
        var eliteCount = 1 + Sts2ReferenceRng.NextInt(ref mapRng, 3);
        var shopCount = Sts2ReferenceRng.NextInt(ref mapRng, 3);
        var restSiteCount = 1 + Sts2ReferenceRng.NextInt(ref mapRng, 3);
        var hasBlessing = Sts2ReferenceRng.NextInt(ref mapRng, 3) != 0;
        var hasCurse = Sts2ReferenceRng.NextInt(ref mapRng, 4) == 0;

        var mapSlots = new[] { 'M', 'M', '?', '$', 'R', 'M', 'E', 'T', '?', 'M', 'B' };
        var map = new StringBuilder();
        foreach (var slot in mapSlots)
        {
            var roll = Sts2ReferenceRng.NextInt(ref mapRng, 100);
            var display = slot;
            if (slot == 'M' && roll < 18)
            {
                display = 'U';
            }
            else if (slot == 'R' && roll < 35)
            {
                display = '$';
            }

            if (map.Length > 0)
            {
                map.Append(' ');
            }

            map.Append(display);
        }

        var bossRng = Sts2ReferenceRng.Create(baseSeed + StreamHash(branch, "up_front"));
        var bossPool = act1MapId == "0" ? OvergrowthBosses : UnderdocksBosses;
        var boss1Id = bossPool[Sts2ReferenceRng.NextInt(ref bossRng, bossPool.Length)];
        var boss2Id = Act2Bosses[Sts2ReferenceRng.NextInt(ref bossRng, Act2Bosses.Length)];
        var boss3Id = Act3Bosses[Sts2ReferenceRng.NextInt(ref bossRng, Act3Bosses.Length)];
        var boss3BId = Act3Bosses[Sts2ReferenceRng.NextInt(ref bossRng, Act3Bosses.Length)];
        if (boss3BId == boss3Id)
        {
            boss3BId = Act3Bosses[(Array.IndexOf(Act3Bosses, boss3BId) + 1) % Act3Bosses.Length];
        }

        var ancient2Id = Act2Ancients[Sts2ReferenceRng.NextInt(ref bossRng, Act2Ancients.Length)];
        var ancient3Id = Act3Ancients[Sts2ReferenceRng.NextInt(ref bossRng, Act3Ancients.Length)];
        var ancient2Offers = SearchTheSpireCatalog.AncientOfferIdsFor(ancient2Id, 2);
        var ancient3Offers = SearchTheSpireCatalog.AncientOfferIdsFor(ancient3Id, 3);
        var ancient2OfferPool = ancient2Offers.Count == 0 ? AncientOfferIds : ancient2Offers;
        var ancient3OfferPool = ancient3Offers.Count == 0 ? AncientOfferIds : ancient3Offers;
        var ancient2OfferId = ancient2OfferPool[Sts2ReferenceRng.NextInt(ref bossRng, ancient2OfferPool.Count)];
        var ancient3OfferId = ancient3OfferPool[Sts2ReferenceRng.NextInt(ref bossRng, ancient3OfferPool.Count)];

        var neowRng = Sts2ReferenceRng.Create(baseSeed + StreamHash(branch, "NEOW"));
        var cursedOffer = CursedOffers[Sts2ReferenceRng.NextInt(ref neowRng, CursedOffers.Length)];
        var bonusPool = BuildNeowBonusPool(cursedOffer, ref neowRng);
        var bonusA = bonusPool[0];
        var bonusB = bonusPool[1];

        var offer = cursedOffer;
        var grantA = "";
        var grantB = "";
        if (offer == "neowsbones")
        {
            var grantPool = GrantRelics.ToList();
            var rewardRng = Sts2ReferenceRng.Create(baseSeed + StreamHash(branch, "rewards"));
            Sts2ReferenceRng.Shuffle(ref rewardRng, grantPool);
            grantA = grantPool[0];
            grantB = grantPool[1];
        }

        var neowSummary = $"{Humanize(offer)} / {Humanize(bonusA)} / {Humanize(bonusB)}";
        var neow = $"{Humanize(offer)} · {neowSummary}";
        var ancients = $"{Humanize(ancient2Id)} / {Humanize(ancient3Id)}";
        var cardPool = CardPool(character);
        var potionPool = PotionPool(character);
        var shopRelicPool = ShopRelicPool(character);
        var capsuleRelicPool = CapsuleRelicPool(character);
        var rewardCardValues = Enumerable.Range(0, 3)
            .Select(_ => cardPool[Sts2ReferenceRng.NextInt(ref bossRng, cardPool.Count)])
            .ToArray();
        var shopRelicValues = Enumerable.Range(0, 2)
            .Select(_ => shopRelicPool[Sts2ReferenceRng.NextInt(ref bossRng, shopRelicPool.Count)])
            .ToArray();
        var bagRelicValues = Enumerable.Range(0, 2)
            .Select(_ => capsuleRelicPool[Sts2ReferenceRng.NextInt(ref bossRng, capsuleRelicPool.Count)])
            .ToArray();
        var eventPool = act1MapId == "0" ? OvergrowthEvents : UnderdocksEvents;
        var eventValues = Enumerable.Range(0, 5)
            .Select(_ => eventPool[Sts2ReferenceRng.NextInt(ref bossRng, eventPool.Length)])
            .ToArray();
        var rewardCards = string.Join('+', rewardCardValues);
        var shopRelics = string.Join('+', shopRelicValues);
        var bagRelics = string.Join('+', bagRelicValues);
        var eventIds = string.Join('+', eventValues);
        var detailSpec = BuildDetailSpec(offer, grantA, grantB, rewardCardValues, cardPool, potionPool, capsuleRelicPool, ref neowRng);

        return new SeedSnapshot(
            seed,
            $"{map}  ·  {eliteCount}E / {shopCount}$ / {restSiteCount}R",
            neow,
            ancients,
            $"{Humanize(boss1Id)} / {Humanize(boss2Id)} / {Humanize(boss3Id)}",
            eliteCount,
            shopCount,
            restSiteCount,
            hasBlessing,
            hasCurse,
            offer,
            grantA,
            grantB,
            "reference-rng",
            act1MapId,
            boss1Id,
            boss2Id,
            boss3Id,
            boss3BId,
            ancient2Id,
            ancient3Id,
            ancient2OfferId,
            ancient3OfferId,
            rewardCards,
            shopRelics,
            bagRelics,
            eventIds,
            character,
            detailSpec,
            ParseAscension(context));
    }

    private static string BuildContext(SeedQuery query) =>
        $"{query.GameApiVersion}|{query.Character}|A{query.Ascension}|{query.RunMode}|{query.HiddenSpec}";

    private static ulong StreamHash(SeedBranch branch, string name) =>
        branch == SeedBranch.PublicBeta
            ? Sts2ReferenceRng.HashCode64(name)
            : unchecked((ulong)(uint)Sts2ReferenceRng.HashCode(name));

    private static List<string> BuildNeowBonusPool(
        string cursedOffer,
        ref Sts2ReferenceRng.RngState rng)
    {
        // The first 13 entries are Neow's always-available positive options.
        // The remaining six entries are the three mutually exclusive pairs
        // added by Neow.GenerateInitialOptions.
        var pool = BonusOffers.Take(13).ToList();
        if (cursedOffer == "cursedpearl")
        {
            pool.Remove("goldenpearl");
        }
        else if (cursedOffer == "heftytablet")
        {
            pool.Remove("arcanescroll");
        }
        else if (cursedOffer == "leafypoultice")
        {
            pool.Remove("newleaf");
        }
        else if (cursedOffer == "precariousshears")
        {
            pool.Remove("precisescissors");
        }
        else if (cursedOffer == "neowssacrifice")
        {
            pool.Remove("phialholster");
            pool.Remove("lostcoffer");
        }

        if (cursedOffer != "largecapsule")
        {
            pool.Add(Sts2ReferenceRng.NextInt(ref rng, 2) == 0 ? "lavarock" : "smallcapsule");
        }

        pool.Add(Sts2ReferenceRng.NextInt(ref rng, 2) == 0 ? "nutritiousoyster" : "stonehumidifier");
        pool.Add(Sts2ReferenceRng.NextInt(ref rng, 2) == 0 ? "neowstalisman" : "pomander");
        Sts2ReferenceRng.Shuffle(ref rng, pool);
        return pool.Take(2).ToList();
    }

    private static IReadOnlyList<string> CardPool(RunCharacter character)
    {
        var versioned = character != RunCharacter.Any && SearchTheSpirePoolData.CardPools.TryGetValue(character, out var own)
            ? own
            : SearchTheSpirePoolData.CardPools.Values.SelectMany(values => values);
        return versioned.Concat(Cards).Distinct(StringComparer.Ordinal).ToArray();
    }

    private static IReadOnlyList<string> PotionPool(RunCharacter character)
    {
        var own = character != RunCharacter.Any && SearchTheSpirePoolData.CharacterPotions.TryGetValue(character, out var values)
            ? values
            : Array.Empty<string>();
        return own.Concat(SearchTheSpirePoolData.SharedPotions).Concat(Potions)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> ShopRelicPool(RunCharacter character)
    {
        var own = character != RunCharacter.Any && SearchTheSpirePoolData.CharacterShopRelics.TryGetValue(character, out var values)
            ? values
            : Array.Empty<string>();
        return SearchTheSpirePoolData.SharedShopRelics.Concat(own).Concat(RelicIds)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> CapsuleRelicPool(RunCharacter character)
    {
        var own = character != RunCharacter.Any && SearchTheSpirePoolData.CharacterCapsuleRelics.TryGetValue(character, out var values)
            ? values
            : Array.Empty<string>();
        return SearchTheSpirePoolData.SharedCapsuleRelics.Concat(own).Concat(RelicIds)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static RunCharacter ParseCharacter(string context)
    {
        var token = context.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ElementAtOrDefault(1);
        return Enum.TryParse<RunCharacter>(token, true, out var character)
            ? character
            : RunCharacter.Any;
    }

    private static int ParseAscension(string context)
    {
        var token = context.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(value => value.StartsWith('A'));
        return token != null && int.TryParse(token[1..], out var ascension)
            ? Math.Clamp(ascension, 0, 20)
            : 0;
    }

    private static string BuildDetailSpec(
        string offer,
        string grantA,
        string grantB,
        IReadOnlyList<string> rewardCards,
        IReadOnlyList<string> cards,
        IReadOnlyList<string> potions,
        IReadOnlyList<string> relics,
        ref Sts2ReferenceRng.RngState neowRng)
    {
        var details = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["reward1_card"] = rewardCards[0],
            ["reward2_card"] = rewardCards[1],
            ["reward3_card"] = rewardCards[2],
        };

        if (offer == "neowsbones")
        {
            details["bones_curse"] = TakeValue(Curses, ref neowRng);
            AddOfferDetails(details, grantA, "bones_", cards, potions, relics, ref neowRng);
            AddOfferDetails(details, grantB, "bones_", cards, potions, relics, ref neowRng);
            var capsulePulls = CapsulePulls(grantA) + CapsulePulls(grantB);
            if (capsulePulls > 0)
            {
                details["bones_capsule_set"] = TakeDistinct(relics, capsulePulls, ref neowRng);
            }
        }
        else
        {
            AddOfferDetails(details, offer, string.Empty, cards, potions, relics, ref neowRng);
        }

        return string.Join(',', details.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}={pair.Value}"));
    }

    private static void AddOfferDetails(
        IDictionary<string, string> details,
        string offer,
        string prefix,
        IReadOnlyList<string> cards,
        IReadOnlyList<string> potions,
        IReadOnlyList<string> relics,
        ref Sts2ReferenceRng.RngState rng)
    {
        if (string.IsNullOrWhiteSpace(offer))
        {
            return;
        }

        switch (offer)
        {
            case "heftytablet":
                details[$"{prefix}tablet_card"] = TakeValue(cards, ref rng);
                break;
            case "arcanescroll":
                details[$"{prefix}arcane_card"] = TakeValue(cards, ref rng);
                break;
            case "leadpaperweight":
                details[$"{prefix}paperweight_card"] = TakeValue(cards, ref rng);
                break;
            case "lostcoffer":
                details[$"{prefix}coffer_card"] = TakeValue(cards, ref rng);
                details[$"{prefix}coffer_potion"] = TakeValue(potions, ref rng);
                break;
            case "largecapsule":
                if (prefix == "bones_")
                {
                    break;
                }

                details[prefix.Length == 0 ? "large_relic" : "bones_capsule_set"] =
                    TakeDistinct(relics, prefix.Length == 0 ? 2 : 3, ref rng);
                break;
            case "smallcapsule":
                if (prefix == "bones_")
                {
                    break;
                }

                details[prefix.Length == 0 ? "capsule_relic" : "bones_capsule_set"] =
                    TakeDistinct(relics, prefix.Length == 0 ? 1 : 3, ref rng);
                break;
            case "kaleidoscope":
                details[$"{prefix}kaleido_distinct"] = TakeDistinct(cards, 2, ref rng);
                break;
            case "newleaf":
                details[$"{prefix}newleaf_card"] = TakeValue(cards, ref rng);
                break;
            case "scrollboxes":
                details[$"{prefix}scrollbox_contains"] = TakeDistinct(cards, 3, ref rng);
                break;
            case "leafypoultice":
                details[$"{prefix}poultice_set"] = TakeDistinct(cards, 2, ref rng);
                break;
            case "phialholster":
                details[$"{prefix}phial_potion"] = TakeDistinct(potions, 2, ref rng);
                break;
        }
    }

    private static string TakeValue(IReadOnlyList<string> values, ref Sts2ReferenceRng.RngState rng) =>
        values[Sts2ReferenceRng.NextInt(ref rng, values.Count)];

    private static int CapsulePulls(string offer) => offer switch
    {
        "smallcapsule" => 1,
        "largecapsule" => 2,
        _ => 0,
    };

    private static string TakeDistinct(
        IReadOnlyList<string> values,
        int count,
        ref Sts2ReferenceRng.RngState rng)
    {
        var selected = new List<string>(count);
        while (selected.Count < count)
        {
            var value = TakeValue(values, ref rng);
            if (!selected.Contains(value, StringComparer.Ordinal))
            {
                selected.Add(value);
            }
        }

        return string.Join('+', selected);
    }

    private static bool Matches(SeedQuery query, SeedSnapshot snapshot)
    {
        if (query.Character != RunCharacter.Any && snapshot.Character != query.Character)
        {
            return false;
        }

        if (snapshot.EliteCount < query.MinimumElites ||
            snapshot.ShopCount < query.MinimumShops ||
            snapshot.RestSiteCount < query.MinimumRestSites)
        {
            return false;
        }

        var neowMatches = query.NeowFilter switch
        {
            NeowFilter.HasBlessing => snapshot.HasBlessing,
            NeowFilter.HasCurse => snapshot.HasCurse,
            _ => true
        };

        return neowMatches &&
               MatchesHiddenSpec(query.HiddenSpec, snapshot) &&
               MatchesNamedFilter(snapshot.Ancients, query.AncientFilter) &&
               MatchesNamedFilter(snapshot.Bosses, query.BossFilter);
    }

    private static bool MatchesHiddenSpec(string spec, SeedSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(spec))
        {
            return true;
        }

        var orderedRewards = false;
        foreach (var fragment in spec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (fragment.Equals("reward_ordered", StringComparison.OrdinalIgnoreCase))
            {
                orderedRewards = true;
                continue;
            }

            if (fragment.Equals("scarcity", StringComparison.OrdinalIgnoreCase))
            {
                if (snapshot.Ascension < 7)
                {
                    return false;
                }

                continue;
            }

            var separator = fragment.IndexOf('=');
            if (separator <= 0 || separator == fragment.Length - 1)
            {
                continue;
            }

            var key = fragment[..separator];
            var expected = fragment[(separator + 1)..];
            if (!MatchesSpecFragment(key, expected, snapshot, orderedRewards))
            {
                return false;
            }
        }

        return true;
    }

    private static bool MatchesNamedFilter(string value, string filter) =>
        string.IsNullOrWhiteSpace(filter) || filter.Equals("Any", StringComparison.OrdinalIgnoreCase) || value.Contains(filter, StringComparison.OrdinalIgnoreCase);

    private static bool ContainsMultiset(string actual, string expectedValue)
    {
        var available = actual.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        foreach (var value in expectedValue.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!available.TryGetValue(value, out var count) || count == 0)
            {
                return false;
            }

            available[value] = count - 1;
        }

        return true;
    }

    private static bool MatchesDetail(SeedSnapshot snapshot, string key, string expected, bool multiset = true)
    {
        var actual = snapshot.DetailSpec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(fragment => fragment.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .FirstOrDefault(parts => parts[0].Equals(key, StringComparison.OrdinalIgnoreCase))?
            [1];
        if (actual == null)
        {
            return false;
        }

        return multiset
            ? ContainsMultiset(actual, expected)
            : actual.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesSpecFragment(string key, string expected, SeedSnapshot snapshot, bool orderedRewards = false)
    {
        return key switch
        {
            // Keep the old local key aliases readable for saved searches.
            "neowOffer" => snapshot.NeowOfferId.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "bonesGrantA" => snapshot.NeowGrantAId.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "bonesGrantB" => snapshot.NeowGrantBId.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "char" => snapshot.Character.ToString().Equals(expected, StringComparison.OrdinalIgnoreCase),
            "neow" => int.TryParse(expected, out var index) && index >= 0 && index < CursedOffers.Length && snapshot.NeowOfferId.Equals(CursedOffers[index], StringComparison.OrdinalIgnoreCase),
            "bonus" => snapshot.NeowOffers.Contains(expected, StringComparison.OrdinalIgnoreCase),
            "act" => snapshot.Act1MapId.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "boss1" => snapshot.Boss1Id.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "boss2" => snapshot.Boss2Id.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "boss3" => snapshot.Boss3Id.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "boss3b" => snapshot.Boss3BId.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "ancient2" => snapshot.Ancient2Id.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "ancient3" => snapshot.Ancient3Id.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "ancient2_offers" or "ancient2_offers_if" => snapshot.Ancient2OfferId.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "ancient3_offers" or "ancient3_offers_if" => snapshot.Ancient3OfferId.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "bones_relic" => ContainsMultiset($"{snapshot.NeowGrantAId}+{snapshot.NeowGrantBId}", expected),
            "reward_cards" => orderedRewards
                ? ContainsSequence(snapshot.RewardCardIds, expected)
                : ContainsMultiset(snapshot.RewardCardIds, expected),
            "reward_within" or "shop_within" or "bag_within" or "event_within" => int.TryParse(expected, out var window) && window > 0,
            "rares" => int.TryParse(expected, out var rares) && rares is >= 1 and <= 6,
            "scarcity" => snapshot.Ascension >= 7,
            "shop_relic" => ContainsMultiset(snapshot.ShopRelicIds, expected),
            "bag_relic" => ContainsMultiset(snapshot.BagRelicIds, expected),
            "event_in1" or "event_in2" or "event_in3" or "event_in4" or "event_in5" => ContainsMultiset(snapshot.EventIds, expected),
            "bones_curse" or "bones_tablet_card" or "bones_arcane_card" or "bones_paperweight_card" or
            "bones_coffer_card" or "bones_coffer_potion" or "bones_newleaf_card" or "bones_capsule_set" or
            "bones_kaleido_distinct" or "bones_scrollbox_contains" or "bones_poultice_set" or
            "tablet_card" or "poultice_set" or "large_relic" or "paperweight_card" or "arcane_card" or
            "coffer_card" or "coffer_potion" or "kaleido_distinct" or "newleaf_card" or "scrollbox_contains" or
            "phial_potion" or "capsule_relic" => MatchesDetail(snapshot, key, expected),
            "reward1" or "reward2" or "reward3" or "reward1_card" or "reward2_card" or "reward3_card" =>
                MatchesDetail(snapshot, key, expected, false) ||
                MatchesDetail(snapshot, key.Replace("_card", string.Empty, StringComparison.Ordinal), expected, false),
            _ => false,
        };
    }

    private static bool ContainsSequence(string actual, string expected)
    {
        var actualValues = actual.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var expectedValues = expected.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (expectedValues.Length == 0 || expectedValues.Length > actualValues.Length)
        {
            return false;
        }

        for (var start = 0; start <= actualValues.Length - expectedValues.Length; start++)
        {
            if (expectedValues.Select((value, index) => actualValues[start + index].Equals(value, StringComparison.OrdinalIgnoreCase)).All(value => value))
            {
                return true;
            }
        }

        return false;
    }

    private static string Humanize(string id) => string.Join(' ', id.Split('_', StringSplitOptions.RemoveEmptyEntries)
        .Select(part => part.Length == 0 ? part : char.ToUpperInvariant(part[0]) + part[1..]));

    private static class SeedCodec
    {
        public static string FromIndex(SeedBranch branch, long index)
        {
            if (branch == SeedBranch.Main)
            {
                return unchecked((uint)index).ToString();
            }

            var value = unchecked((ulong)Math.Max(0, index));
            var buffer = new char[BetaSeedLength];
            for (var position = buffer.Length - 1; position >= 0; position--)
            {
                buffer[position] = BetaAlphabet[(int)(value % (ulong)BetaAlphabet.Length)];
                value /= (ulong)BetaAlphabet.Length;
            }

            return new string(buffer);
        }
    }

    private static class Sts2ReferenceRng
    {
        public static int HashCode(string value)
        {
            uint first = 352654597;
            uint second = first;
            for (var index = 0; index < value.Length; index += 2)
            {
                first = unchecked((first * 33) ^ value[index]);
                if (index + 1 >= value.Length)
                {
                    break;
                }

                second = unchecked((second * 33) ^ value[index + 1]);
            }

            return unchecked((int)(first + second * 1566083941u));
        }

        public static ulong HashCode64(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            var offset = 0;
            var length = bytes.Length;
            ulong hash;

            if (length >= 32)
            {
                var v1 = unchecked(Prime5 + Prime1 + Prime2);
                var v2 = unchecked(Prime5 + Prime2);
                var v3 = Prime5;
                var v4 = unchecked(Prime5 - Prime1);
                var limit = length - 32;
                while (offset <= limit)
                {
                    v1 = Round(v1, ReadUInt64(bytes, offset));
                    offset += 8;
                    v2 = Round(v2, ReadUInt64(bytes, offset));
                    offset += 8;
                    v3 = Round(v3, ReadUInt64(bytes, offset));
                    offset += 8;
                    v4 = Round(v4, ReadUInt64(bytes, offset));
                    offset += 8;
                }

                hash = RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);
                hash = MergeRound(hash, v1);
                hash = MergeRound(hash, v2);
                hash = MergeRound(hash, v3);
                hash = MergeRound(hash, v4);
            }
            else
            {
                hash = Prime5;
            }

            hash += (ulong)length;
            while (offset + 8 <= length)
            {
                hash ^= Round(0, ReadUInt64(bytes, offset));
                hash = RotateLeft(hash, 27) * Prime1 + Prime4;
                offset += 8;
            }

            if (offset + 4 <= length)
            {
                hash ^= ReadUInt32(bytes, offset) * Prime1;
                hash = RotateLeft(hash, 23) * Prime2 + Prime3;
                offset += 4;
            }

            while (offset < length)
            {
                hash ^= bytes[offset] * Prime5;
                hash = RotateLeft(hash, 11) * Prime1;
                offset++;
            }

            hash ^= hash >> 33;
            hash *= Prime2;
            hash ^= hash >> 29;
            hash *= Prime3;
            return hash ^ (hash >> 32);
        }

        public static RngState Create(ulong preseed)
        {
            var seed = preseed;
            return new RngState(NextState(ref seed), NextState(ref seed), NextState(ref seed), NextState(ref seed));
        }

        public static void Shuffle(ref RngState state, IList<string> values)
        {
            for (var index = values.Count - 1; index > 0; index--)
            {
                var other = NextInt(ref state, index + 1);
                (values[index], values[other]) = (values[other], values[index]);
            }
        }

        public static int NextInt(ref RngState state, int max)
        {
            if (max <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(max));
            }

            var sample = Next(ref state) >> 11;
            return (int)((sample * (ulong)max) / 9_007_199_254_740_992UL);
        }

        private static ulong NextState(ref ulong seed)
        {
            seed = unchecked(seed + 11400714819323198485UL);
            var value = seed;
            value = unchecked((value ^ (value >> 30)) * 13787848793156543929UL);
            value = unchecked((value ^ (value >> 27)) * 10723151780598845931UL);
            return value ^ (value >> 31);
        }

        private static ulong Round(ulong accumulator, ulong input) =>
            RotateLeft(accumulator + input * Prime2, 31) * Prime1;

        private static ulong MergeRound(ulong accumulator, ulong value) =>
            (accumulator ^ Round(0, value)) * Prime1 + Prime4;

        private static ulong ReadUInt64(byte[] bytes, int offset)
        {
            ulong value = 0;
            for (var index = 0; index < 8; index++)
            {
                value |= (ulong)bytes[offset + index] << (index * 8);
            }

            return value;
        }

        private static uint ReadUInt32(byte[] bytes, int offset) =>
            (uint)(bytes[offset]
                | (bytes[offset + 1] << 8)
                | (bytes[offset + 2] << 16)
                | (bytes[offset + 3] << 24));

        private static ulong RotateLeft(ulong value, int bits) =>
            (value << bits) | (value >> (64 - bits));

        private const ulong Prime1 = 11400714785074694791UL;
        private const ulong Prime2 = 14029467366897019727UL;
        private const ulong Prime3 = 1609587929392839161UL;
        private const ulong Prime4 = 9650029242287828579UL;
        private const ulong Prime5 = 2870177450012600261UL;

        public static ulong Next(ref RngState state)
        {
            var product = unchecked(state.S1 * 5UL);
            var result = unchecked((((product << 7) | (product >> 57)) * 9UL));
            var temporary = state.S1 << 17;
            state.S2 ^= state.S0;
            state.S3 ^= state.S1;
            state.S1 ^= state.S2;
            state.S0 ^= state.S3;
            state.S2 ^= temporary;
            state.S3 = (state.S3 << 45) | (state.S3 >> 19);
            return result;
        }

        public struct RngState
        {
            public RngState(ulong s0, ulong s1, ulong s2, ulong s3)
            {
                S0 = s0;
                S1 = s1;
                S2 = s2;
                S3 = s3;
            }

            public ulong S0;
            public ulong S1;
            public ulong S2;
            public ulong S3;
        }
    }
}
