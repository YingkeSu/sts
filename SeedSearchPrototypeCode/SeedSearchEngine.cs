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
        "dollroom", "selfhelpbook", "trashheap", "thisorthat", "abyssalbath", "waterloggedscriptorium",
        "slipperybridge", "brainleech", "punch_off", "symbiote",
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

    public static IReadOnlyList<ulong> NextOutputs(int preseed, int count)
    {
        var state = Sts2ReferenceRng.Create(preseed);
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

        var baseSeed = Sts2ReferenceRng.HashCode(seed);
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

        var bossRng = Sts2ReferenceRng.Create(unchecked((int)(uint)(baseSeed + Sts2ReferenceRng.HashCode("up_front"))));
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
        var ancient2OfferId = AncientOfferIds[Sts2ReferenceRng.NextInt(ref bossRng, AncientOfferIds.Length)];
        var ancient3OfferId = AncientOfferIds[Sts2ReferenceRng.NextInt(ref bossRng, AncientOfferIds.Length)];

        var neowRng = Sts2ReferenceRng.Create(unchecked((int)(uint)(baseSeed + Sts2ReferenceRng.HashCode("NEOW"))));
        var cursedOffer = CursedOffers[Sts2ReferenceRng.NextInt(ref neowRng, CursedOffers.Length)];
        var bonusA = BonusOffers[Sts2ReferenceRng.NextInt(ref neowRng, BonusOffers.Length)];
        var bonusB = BonusOffers[Sts2ReferenceRng.NextInt(ref neowRng, BonusOffers.Length)];
        if (bonusB == bonusA)
        {
            bonusB = BonusOffers[(Array.IndexOf(BonusOffers, bonusB) + 1) % BonusOffers.Length];
        }

        var offer = cursedOffer;
        var grantA = "";
        var grantB = "";
        if (offer == "neowsbones")
        {
            grantA = GrantRelics[Sts2ReferenceRng.NextInt(ref neowRng, GrantRelics.Length)];
            do
            {
                grantB = GrantRelics[Sts2ReferenceRng.NextInt(ref neowRng, GrantRelics.Length)];
            }
            while (grantB == grantA);
        }

        var neowSummary = $"{Humanize(offer)} / {Humanize(bonusA)} / {Humanize(bonusB)}";
        var neow = $"{Humanize(offer)} · {neowSummary}";
        var ancients = $"{Humanize(ancient2Id)} / {Humanize(ancient3Id)}";
        var rewardCardValues = Enumerable.Range(0, 3)
            .Select(_ => Cards[Sts2ReferenceRng.NextInt(ref bossRng, Cards.Length)])
            .ToArray();
        var shopRelicValues = Enumerable.Range(0, 2)
            .Select(_ => RelicIds[Sts2ReferenceRng.NextInt(ref bossRng, RelicIds.Length)])
            .ToArray();
        var bagRelicValues = Enumerable.Range(0, 2)
            .Select(_ => RelicIds[Sts2ReferenceRng.NextInt(ref bossRng, RelicIds.Length)])
            .ToArray();
        var eventValues = Enumerable.Range(0, 2)
            .Select(_ => EventIds[Sts2ReferenceRng.NextInt(ref bossRng, EventIds.Length)])
            .ToArray();
        var rewardCards = string.Join('+', rewardCardValues);
        var shopRelics = string.Join('+', shopRelicValues);
        var bagRelics = string.Join('+', bagRelicValues);
        var eventIds = string.Join('+', eventValues);
        var detailSpec = BuildDetailSpec(offer, grantA, grantB, rewardCardValues, ref neowRng);

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
            detailSpec);
    }

    private static string BuildContext(SeedQuery query) =>
        $"{query.GameApiVersion}|{query.Character}|A{query.Ascension}|{query.RunMode}|{query.HiddenSpec}";

    private static RunCharacter ParseCharacter(string context)
    {
        var token = context.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ElementAtOrDefault(1);
        return Enum.TryParse<RunCharacter>(token, true, out var character)
            ? character
            : RunCharacter.Any;
    }

    private static string BuildDetailSpec(
        string offer,
        string grantA,
        string grantB,
        IReadOnlyList<string> rewardCards,
        ref Sts2ReferenceRng.RngState neowRng)
    {
        var details = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["reward1"] = rewardCards[0],
            ["reward2"] = rewardCards[1],
            ["reward3"] = rewardCards[2],
        };

        if (offer == "neowsbones")
        {
            details["bones_curse"] = TakeValue(Curses, ref neowRng);
            AddOfferDetails(details, grantA, "bones_", ref neowRng);
            AddOfferDetails(details, grantB, "bones_", ref neowRng);
        }
        else
        {
            AddOfferDetails(details, offer, string.Empty, ref neowRng);
        }

        return string.Join(',', details.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}={pair.Value}"));
    }

    private static void AddOfferDetails(
        IDictionary<string, string> details,
        string offer,
        string prefix,
        ref Sts2ReferenceRng.RngState rng)
    {
        if (string.IsNullOrWhiteSpace(offer))
        {
            return;
        }

        switch (offer)
        {
            case "heftytablet":
                details[$"{prefix}tablet_card"] = TakeValue(Cards, ref rng);
                break;
            case "arcanescroll":
                details[$"{prefix}arcane_card"] = TakeValue(Cards, ref rng);
                break;
            case "leadpaperweight":
                details[$"{prefix}paperweight_card"] = TakeValue(Cards, ref rng);
                break;
            case "lostcoffer":
                details[$"{prefix}coffer_card"] = TakeValue(Cards, ref rng);
                details[$"{prefix}coffer_potion"] = TakeValue(Potions, ref rng);
                break;
            case "largecapsule":
                details[prefix.Length == 0 ? "large_relic" : "bones_capsule_set"] =
                    TakeDistinct(RelicIds, prefix.Length == 0 ? 2 : 3, ref rng);
                break;
            case "smallcapsule":
                details[prefix.Length == 0 ? "capsule_relic" : "bones_capsule_set"] =
                    TakeDistinct(RelicIds, prefix.Length == 0 ? 1 : 3, ref rng);
                break;
            case "kaleidoscope":
                details[$"{prefix}kaleido_distinct"] = TakeDistinct(Cards, 2, ref rng);
                break;
            case "newleaf":
                details[$"{prefix}newleaf_card"] = TakeValue(Cards, ref rng);
                break;
            case "scrollboxes":
                details[$"{prefix}scrollbox_contains"] = TakeDistinct(Cards, 3, ref rng);
                break;
            case "leafypoultice":
                details[$"{prefix}poultice_set"] = TakeDistinct(Cards, 2, ref rng);
                break;
            case "phialholster":
                details[$"{prefix}phial_potion"] = TakeDistinct(Potions, 2, ref rng);
                break;
        }
    }

    private static string TakeValue(IReadOnlyList<string> values, ref Sts2ReferenceRng.RngState rng) =>
        values[Sts2ReferenceRng.NextInt(ref rng, values.Count)];

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

        foreach (var fragment in spec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = fragment.IndexOf('=');
            if (separator <= 0 || separator == fragment.Length - 1)
            {
                continue;
            }

            var key = fragment[..separator];
            var expected = fragment[(separator + 1)..];
            if (!MatchesSpecFragment(key, expected, snapshot))
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

    private static bool MatchesSpecFragment(string key, string expected, SeedSnapshot snapshot)
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
            "reward_cards" => ContainsMultiset(snapshot.RewardCardIds, expected),
            "shop_relic" => ContainsMultiset(snapshot.ShopRelicIds, expected),
            "bag_relic" => ContainsMultiset(snapshot.BagRelicIds, expected),
            "event_in1" or "event_in2" or "event_in3" or "event_in4" or "event_in5" => ContainsMultiset(snapshot.EventIds, expected),
            "bones_curse" or "bones_tablet_card" or "bones_arcane_card" or "bones_paperweight_card" or
            "bones_coffer_card" or "bones_coffer_potion" or "bones_newleaf_card" or "bones_capsule_set" or
            "bones_kaleido_distinct" or "bones_scrollbox_contains" or "bones_poultice_set" or
            "tablet_card" or "poultice_set" or "large_relic" or "paperweight_card" or "arcane_card" or
            "coffer_card" or "coffer_potion" or "kaleido_distinct" or "newleaf_card" or "scrollbox_contains" or
            "phial_potion" or "capsule_relic" => MatchesDetail(snapshot, key, expected),
            "reward1" or "reward2" or "reward3" => MatchesDetail(snapshot, key, expected, false),
            _ => false,
        };
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

        public static RngState Create(int preseed)
        {
            var seed = unchecked((ulong)(uint)preseed);
            return new RngState(NextState(ref seed), NextState(ref seed), NextState(ref seed), NextState(ref seed));
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
