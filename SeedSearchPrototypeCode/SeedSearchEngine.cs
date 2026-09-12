using System.Collections.Concurrent;

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
    /// <summary>
    /// Single source of truth for the pinned public-beta game version. The
    /// manifest, UI labels and tests assert against this constant so a future
    /// game update cannot silently drift the search surface.
    /// </summary>
    public const string PinnedGameApiVersion = "0.110.1";

    private const string BetaAlphabet = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const int BetaSeedLength = 12;
    private const long PublicBetaSeedCount = 2_386_420_683_693_101_056L; // 34^12
    private const long MainSeedCount = 4_294_967_296L; // uint seed space
    private static readonly string[] CursedOffers =
    {
        "cursedpearl", "dowsingrod", "heftytablet", "largecapsule", "leafypoultice",
        "neowsbones", "neowssacrifice", "precariousshears", "silkentress", "silvercrucible",
    };

    private static readonly string[] BonusOffers =
    {
        "arcanescroll", "boomingconch", "fishingrod", "goldenpearl", "kaleidoscope",
        "leadpaperweight", "lostcoffer", "massivescroll", "neowstorment", "newleaf",
        "phialholster", "precisescissors", "scrollboxes", "wingedboots", "lavarock",
        "smallcapsule", "nutritiousoyster", "stonehumidifier", "neowstalisman", "pomander",
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

    // Encounter tag flags from the community v0.107.1 decompile. The run's
    // UpFront stream consumes the weak/regular encounter deques before the
    // boss/ancient rolls; rejection depends on these shared tags, so the
    // values must stay exactly in decompile order.
    private const int CombatBurrower = 0x100;
    private const int CombatChomper = 0x200;
    private const int CombatNibbit = 0x400;
    private const int CombatShrinker = 0x800;
    private const int CombatSlimes = 0x1000;
    private const int CombatThieves = 0x2000;
    private const int CombatWorkers = 0x4000;
    private const int CombatCrawler = 0x8000;
    private const int CombatMushroom = 0x10000;
    private const int CombatKnights = 0x20000;
    private const int CombatScrolls = 0x40000;
    private const int CombatSeapunk = 0x80000;
    private const int CombatSlugs = 0x100000;
    private const int CombatExoskeletons = 0x200000;
    private const int CombatJaxfruit = 0x400000;

    private static readonly int[] OvergrowthEasyCombats =
    {
        CombatCrawler, 1 | CombatNibbit, 2 | CombatShrinker, 3 | CombatSlimes,
    };

    private static readonly int[] OvergrowthHardCombats =
    {
        4, 5 | CombatMushroom | CombatSlimes, 6, 7, 8, 9,
        10 | CombatShrinker | CombatCrawler, 11, 12 | CombatSlimes,
        13 | CombatSlimes | CombatJaxfruit, 14 | CombatMushroom | CombatJaxfruit, 15,
    };

    private static readonly int[] UnderdocksEasyCombats =
    {
        CombatSlugs, 1 | CombatSeapunk, 2, 3,
    };

    private static readonly int[] UnderdocksHardCombats =
    {
        4 | CombatSlugs, 5, 6, 7, 8, 9, 10, 11 | CombatSeapunk, 12, 13,
    };

    private static readonly int[] HiveEasyCombats =
    {
        CombatWorkers, 1 | CombatExoskeletons, 2 | CombatThieves, 3 | CombatBurrower,
    };

    private static readonly int[] HiveHardCombats =
    {
        4 | CombatWorkers, 5 | CombatChomper, 6 | CombatExoskeletons, 7, 8, 9, 10,
        11 | CombatWorkers, 12, 13,
    };

    private static readonly int[] GloryEasyCombats =
    {
        0, 1 | CombatScrolls, 2,
    };

    private static readonly int[] GloryHardCombats =
    {
        3, 4, 5, 6, 7, 8, 9 | CombatScrolls, 10, 11,
    };
    internal static readonly string[] OvergrowthEvents =
    {
        "aromaofchaos", "byrdonisnest", "densevegetation", "junglemazeadventure", "luminouschoir",
        "morphicgrove", "sapphireseed", "tabletoftruth", "unrestsite", "wellspring", "whisperinghollow",
        "woodcarvings", "sunkenstatue", "brainleech", "roomfullofcheese", "selfhelpbook", "slipperybridge",
        "teamaster", "thefutureofpotions", "thelegendsweretrue", "thisorthat",
    };
    internal static readonly string[] UnderdocksEvents =
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
        "leadpaperweight", "lostcoffer", "massivescroll", "neowstorment", "newleaf",
        "phialholster", "precisescissors", "scrollboxes", "wingedboots", "lavarock",
        "neowstalisman", "nutritiousoyster", "pomander", "smallcapsule", "stonehumidifier",
    };

    public static string CreateSeed(SeedBranch branch, long index) => SeedCodec.FromIndex(branch, index);

    public static long SeedCount(SeedBranch branch) =>
        branch == SeedBranch.PublicBeta ? PublicBetaSeedCount : MainSeedCount;

    /// <summary>
    /// Picks a deterministic-range default search start that stays away from
    /// the seed space edges, so consecutive searches do not keep producing
    /// near-zero seeds. The offset is resolved once per search so the exact
    /// scanned window stays reproducible in saved queries.
    /// </summary>
    public static long PickRandomStartOffset(SeedBranch branch, long budget)
    {
        var total = SeedCount(branch);
        var safeLow = total / 10;
        var safeBudget = Math.Max(1, Math.Min(budget, total));
        var safeHigh = total - safeLow - safeBudget;
        if (safeHigh <= safeLow)
        {
            // A full-space budget cannot stay clear of the far end; keep it
            // clear of the beginning and let Search wrap past the end.
            safeHigh = total - safeLow - 1;
        }

        if (safeHigh <= safeLow)
        {
            safeHigh = total - 1;
        }

        return Random.Shared.NextInt64(safeLow, safeHigh + 1);
    }

    /// <summary>
    /// Scans candidates in parallel waves and returns the first StopAfter
    /// matches in candidate-index order. When <paramref name="onMatch"/> is
    /// supplied, it is invoked once per match, in that same deterministic
    /// order, as soon as the wave that found the match merges — before the
    /// search completes. The callback runs on a search worker thread: it must
    /// be thread-safe and must never touch UI nodes; marshal to the main
    /// thread at the call site (for example via a concurrent queue plus
    /// CallDeferred). At most StopAfter callbacks are ever invoked.
    /// </summary>
    public IReadOnlyList<SeedMatch> Search(
        SeedQuery query,
        CancellationToken cancellationToken,
        Action<SearchProgress>? progress = null,
        Action<SeedMatch>? onMatch = null)
    {
        const long chunkSize = 1024;

        var stopAfter = Math.Clamp(query.StopAfter, 1, 1000);
        var seedCount = SeedCount(query.Branch);
        var budget = (long)Math.Min(Math.Max(1, query.MaxCandidates), seedCount);
        var start = Math.Clamp(query.StartOffset, 0, seedCount - 1);
        var context = BuildContext(query);
        var plan = BuildPlan(query);
        var useFastPath = query.Branch == SeedBranch.PublicBeta && !plan.NeedsDetail;
        var workerCount = Math.Max(1, Environment.ProcessorCount);
        var totalChunks = (budget + chunkSize - 1) / chunkSize;
        var results = new List<SeedMatch>();
        var checkedTotal = 0L;

        // Search contiguous candidate blocks in parallel waves, then merge the
        // blocks in chunk order. A block never needs more than stopAfter local
        // matches, and the wave boundary gives deterministic early stopping
        // without changing which matches are the first stopAfter by index.
        // The merge is also the streaming boundary: each match is handed to
        // onMatch (on this worker thread) the moment it enters the ordered
        // result list, so a consumer can render rows while the scan continues.
        for (long waveStart = 0; waveStart < totalChunks && results.Count < stopAfter; waveStart += workerCount)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var waveEnd = Math.Min(totalChunks, waveStart + workerCount);
            var waveLength = (int)(waveEnd - waveStart);
            var waveMatches = new List<SeedMatch>[waveLength];
            var waveChecked = new long[waveLength];

            try
            {
                Parallel.For(
                    waveStart,
                    waveEnd,
                    new ParallelOptions
                    {
                        CancellationToken = cancellationToken,
                        MaxDegreeOfParallelism = workerCount,
                    },
                    chunkIndex =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var local = new List<SeedMatch>();
                        var offsetStart = chunkIndex * chunkSize;
                        var offsetEnd = Math.Min(budget, offsetStart + chunkSize);
                        long localChecked = 0;
                        for (var offset = offsetStart; offset < offsetEnd; offset++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var candidateIndex = start + offset;
                            if (candidateIndex >= seedCount)
                            {
                                candidateIndex -= seedCount;
                            }

                            SeedMatch? match = null;
                            if (useFastPath)
                            {
                                var baseSeed = HashBetaSeed(candidateIndex);
                                if (FastMatch(
                                        query,
                                        plan,
                                        baseSeed,
                                        query.Ascension,
                                        query.Character,
                                        out _))
                                {
                                    var seed = SeedCodec.FromIndex(query.Branch, candidateIndex);
                                    match = new SeedMatch(seed, Inspect(seed, query.Branch, context));
                                }
                            }
                            else
                            {
                                var seed = SeedCodec.FromIndex(query.Branch, candidateIndex);
                                var needsMap = query.MinimumElites > 0 ||
                                               query.MinimumShops > 0 ||
                                               query.MinimumRestSites > 0;
                                var snapshot = Inspect(seed, query.Branch, context, needsMap);
                                if (Matches(query, snapshot))
                                {
                                    var full = snapshot.Map == null
                                        ? Inspect(seed, query.Branch, context)
                                        : snapshot;
                                    match = new SeedMatch(seed, full);
                                }
                            }

                            localChecked = offset - offsetStart + 1;
                            if (match != null)
                            {
                                local.Add(match);
                                if (local.Count >= stopAfter)
                                {
                                    break;
                                }
                            }
                        }

                        waveMatches[chunkIndex - waveStart] = local;
                        waveChecked[chunkIndex - waveStart] = localChecked;
                    });
            }
            catch (AggregateException aggregate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw aggregate.InnerExceptions.Count == 1
                    ? aggregate.InnerExceptions[0]
                    : aggregate;
            }

            for (var chunk = 0; chunk < waveLength; chunk++)
            {
                checkedTotal += waveChecked[chunk];
                var waveChunk = waveMatches[chunk];
                for (var matchIndex = 0; matchIndex < waveChunk.Count && results.Count < stopAfter; matchIndex++)
                {
                    var match = waveChunk[matchIndex];
                    results.Add(match);
                    onMatch?.Invoke(match);
                }
            }

            progress?.Invoke(new SearchProgress(checkedTotal, budget, Math.Min(results.Count, stopAfter)));
        }

        if (results.Count > stopAfter)
        {
            results.RemoveRange(stopAfter, results.Count - stopAfter);
        }

        progress?.Invoke(new SearchProgress(checkedTotal, budget, results.Count));
        return results;
    }

    public SeedSnapshot Inspect(
        string rawSeed,
        SeedBranch branch,
        string context = "",
        bool generateMap = true)
    {
        var seed = string.IsNullOrWhiteSpace(rawSeed)
            ? SeedCodec.FromIndex(branch, 0)
            : rawSeed.Trim().ToUpperInvariant();
        var character = ParseCharacter(context);

        var baseSeed = branch == SeedBranch.PublicBeta
            ? Sts2ReferenceRng.HashCode64(seed)
            : unchecked((ulong)(uint)Sts2ReferenceRng.HashCode(seed));

        var actSelectionRng = Sts2ReferenceRng.Create(baseSeed + StreamHash(branch, "act_selection"));
        var act1MapId = Sts2ReferenceRng.NextInt(ref actSelectionRng, 2).ToString();
        var ascension = ParseAscension(context);

        // Bosses, ancients and the A10 second boss are rolled from the
        // UpFront stream only after the shared/player relic shuffles, the
        // shared-ancient assignment, and each act's event/combat/elite deque
        // consumption. Order and counts mirror RunManager.GenerateRooms and
        // ActModel.GenerateRooms (v0.110.1 decompile).
        var layoutRng = Sts2ReferenceRng.Create(baseSeed + StreamHash(branch, "up_front"));
        Advance(ref layoutRng, 29 + 24 + 34 + 24 + 1);
        Advance(ref layoutRng, 31 + 25 + 37 + 25);
        var darv2 = Sts2ReferenceRng.NextInt(ref layoutRng, 2);
        var darv3 = Sts2ReferenceRng.NextInt(ref layoutRng, 2) != 0 && darv2 == 0;

        // Act 1.
        Advance(ref layoutRng, (act1MapId == "1" ? 10 : 13) + 18 - 1);
        var act1Easy1 = Sts2ReferenceRng.NextInt(ref layoutRng, 4);
        var act1Easy2 = Sts2ReferenceRng.NextInt(ref layoutRng, 3);
        var act1Easy3 = Sts2ReferenceRng.NextInt(ref layoutRng, 2);
        AdjustDistinctAct1Easy(ref act1Easy1, ref act1Easy2, ref act1Easy3);
        var previousCombat = (act1MapId == "1" ? UnderdocksEasyCombats : OvergrowthEasyCombats)[act1Easy3];
        ConsumeHardPool(
            ref layoutRng,
            act1MapId == "1" ? UnderdocksHardCombats : OvergrowthHardCombats,
            12,
            ref previousCombat);
        AdvanceElites(ref layoutRng);
        var boss1Id = (act1MapId == "1" ? UnderdocksBosses : OvergrowthBosses)
            [Sts2ReferenceRng.NextInt(ref layoutRng, 3)];
        Advance(ref layoutRng, 1); // Act 1's ancient is always Neow.

        MapLayout? layout = null;
        var eliteCount = -1;
        var shopCount = -1;
        var restSiteCount = -1;
        var mapSummary = "";
        if (generateMap)
        {
            var mapStreamSeed = unchecked(baseSeed + StreamHash(branch, "act_1_map"));
            layout = ReferenceActMap.Generate(act1MapId, mapStreamSeed, ascension, boss1Id);
            eliteCount = layout.Nodes.Count(node => node.Kind == "elite");
            shopCount = layout.Nodes.Count(node => node.Kind == "shop");
            restSiteCount = layout.Nodes.Count(node => node.Kind == "rest");
            mapSummary = $"{(act1MapId == "1" ? "Underdocks" : "Overgrowth")} · " +
                         $"{layout.Nodes.Count} nodes · {eliteCount}E / {shopCount}$ / {restSiteCount}R";
        }

        // Act 2.
        Advance(ref layoutRng, 10 + 18 - 1);
        var act2Easy1 = Sts2ReferenceRng.NextInt(ref layoutRng, 4);
        var act2Easy2 = Sts2ReferenceRng.NextInt(ref layoutRng, 3);
        if (act2Easy2 >= act2Easy1)
        {
            act2Easy2 += 1;
        }
        previousCombat = HiveEasyCombats[act2Easy2];
        ConsumeHardPool(ref layoutRng, HiveHardCombats, 12, ref previousCombat);
        AdvanceElites(ref layoutRng);
        var boss2Id = Act2Bosses[Sts2ReferenceRng.NextInt(ref layoutRng, Act2Bosses.Length)];
        var ancient2Id = Act2Ancients[Sts2ReferenceRng.NextInt(ref layoutRng, 3 + (darv2 != 0 ? 1 : 0))];

        // Act 3.
        Advance(ref layoutRng, 7 + 18 - 1);
        var act3Easy1 = Sts2ReferenceRng.NextInt(ref layoutRng, 3);
        var act3Easy2 = Sts2ReferenceRng.NextInt(ref layoutRng, 2);
        if (act3Easy2 >= act3Easy1)
        {
            act3Easy2 += 1;
        }
        previousCombat = GloryEasyCombats[act3Easy2];
        ConsumeHardPool(ref layoutRng, GloryHardCombats, 11, ref previousCombat);
        AdvanceElites(ref layoutRng);
        var boss3Id = Act3Bosses[Sts2ReferenceRng.NextInt(ref layoutRng, Act3Bosses.Length)];
        var ancient3Id = Act3Ancients[Sts2ReferenceRng.NextInt(ref layoutRng, 3 + (darv3 ? 1 : 0))];
        var secondBossPool = Act3Bosses
            .Where(id => !id.Equals(boss3Id, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var boss3BId = secondBossPool[Sts2ReferenceRng.NextInt(ref layoutRng, secondBossPool.Length)];

        var ancient2Offers = AncientOfferPool(ancient2Id, 2);
        var ancient3Offers = AncientOfferPool(ancient3Id, 3);
        var ancient2OfferPool = ancient2Offers.Count == 0 ? AncientOfferIds : ancient2Offers;
        var ancient3OfferPool = ancient3Offers.Count == 0 ? AncientOfferIds : ancient3Offers;
        var ancient2OfferId = ancient2OfferPool[Sts2ReferenceRng.NextInt(ref layoutRng, ancient2OfferPool.Count)];
        var ancient3OfferId = ancient3OfferPool[Sts2ReferenceRng.NextInt(ref layoutRng, ancient3OfferPool.Count)];

        var neowRng = Sts2ReferenceRng.Create(baseSeed + StreamHash(branch, "NEOW"));
        var rewardsRng = Sts2ReferenceRng.Create(baseSeed + StreamHash(branch, "rewards"));
        var nicheRng = Sts2ReferenceRng.Create(baseSeed + StreamHash(branch, "niche"));
        var transformationsRng = Sts2ReferenceRng.Create(baseSeed + StreamHash(branch, "transformations"));
        var combatPotionRng = Sts2ReferenceRng.Create(baseSeed + StreamHash(branch, "combat_potion_generation"));
        var cursedOffer = CursedOffers[Sts2ReferenceRng.NextInt(ref neowRng, CursedOffers.Length)];
        var bonusPool = BuildNeowBonusPool(cursedOffer, ref neowRng);
        var bonusA = bonusPool[0];
        var bonusB = bonusPool[1];

        var offer = cursedOffer;
        var grantA = "";
        var grantB = "";
        // Neow always shows two bonus offers and one cursed offer, so both
        // category flags are present on every candidate. The specific pinned
        // offer (if any) is enforced by HiddenSpec via `neow=` / `bonus=`.
        var hasCurse = true;
        var hasBlessing = true;
        if (offer == "neowsbones")
        {
            var grantPool = GrantRelics.ToList();
            Sts2ReferenceRng.Shuffle(ref rewardsRng, grantPool);
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
        var cardDetailPools = new CardDetailPools(
            Rollable: cardPool,
            Rare: RareCardPool(character),
            CommonUncommon: CommonUncommonCardPool(character),
            OtherCharacters: OtherCharacterCardPool(character));
        var rewardCardValues = Enumerable.Range(0, 3)
            .Select(_ => cardPool[Sts2ReferenceRng.NextInt(ref layoutRng, cardPool.Count)])
            .ToArray();
        var shopRelicValues = Enumerable.Range(0, 2)
            .Select(_ => shopRelicPool[Sts2ReferenceRng.NextInt(ref layoutRng, shopRelicPool.Count)])
            .ToArray();
        var bagRelicValues = Enumerable.Range(0, 2)
            .Select(_ => capsuleRelicPool[Sts2ReferenceRng.NextInt(ref layoutRng, capsuleRelicPool.Count)])
            .ToArray();
        var eventPool = act1MapId == "0" ? OvergrowthEvents : UnderdocksEvents;
        var eventValues = Enumerable.Range(0, 5)
            .Select(_ => eventPool[Sts2ReferenceRng.NextInt(ref layoutRng, eventPool.Length)])
            .ToArray();
        var rewardCards = string.Join('+', rewardCardValues);
        var shopRelics = string.Join('+', shopRelicValues);
        var bagRelics = string.Join('+', bagRelicValues);
        var eventIds = string.Join('+', eventValues);
        var kaleidoDistinct = bonusA == "kaleidoscope" || bonusB == "kaleidoscope"
            ? SimulateKaleidoCards(baseSeed, branch, character, ascension)
            : null;
        var detailSpec = BuildDetailSpec(
            offer,
            grantA,
            grantB,
            rewardCardValues,
            cardDetailPools,
            potionPool,
            capsuleRelicPool,
            ref rewardsRng,
            ref nicheRng,
            ref transformationsRng,
            ref combatPotionRng,
            character,
            ascension,
            kaleidoDistinct);

        return new SeedSnapshot(
            seed,
            mapSummary,
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
            ascension,
            layout);
    }

    private static string BuildContext(SeedQuery query) =>
        $"{query.GameApiVersion}|{query.Character}|A{query.Ascension}|{query.RunMode}|{query.HiddenSpec}";

    private sealed class SearchPlan
    {
        public bool NeedsMap;
        public bool NeedsNeow;
        public bool NeedsLayout;
        public bool NeedsDetail;
        public int LayoutDepth;
        public List<SpecFragment> Fragments = new();
        public string CharacterName = "";
    }

    private readonly record struct SpecFragment(string Key, string Expected, bool HasExpected);

    internal struct CandidateData
    {
        public CandidateData()
        {
            Boss1Id = "";
            Boss2Id = "";
            Boss3Id = "";
            Boss3BId = "";
            Ancient2Id = "";
            Ancient3Id = "";
            Ancient2OfferId = "";
            Ancient3OfferId = "";
            NeowOfferId = "";
            NeowGrantAId = "";
            NeowGrantBId = "";
            BonusAId = "";
            BonusBId = "";
        }

        public int Act1MapId;
        public int EliteCount;
        public int ShopCount;
        public int RestSiteCount;
        public string Boss1Id = "";
        public string Boss2Id = "";
        public string Boss3Id = "";
        public string Boss3BId = "";
        public string Ancient2Id = "";
        public string Ancient3Id = "";
        public string Ancient2OfferId = "";
        public string Ancient3OfferId = "";
        public string NeowOfferId = "";
        public string NeowGrantAId = "";
        public string NeowGrantBId = "";
        public string BonusAId = "";
        public string BonusBId = "";
        public bool HasMap;
    }

    private static readonly ConcurrentDictionary<(string Ancient, int Act), IReadOnlyList<string>>
        AncientOfferPools = new();

    private static IReadOnlyList<string> AncientOfferPool(string ancient, int act) =>
        ancient.Length == 0
            ? Array.Empty<string>()
            : AncientOfferPools.GetOrAdd(
                (ancient, act),
                key => SearchTheSpireCatalog.AncientOfferIdsFor(key.Ancient, key.Act));

    private static SearchPlan BuildPlan(SeedQuery query)
    {
        var plan = new SearchPlan
        {
            NeedsMap = query.MinimumElites > 0 ||
                       query.MinimumShops > 0 ||
                       query.MinimumRestSites > 0,
            NeedsLayout = !IsAnyFilter(query.AncientFilter) || !IsAnyFilter(query.BossFilter),
            LayoutDepth = !IsAnyFilter(query.AncientFilter) || !IsAnyFilter(query.BossFilter) ? 3 : 0,
            CharacterName = query.Character.ToString(),
        };
        if (plan.NeedsMap)
        {
            plan.LayoutDepth = Math.Max(plan.LayoutDepth, 1);
        }

        if (!string.IsNullOrWhiteSpace(query.HiddenSpec))
        {
            foreach (var fragment in query.HiddenSpec.Split(
                         ',',
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var separator = fragment.IndexOf('=');
                var key = separator > 0 ? fragment[..separator] : fragment;
                var expected = separator > 0 ? fragment[(separator + 1)..] : "";
                plan.Fragments.Add(new SpecFragment(key, expected, separator > 0));
                plan.NeedsNeow |= SpecNeedsNeow(key);
                plan.NeedsLayout |= SpecNeedsLayout(key);
                plan.NeedsDetail |= SpecNeedsDetail(key);
                plan.LayoutDepth = Math.Max(plan.LayoutDepth, SpecLayoutDepth(key));
            }
        }

        if (plan.NeedsMap || plan.NeedsDetail)
        {
            plan.NeedsLayout = true;
            plan.LayoutDepth = Math.Max(plan.LayoutDepth, plan.NeedsDetail ? 3 : 1);
        }

        return plan;
    }

    private static bool IsAnyFilter(string filter) =>
        string.IsNullOrWhiteSpace(filter) ||
        filter.Equals("Any", StringComparison.OrdinalIgnoreCase) ||
        filter is "任意" or "任何";

    private static bool SpecNeedsNeow(string key) =>
        key is "neow" or "neowOffer" or "bonus" or "bonesGrantA" or "bonesGrantB" or "bones_relic";

    private static bool SpecNeedsLayout(string key) =>
        key is "act" or "boss1" or "boss2" or "boss3" or "boss3b" or
            "ancient2" or "ancient3" or "ancient2_offers" or "ancient2_offers_if" or
            "ancient3_offers" or "ancient3_offers_if";

    private static int SpecLayoutDepth(string key) => key switch
    {
        "boss1" => 1,
        "boss2" or "ancient2" => 2,
        "boss3" or "boss3b" or "ancient3" or
            "ancient2_offers" or "ancient2_offers_if" or
            "ancient3_offers" or "ancient3_offers_if" => 3,
        _ => 0,
    };

    private static bool SpecNeedsDetail(string key) =>
        key is "reward_cards" or "reward1" or "reward2" or "reward3" or
            "reward1_card" or "reward2_card" or "reward3_card" or
            "shop_relic" or "bag_relic" or
            "event_in1" or "event_in2" or "event_in3" or "event_in4" or "event_in5" or
            "bones_curse" or "bones_tablet_card" or "bones_arcane_card" or
            "bones_paperweight_card" or "bones_coffer_card" or "bones_coffer_potion" or
            "bones_newleaf_card" or "bones_capsule_set" or "bones_kaleido_distinct" or
            "bones_scrollbox_contains" or "bones_poultice_set" or
            "tablet_card" or "poultice_set" or "large_relic" or "paperweight_card" or
            "arcane_card" or "coffer_card" or "coffer_potion" or "kaleido_distinct" or
            "newleaf_card" or "scrollbox_contains" or "phial_potion" or "capsule_relic";

    private static bool FastMatch(
        SeedQuery query,
        SearchPlan plan,
        ulong baseSeed,
        int ascension,
        RunCharacter character,
        out CandidateData data)
    {
        data = new CandidateData
        {
            Act1MapId = -1,
            EliteCount = -1,
            ShopCount = -1,
            RestSiteCount = -1,
        };
        if (plan.NeedsMap || plan.NeedsLayout)
        {
            var actSelectionRng = Sts2ReferenceRng.Create(baseSeed + StreamHash(query.Branch, "act_selection"));
            data.Act1MapId = Sts2ReferenceRng.NextInt(ref actSelectionRng, 2);
        }

        if (plan.NeedsNeow)
        {
            ComputeNeowFast(baseSeed, query.Branch, ref data);
        }

        if (plan.NeedsLayout)
        {
            ComputeLayoutFast(baseSeed, query.Branch, data.Act1MapId, plan.LayoutDepth, ref data);
            if (plan.NeedsMap)
            {
                var map = ReferenceActMap.Generate(
                    data.Act1MapId.ToString(),
                    unchecked(baseSeed + StreamHash(query.Branch, "act_1_map")),
                    ascension,
                    data.Boss1Id);
                data.EliteCount = map.Nodes.Count(node => node.Kind == "elite");
                data.ShopCount = map.Nodes.Count(node => node.Kind == "shop");
                data.RestSiteCount = map.Nodes.Count(node => node.Kind == "rest");
                data.HasMap = true;
            }
        }

        if (query.MinimumElites > 0 && data.EliteCount < query.MinimumElites)
        {
            return false;
        }

        if (query.MinimumShops > 0 && data.ShopCount < query.MinimumShops)
        {
            return false;
        }

        if (query.MinimumRestSites > 0 && data.RestSiteCount < query.MinimumRestSites)
        {
            return false;
        }

        if (!IsAnyFilter(query.AncientFilter) &&
            !MatchesNamedFilter(AncientSummary(in data), query.AncientFilter))
        {
            return false;
        }

        if (!IsAnyFilter(query.BossFilter) &&
            !MatchesNamedFilter(BossSummary(in data), query.BossFilter))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.HiddenSpec))
        {
            if (!MatchesSpecFragmentsFast(
                    plan.Fragments,
                    in data,
                    character,
                    ascension,
                    plan.CharacterName))
            {
                return false;
            }
        }

        return true;
    }

    private static void ComputeNeowFast(
        ulong baseSeed,
        SeedBranch branch,
        ref CandidateData data)
    {
        var neowRng = Sts2ReferenceRng.Create(baseSeed + StreamHash(branch, "NEOW"));
        var rewardsRng = Sts2ReferenceRng.Create(baseSeed + StreamHash(branch, "rewards"));
        data.NeowOfferId = CursedOffers[Sts2ReferenceRng.NextInt(ref neowRng, CursedOffers.Length)];
        var bonusPool = BuildNeowBonusPool(data.NeowOfferId, ref neowRng);
        data.BonusAId = bonusPool[0];
        data.BonusBId = bonusPool[1];
        if (data.NeowOfferId == "neowsbones")
        {
            var grantPool = GrantRelics.ToList();
            Sts2ReferenceRng.Shuffle(ref rewardsRng, grantPool);
            data.NeowGrantAId = grantPool[0];
            data.NeowGrantBId = grantPool[1];
        }
    }

    private static void ComputeLayoutFast(
        ulong baseSeed,
        SeedBranch branch,
        int act1MapId,
        int depth,
        ref CandidateData data)
    {
        var layoutRng = Sts2ReferenceRng.Create(baseSeed + StreamHash(branch, "up_front"));
        Advance(ref layoutRng, 29 + 24 + 34 + 24 + 1);
        Advance(ref layoutRng, 31 + 25 + 37 + 25);
        var darv2 = Sts2ReferenceRng.NextInt(ref layoutRng, 2);
        var darv3 = Sts2ReferenceRng.NextInt(ref layoutRng, 2) != 0 && darv2 == 0;

        Advance(ref layoutRng, (act1MapId == 1 ? 10 : 13) + 18 - 1);
        var act1Easy1 = Sts2ReferenceRng.NextInt(ref layoutRng, 4);
        var act1Easy2 = Sts2ReferenceRng.NextInt(ref layoutRng, 3);
        var act1Easy3 = Sts2ReferenceRng.NextInt(ref layoutRng, 2);
        AdjustDistinctAct1Easy(ref act1Easy1, ref act1Easy2, ref act1Easy3);
        var previousCombat = (act1MapId == 1 ? UnderdocksEasyCombats : OvergrowthEasyCombats)[act1Easy3];
        ConsumeHardPoolNoAlloc(
            ref layoutRng,
            act1MapId == 1 ? UnderdocksHardCombats : OvergrowthHardCombats,
            12,
            ref previousCombat);
        AdvanceElites(ref layoutRng);
        data.Boss1Id = (act1MapId == 1 ? UnderdocksBosses : OvergrowthBosses)
            [Sts2ReferenceRng.NextInt(ref layoutRng, 3)];
        Advance(ref layoutRng, 1);
        if (depth < 2)
        {
            return;
        }

        Advance(ref layoutRng, 10 + 18 - 1);
        var act2Easy1 = Sts2ReferenceRng.NextInt(ref layoutRng, 4);
        var act2Easy2 = Sts2ReferenceRng.NextInt(ref layoutRng, 3);
        if (act2Easy2 >= act2Easy1)
        {
            act2Easy2 += 1;
        }

        previousCombat = HiveEasyCombats[act2Easy2];
        ConsumeHardPoolNoAlloc(ref layoutRng, HiveHardCombats, 12, ref previousCombat);
        AdvanceElites(ref layoutRng);
        data.Boss2Id = Act2Bosses[Sts2ReferenceRng.NextInt(ref layoutRng, Act2Bosses.Length)];
        data.Ancient2Id = Act2Ancients[Sts2ReferenceRng.NextInt(ref layoutRng, 3 + (darv2 != 0 ? 1 : 0))];
        if (depth < 3)
        {
            return;
        }

        Advance(ref layoutRng, 7 + 18 - 1);
        var act3Easy1 = Sts2ReferenceRng.NextInt(ref layoutRng, 3);
        var act3Easy2 = Sts2ReferenceRng.NextInt(ref layoutRng, 2);
        if (act3Easy2 >= act3Easy1)
        {
            act3Easy2 += 1;
        }

        previousCombat = GloryEasyCombats[act3Easy2];
        ConsumeHardPoolNoAlloc(ref layoutRng, GloryHardCombats, 11, ref previousCombat);
        AdvanceElites(ref layoutRng);
        data.Boss3Id = Act3Bosses[Sts2ReferenceRng.NextInt(ref layoutRng, Act3Bosses.Length)];
        data.Ancient3Id = Act3Ancients[Sts2ReferenceRng.NextInt(ref layoutRng, 3 + (darv3 ? 1 : 0))];
        data.Boss3BId = SecondBossNoAlloc(ref layoutRng, data.Boss3Id);

        var ancient2OfferPool = AncientOfferPool(data.Ancient2Id, 2);
        var ancient3OfferPool = AncientOfferPool(data.Ancient3Id, 3);
        data.Ancient2OfferId = ancient2OfferPool.Count == 0
            ? AncientOfferIds[Sts2ReferenceRng.NextInt(ref layoutRng, AncientOfferIds.Length)]
            : ancient2OfferPool[Sts2ReferenceRng.NextInt(ref layoutRng, ancient2OfferPool.Count)];
        data.Ancient3OfferId = ancient3OfferPool.Count == 0
            ? AncientOfferIds[Sts2ReferenceRng.NextInt(ref layoutRng, AncientOfferIds.Length)]
            : ancient3OfferPool[Sts2ReferenceRng.NextInt(ref layoutRng, ancient3OfferPool.Count)];
    }

    private static string SecondBossNoAlloc(ref Sts2ReferenceRng.RngState rng, string boss3Id)
    {
        var first = "";
        var second = "";
        foreach (var id in Act3Bosses)
        {
            if (id.Equals(boss3Id, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (first.Length == 0)
            {
                first = id;
            }
            else
            {
                second = id;
                break;
            }
        }

        return Sts2ReferenceRng.NextInt(ref rng, 2) == 0 ? first : second;
    }

    private static void ConsumeHardPoolNoAlloc(
        ref Sts2ReferenceRng.RngState rng,
        int[] fullPool,
        int count,
        ref int previous)
    {
        Span<bool> active = stackalloc bool[16];
        for (var index = 0; index < fullPool.Length; index++)
        {
            active[index] = true;
        }

        var activeCount = fullPool.Length;
        for (var i = 0; i < count; i++)
        {
            if (activeCount == 0)
            {
                for (var index = 0; index < fullPool.Length; index++)
                {
                    active[index] = true;
                }

                activeCount = fullPool.Length;
            }

            var doCheck = false;
            for (var index = 0; index < fullPool.Length; index++)
            {
                if (active[index] && !SharesCombatTags(fullPool[index], previous))
                {
                    doCheck = true;
                    break;
                }
            }

            int pickedIndex;
            do
            {
                var pick = Sts2ReferenceRng.NextInt(ref rng, activeCount);
                pickedIndex = -1;
                var seen = 0;
                for (var index = 0; index < fullPool.Length; index++)
                {
                    if (!active[index])
                    {
                        continue;
                    }

                    if (seen++ == pick)
                    {
                        pickedIndex = index;
                        break;
                    }
                }
            } while (doCheck && SharesCombatTags(fullPool[pickedIndex], previous));

            previous = fullPool[pickedIndex];
            active[pickedIndex] = false;
            activeCount--;
        }
    }

    private static string AncientSummary(in CandidateData data) =>
        data.Ancient2Id.Length == 0
            ? ""
            : $"{Humanize(data.Ancient2Id)} / {Humanize(data.Ancient3Id)}";

    private static string BossSummary(in CandidateData data) =>
        data.Boss1Id.Length == 0
            ? ""
            : $"{Humanize(data.Boss1Id)} / {Humanize(data.Boss2Id)} / {Humanize(data.Boss3Id)}";

    private static string BuildNeowSummary(in CandidateData data) =>
        data.NeowOfferId.Length == 0
            ? ""
            : $"{Humanize(data.NeowOfferId)} · " +
              $"{Humanize(data.NeowOfferId)} / {Humanize(data.BonusAId)} / {Humanize(data.BonusBId)}";

    private static bool MatchesSpecFragmentsFast(
        IReadOnlyList<SpecFragment> fragments,
        in CandidateData data,
        RunCharacter character,
        int ascension,
        string characterName)
    {
        var neowSummary = (string?)null;
        var actMapId = (string?)null;
        foreach (var fragment in fragments)
        {
            if (fragment.Key == "reward_ordered")
            {
                continue;
            }

            if (fragment.Key == "scarcity")
            {
                if (ascension < 7)
                {
                    return false;
                }

                continue;
            }

            if (!fragment.HasExpected)
            {
                continue;
            }

            if (fragment.Key == "bonus")
            {
                neowSummary ??= BuildNeowSummary(in data);
                if (!neowSummary.Contains(fragment.Expected, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                continue;
            }

            if (fragment.Key == "act")
            {
                actMapId ??= data.Act1MapId.ToString();
                if (!actMapId.Equals(fragment.Expected, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                continue;
            }

            if (!MatchesSpecFragmentFast(
                    fragment.Key,
                    fragment.Expected,
                    in data,
                    character,
                    characterName))
            {
                return false;
            }
        }

        return true;
    }

    private static bool MatchesSpecFragmentFast(
        string key,
        string expected,
        in CandidateData data,
        RunCharacter character,
        string characterName)
    {
        return key switch
        {
            "neowOffer" => data.NeowOfferId.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "bonesGrantA" => data.NeowGrantAId.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "bonesGrantB" => data.NeowGrantBId.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "char" => characterName.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "neow" => int.TryParse(expected, out var index) &&
                      index >= 0 &&
                      index < CursedOffers.Length &&
                      data.NeowOfferId.Equals(CursedOffers[index], StringComparison.OrdinalIgnoreCase),
            "boss1" => data.Boss1Id.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "boss2" => data.Boss2Id.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "boss3" => data.Boss3Id.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "boss3b" => data.Boss3BId.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "ancient2" => data.Ancient2Id.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "ancient3" => data.Ancient3Id.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "ancient2_offers" or "ancient2_offers_if" =>
                data.Ancient2OfferId.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "ancient3_offers" or "ancient3_offers_if" =>
                data.Ancient3OfferId.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "bones_relic" => ContainsMultiset($"{data.NeowGrantAId}+{data.NeowGrantBId}", expected),
            "reward_within" or "shop_within" or "bag_within" or "event_within" =>
                int.TryParse(expected, out var window) && window > 0,
            "rares" => int.TryParse(expected, out var rares) && rares is >= 1 and <= 6,
            _ => false,
        };
    }

    private static ulong HashBetaSeed(long index)
    {
        Span<byte> bytes = stackalloc byte[BetaSeedLength];
        var value = unchecked((ulong)Math.Max(0, index));
        for (var position = bytes.Length - 1; position >= 0; position--)
        {
            bytes[position] = (byte)BetaAlphabet[(int)(value % (ulong)BetaAlphabet.Length)];
            value /= (ulong)BetaAlphabet.Length;
        }

        bytes.Reverse();
        return Sts2ReferenceRng.HashCode64(bytes);
    }

    internal static CandidateData EvaluateFastCandidate(
        SeedBranch branch,
        long index,
        RunCharacter character,
        int ascension,
        bool withMap = false)
    {
        var baseSeed = HashBetaSeed(index);
        var data = new CandidateData
        {
            Act1MapId = -1,
            EliteCount = -1,
            ShopCount = -1,
            RestSiteCount = -1,
        };
        var actSelectionRng = Sts2ReferenceRng.Create(baseSeed + StreamHash(branch, "act_selection"));
        data.Act1MapId = Sts2ReferenceRng.NextInt(ref actSelectionRng, 2);
        ComputeNeowFast(baseSeed, branch, ref data);
        ComputeLayoutFast(baseSeed, branch, data.Act1MapId, 3, ref data);
        if (withMap)
        {
            var map = ReferenceActMap.Generate(
                data.Act1MapId.ToString(),
                unchecked(baseSeed + StreamHash(branch, "act_1_map")),
                ascension,
                data.Boss1Id);
            data.EliteCount = map.Nodes.Count(node => node.Kind == "elite");
            data.ShopCount = map.Nodes.Count(node => node.Kind == "shop");
            data.RestSiteCount = map.Nodes.Count(node => node.Kind == "rest");
            data.HasMap = true;
        }

        return data;
    }

    private static void Advance(ref Sts2ReferenceRng.RngState rng, int count)
    {
        for (var i = 0; i < count; i++)
        {
            Sts2ReferenceRng.Next(ref rng);
        }
    }

    private static void AdjustDistinctAct1Easy(ref int e11, ref int e12, ref int e13)
    {
        if (e12 >= e11)
        {
            e12 += 1;
            if (e13 >= e11)
            {
                e13 += 1;
            }

            if (e13 >= e12)
            {
                e13 += 1;
            }
        }
        else
        {
            if (e13 >= e12)
            {
                e13 += 1;
            }

            if (e13 >= e11)
            {
                e13 += 1;
            }
        }
    }

    private static bool SharesCombatTags(int value, int previous) =>
        previous >= 0 && ((value & previous & ~0xff) != 0 || value == previous);

    private static void ConsumeHardPool(
        ref Sts2ReferenceRng.RngState rng,
        int[] fullPool,
        int count,
        ref int previous)
    {
        var pool = fullPool.ToList();
        for (var i = 0; i < count; i++)
        {
            if (pool.Count == 0)
            {
                pool = fullPool.ToList();
            }

            var doCheck = false;
            foreach (var poolValue in pool)
            {
                if (!SharesCombatTags(poolValue, previous))
                {
                    doCheck = true;
                    break;
                }
            }
            int candidate;
            int value;
            do
            {
                candidate = Sts2ReferenceRng.NextInt(ref rng, pool.Count);
                value = pool[candidate];
            } while (doCheck && SharesCombatTags(value, previous));

            previous = value;
            pool.RemoveAt(candidate);
        }
    }

    private static void AdvanceElites(ref Sts2ReferenceRng.RngState rng)
    {
        var previous = -1;
        for (var i = 0; i < 5; i++)
        {
            int e1;
            do
            {
                e1 = Sts2ReferenceRng.NextInt(ref rng, 3);
            } while (e1 == previous);

            var e2 = Sts2ReferenceRng.NextInt(ref rng, 2);
            previous = 2 - (e1 + e2 * 3) / 2;
            Sts2ReferenceRng.Next(ref rng);
        }
    }

    private static ulong StreamHash(SeedBranch branch, string name) =>
        branch == SeedBranch.PublicBeta
            ? Sts2ReferenceRng.HashCode64(name)
            : unchecked((ulong)(uint)Sts2ReferenceRng.HashCode(name));

    private static List<string> BuildNeowBonusPool(
        string cursedOffer,
        ref Sts2ReferenceRng.RngState rng)
    {
        // The first 14 entries are Neow's always-available positive options
        // (v0.110.1 adds Massive Scroll between Lost Coffer and Neow's
        // Torment). The remaining six entries are the three mutually
        // exclusive pairs added by Neow.GenerateInitialOptions.
        var pool = BonusOffers.Take(14).ToList();
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

    private static IReadOnlyDictionary<RunCharacter, IReadOnlyList<string>> BuildPoolCache(
        Func<RunCharacter, IReadOnlyList<string>> builder) =>
        Enum.GetValues<RunCharacter>().ToDictionary(character => character, builder);

    // The versioned pools are immutable for a pinned game version. Building
    // them once per character instead of once per candidate removes the
    // dominant per-seed allocation from the hot search loop.
    private static readonly IReadOnlyDictionary<RunCharacter, IReadOnlyList<string>> CardPoolCache =
        BuildPoolCache(BuildCardPool);
    private static readonly IReadOnlyDictionary<RunCharacter, IReadOnlyList<string>> RareCardPoolCache =
        BuildPoolCache(BuildRareCardPool);
    private static readonly IReadOnlyDictionary<RunCharacter, IReadOnlyList<string>> CommonUncommonCardPoolCache =
        BuildPoolCache(BuildCommonUncommonCardPool);
    private static readonly IReadOnlyDictionary<RunCharacter, IReadOnlyList<string>> OtherCharacterCardPoolCache =
        BuildPoolCache(BuildOtherCharacterCardPool);
    private static readonly IReadOnlyDictionary<RunCharacter, IReadOnlyList<string>> PotionPoolCache =
        BuildPoolCache(BuildPotionPool);
    private static readonly IReadOnlyDictionary<RunCharacter, IReadOnlyList<string>> ShopRelicPoolCache =
        BuildPoolCache(BuildShopRelicPool);
    private static readonly IReadOnlyDictionary<RunCharacter, IReadOnlyList<string>> CapsuleRelicPoolCache =
        BuildPoolCache(BuildCapsuleRelicPool);

    private static IReadOnlyList<string> CardPool(RunCharacter character) => CardPoolCache[character];

    private static IReadOnlyList<string> RareCardPool(RunCharacter character) => RareCardPoolCache[character];

    private static IReadOnlyList<string> CommonUncommonCardPool(RunCharacter character) =>
        CommonUncommonCardPoolCache[character];

    private static IReadOnlyList<string> OtherCharacterCardPool(RunCharacter character) =>
        OtherCharacterCardPoolCache[character];

    private static IReadOnlyList<string> PotionPool(RunCharacter character) => PotionPoolCache[character];

    private static IReadOnlyList<string> ShopRelicPool(RunCharacter character) => ShopRelicPoolCache[character];

    private static IReadOnlyList<string> CapsuleRelicPool(RunCharacter character) => CapsuleRelicPoolCache[character];

    private static IReadOnlyList<string> BuildCardPool(RunCharacter character)
    {
        var versioned = character != RunCharacter.Any && SearchTheSpirePoolData.CardPools.TryGetValue(character, out var own)
            ? own
            : SearchTheSpirePoolData.CardPools.Values.SelectMany(values => values);
        return versioned.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static IReadOnlyList<string> BuildRareCardPool(RunCharacter character) =>
        character != RunCharacter.Any && SearchTheSpirePoolData.RareCards.TryGetValue(character, out var own)
            ? own
            : SearchTheSpirePoolData.RareCards.Values.SelectMany(values => values).Distinct(StringComparer.Ordinal).ToArray();

    private static IReadOnlyList<string> BuildCommonUncommonCardPool(RunCharacter character)
    {
        var commons = character != RunCharacter.Any && SearchTheSpirePoolData.CommonCards.TryGetValue(character, out var ownCommons)
            ? ownCommons
            : SearchTheSpirePoolData.CommonCards.Values.SelectMany(values => values);
        var uncommons = character != RunCharacter.Any && SearchTheSpirePoolData.UncommonCards.TryGetValue(character, out var ownUncommons)
            ? ownUncommons
            : SearchTheSpirePoolData.UncommonCards.Values.SelectMany(values => values);
        return commons.Concat(uncommons).Distinct(StringComparer.Ordinal).ToArray();
    }

    private static IReadOnlyList<string> BuildOtherCharacterCardPool(RunCharacter character) =>
        SearchTheSpirePoolData.CardPools
            .Where(pair => character == RunCharacter.Any || pair.Key != character)
            .SelectMany(pair => pair.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private sealed record CardDetailPools(
        IReadOnlyList<string> Rollable,
        IReadOnlyList<string> Rare,
        IReadOnlyList<string> CommonUncommon,
        IReadOnlyList<string> OtherCharacters);

    private static string SimulateKaleidoCards(
        ulong baseSeed,
        SeedBranch branch,
        RunCharacter character,
        int ascension)
    {
        var nicheRng = Sts2ReferenceRng.Create(baseSeed + StreamHash(branch, "niche"));
        var rewardsRng = Sts2ReferenceRng.Create(baseSeed + StreamHash(branch, "rewards"));
        return SimulateKaleidoCards(ref nicheRng, ref rewardsRng, character, ascension);
    }

    private static string SimulateKaleidoCards(
        ref Sts2ReferenceRng.RngState nicheRng,
        ref Sts2ReferenceRng.RngState rewardsRng,
        RunCharacter character,
        int ascension)
    {
        var pools = SearchTheSpirePoolData.CardPools.Keys
            .Where(key => key != character)
            .OrderBy(key => key.ToString())
            .ToList();
        var cards = new List<string>();
        for (var reward = 0; reward < 2; reward++)
        {
            var shuffled = pools.ToList();
            Sts2ReferenceRng.Shuffle(ref nicheRng, shuffled);
            foreach (var owner in shuffled.Take(3))
            {
                var rarityRoll = Sts2ReferenceRng.NextDouble(ref rewardsRng);
                var rareOdds = ascension >= 7 ? 0.0149 : 0.03;
                var rarityPool = rarityRoll < rareOdds
                    ? SearchTheSpirePoolData.RareCards[owner]
                    : rarityRoll < 0.37 + rareOdds
                        ? SearchTheSpirePoolData.UncommonCards[owner]
                        : SearchTheSpirePoolData.CommonCards[owner];
                cards.Add(rarityPool[Sts2ReferenceRng.NextInt(ref rewardsRng, rarityPool.Length)]);
                _ = Sts2ReferenceRng.NextDouble(ref rewardsRng);
            }
        }

        return string.Join('+', cards);
    }

    private static IReadOnlyList<string> BuildPotionPool(RunCharacter character)
    {
        var own = character != RunCharacter.Any && SearchTheSpirePoolData.CharacterPotions.TryGetValue(character, out var values)
            ? values
            : Array.Empty<string>();
        return own.Concat(SearchTheSpirePoolData.SharedPotions).Concat(Potions)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> BuildShopRelicPool(RunCharacter character)
    {
        var own = character != RunCharacter.Any && SearchTheSpirePoolData.CharacterShopRelics.TryGetValue(character, out var values)
            ? values
            : Array.Empty<string>();
        return SearchTheSpirePoolData.SharedShopRelics.Concat(own).Concat(RelicIds)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> BuildCapsuleRelicPool(RunCharacter character)
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
            .FirstOrDefault(value => value.Length > 1 &&
                                     value[0] == 'A' &&
                                     int.TryParse(value[1..], out _));
        return token != null && int.TryParse(token[1..], out var ascension)
            ? Math.Clamp(ascension, 0, SearchTheSpireBoardState.MaxAscension)
            : 0;
    }

    private static string BuildDetailSpec(
        string offer,
        string grantA,
        string grantB,
        IReadOnlyList<string> rewardCards,
        CardDetailPools cardPools,
        IReadOnlyList<string> potions,
        IReadOnlyList<string> relics,
        ref Sts2ReferenceRng.RngState rewardsRng,
        ref Sts2ReferenceRng.RngState nicheRng,
        ref Sts2ReferenceRng.RngState transformationsRng,
        ref Sts2ReferenceRng.RngState combatPotionRng,
        RunCharacter character,
        int ascension,
        string? kaleidoDistinct = null)
    {
        var details = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["reward1_card"] = rewardCards[0],
            ["reward2_card"] = rewardCards[1],
            ["reward3_card"] = rewardCards[2],
        };
        if (kaleidoDistinct != null)
        {
            details["kaleido_distinct"] = kaleidoDistinct;
        }

        if (offer == "neowsbones")
        {
            AddOfferDetails(
                details, grantA, "bones_", cardPools, potions, relics,
                ref rewardsRng, ref nicheRng, ref transformationsRng, ref combatPotionRng,
                character, ascension);
            AddBonesCapsulePulls(details, grantA, relics, ref rewardsRng);
            AddOfferDetails(
                details, grantB, "bones_", cardPools, potions, relics,
                ref rewardsRng, ref nicheRng, ref transformationsRng, ref combatPotionRng,
                character, ascension);
            AddBonesCapsulePulls(details, grantB, relics, ref rewardsRng);
            // The curse is rolled after both grants' pickup effects, in the
            // order NeowsBones.AfterObtained consumes the Niche stream.
            details["bones_curse"] = TakeValue(Curses, ref nicheRng);
        }
        else
        {
            AddOfferDetails(
                details, offer, string.Empty, cardPools, potions, relics,
                ref rewardsRng, ref nicheRng, ref transformationsRng, ref combatPotionRng,
                character, ascension);
        }

        return string.Join(',', details.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}={pair.Value}"));
    }

    private static void AddBonesCapsulePulls(
        IDictionary<string, string> details,
        string offer,
        IReadOnlyList<string> relics,
        ref Sts2ReferenceRng.RngState rewardsRng)
    {
        var pulls = CapsulePulls(offer);
        if (pulls == 0)
        {
            return;
        }

        var pulled = TakeDistinct(relics, pulls, ref rewardsRng);
        details["bones_capsule_set"] = details.TryGetValue("bones_capsule_set", out var existing)
            ? existing + "+" + pulled
            : pulled;
    }

    private static void AddOfferDetails(
        IDictionary<string, string> details,
        string offer,
        string prefix,
        CardDetailPools cardPools,
        IReadOnlyList<string> potions,
        IReadOnlyList<string> relics,
        ref Sts2ReferenceRng.RngState rewardsRng,
        ref Sts2ReferenceRng.RngState nicheRng,
        ref Sts2ReferenceRng.RngState transformationsRng,
        ref Sts2ReferenceRng.RngState combatPotionRng,
        RunCharacter character,
        int ascension)
    {
        if (string.IsNullOrWhiteSpace(offer))
        {
            return;
        }

        switch (offer)
        {
            case "heftytablet":
                details[$"{prefix}tablet_card"] = TakeValue(cardPools.Rare, ref rewardsRng);
                break;
            case "arcanescroll":
                details[$"{prefix}arcane_card"] = TakeValue(cardPools.Rare, ref rewardsRng);
                break;
            case "leadpaperweight":
                details[$"{prefix}paperweight_card"] = TakeValue(cardPools.Rollable, ref rewardsRng);
                break;
            case "lostcoffer":
                details[$"{prefix}coffer_card"] = TakeValue(cardPools.Rollable, ref rewardsRng);
                details[$"{prefix}coffer_potion"] = TakeValue(potions, ref rewardsRng);
                break;
            case "largecapsule":
                if (prefix == "bones_")
                {
                    break;
                }

                details[prefix.Length == 0 ? "large_relic" : "bones_capsule_set"] =
                    TakeDistinct(relics, prefix.Length == 0 ? 2 : 3, ref rewardsRng);
                break;
            case "smallcapsule":
                if (prefix == "bones_")
                {
                    break;
                }

                details[prefix.Length == 0 ? "capsule_relic" : "bones_capsule_set"] =
                    TakeDistinct(relics, prefix.Length == 0 ? 1 : 3, ref rewardsRng);
                break;
            case "kaleidoscope":
                details[$"{prefix}kaleido_distinct"] =
                    SimulateKaleidoCards(ref nicheRng, ref rewardsRng, character, ascension);
                break;
            case "newleaf":
                details[$"{prefix}newleaf_card"] = TakeValue(cardPools.Rollable, ref nicheRng);
                break;
            case "scrollboxes":
                details[$"{prefix}scrollbox_contains"] = TakeDistinct(cardPools.CommonUncommon, 3, ref rewardsRng);
                break;
            case "leafypoultice":
                details[$"{prefix}poultice_set"] = TakeDistinct(cardPools.Rollable, 2, ref transformationsRng);
                break;
            case "phialholster":
                details[$"{prefix}phial_potion"] = TakeDistinct(potions, 2, ref combatPotionRng);
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

    internal static bool Matches(SeedQuery query, SeedSnapshot snapshot)
    {
        if (query.Character != RunCharacter.Any && snapshot.Character != query.Character)
        {
            return false;
        }

        if (query.MinimumElites > 0 && (snapshot.Map == null || snapshot.EliteCount < query.MinimumElites))
        {
            return false;
        }

        if (query.MinimumShops > 0 && (snapshot.Map == null || snapshot.ShopCount < query.MinimumShops))
        {
            return false;
        }

        if (query.MinimumRestSites > 0 && (snapshot.Map == null || snapshot.RestSiteCount < query.MinimumRestSites))
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
        string.IsNullOrWhiteSpace(filter) ||
        filter.Equals("Any", StringComparison.OrdinalIgnoreCase) ||
        // Compatibility for saved searches created by the first Chinese UI
        // pass. New queries use the canonical English `Any` token at the
        // display/model boundary, but old saves must not become zero-result.
        filter is "任意" or "任何" ||
        value.Contains(filter, StringComparison.OrdinalIgnoreCase);

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

    private static string Humanize(string id) => SearchTheSpireCatalog.DisplayName(id);

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

            // SearchTheSpire's display_for_index writes the beta seed with the
            // most significant digit first; keep the batch-search enumeration
            // aligned with its candidate offsets.
            Array.Reverse(buffer);
            return new string(buffer);
        }
    }

}
