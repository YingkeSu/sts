using SeedSearchPrototype;
using System.Text.Json;
using System.Text.Json.Serialization;

var engine = new SeedSearchEngine();
if (SeedSearchEngine.HashCode("000000000000") != -626668290 ||
    SeedSearchEngine.HashCode("00000000000E") != -559411147)
{
    throw new InvalidOperationException("The reference engine hash does not match SearchTheSpire's STS2 hash primitive.");
}

if (SeedSearchEngine.HashCode64("000000000000") != 8976409089464021311UL)
{
    throw new InvalidOperationException("The public-beta seed hash does not match STS2's XxHash64 seed primitive.");
}

var rngOutputs = SeedSearchEngine.NextOutputs(0, 3);
if (!rngOutputs.SequenceEqual(new ulong[] { 11091344671253066420UL, 13793997310169335082UL, 1900383378846508768UL }))
{
    throw new InvalidOperationException("The reference engine RNG does not match the community STS2 primitive.");
}

var first = engine.Inspect("000000000000", SeedBranch.PublicBeta);
var second = engine.Inspect("000000000000", SeedBranch.PublicBeta);

if (!ReferenceEquals(first.Map, null) &&
    JsonSerializer.Serialize(first) != JsonSerializer.Serialize(second))
{
    throw new InvalidOperationException("Seed inspection is not deterministic.");
}

if (SeedSearchEngine.CreateSeed(SeedBranch.PublicBeta, 42).Length != 12)
{
    throw new InvalidOperationException("Beta seed codec did not produce a 12-character seed.");
}

if (SeedSearchEngine.CreateSeed(SeedBranch.PublicBeta, 1) != "100000000000" ||
    SeedSearchEngine.CreateSeed(SeedBranch.PublicBeta, 941) != "PT0000000000")
{
    throw new InvalidOperationException("Beta seed codec does not match SearchTheSpire display_for_index.");
}

if (SeedSearchEngine.SeedCount(SeedBranch.PublicBeta) != 2_386_420_683_693_101_056L ||
    SeedSearchEngine.SeedCount(SeedBranch.Main) != 4_294_967_296L)
{
    throw new InvalidOperationException("Seed space sizes drifted from the beta 34^12 / main uint ranges.");
}

// Searches may start far from the beginning (default random offset) and must
// wrap past the end of the seed space instead of revisiting aliased seeds.
var wrapTotal = SeedSearchEngine.SeedCount(SeedBranch.PublicBeta);
var wrapQuery = new SeedQuery(
    SeedBranch.PublicBeta,
    GameApiVersion: SeedSearchEngine.PinnedGameApiVersion,
    Character: RunCharacter.Any,
    Ascension: 0,
    RunMode: RunMode.Plain,
    StopAfter: 20,
    StartOffset: wrapTotal - 10,
    MaxCandidates: 40,
    MinimumElites: 0,
    MinimumShops: 0,
    MinimumRestSites: 0,
    NeowFilter: NeowFilter.Any,
    AncientFilter: "Any",
    BossFilter: "Any",
    HiddenSpec: "");
var wrapResults = engine.Search(wrapQuery, CancellationToken.None);
if (wrapResults.Count != 20 ||
    wrapResults[0].Seed != SeedSearchEngine.CreateSeed(SeedBranch.PublicBeta, wrapTotal - 10) ||
    wrapResults.Select(result => result.Seed).Distinct(StringComparer.Ordinal).Count() != 20)
{
    throw new InvalidOperationException("Search does not wrap past the seed-space end without duplicates.");
}

var fullSpaceStart = SeedSearchEngine.PickRandomStartOffset(
    SeedBranch.PublicBeta,
    SeedSearchEngine.SeedCount(SeedBranch.PublicBeta));
var fullSpaceQuery = new SeedQuery(
    SeedBranch.PublicBeta,
    GameApiVersion: SeedSearchEngine.PinnedGameApiVersion,
    Character: RunCharacter.Any,
    Ascension: 0,
    RunMode: RunMode.Plain,
    StopAfter: 5,
    StartOffset: fullSpaceStart,
    MaxCandidates: SeedSearchEngine.SeedCount(SeedBranch.PublicBeta),
    MinimumElites: 0,
    MinimumShops: 0,
    MinimumRestSites: 0,
    NeowFilter: NeowFilter.Any,
    AncientFilter: "Any",
    BossFilter: "Any",
    HiddenSpec: "");
if (engine.Search(fullSpaceQuery, CancellationToken.None).Count != 5)
{
    throw new InvalidOperationException("The default full-space UI search unexpectedly found no seeds.");
}

var randomStartLow = SeedSearchEngine.SeedCount(SeedBranch.PublicBeta) / 10;
for (var sample = 0; sample < 200; sample++)
{
    var offset = SeedSearchEngine.PickRandomStartOffset(SeedBranch.PublicBeta, 1_000_000L);
    if (offset < randomStartLow || offset >= SeedSearchEngine.SeedCount(SeedBranch.PublicBeta) - randomStartLow)
    {
        throw new InvalidOperationException($"Random search start {offset} is not far from the seed-space edges.");
    }
}

// The allocation-free fast evaluator must reproduce Inspect's raw layout,
// Neow and map-count values exactly (differential check over a spread).
for (var sample = 0; sample < 200; sample++)
{
    var index = 1_000_000L + sample * 997L;
    var seed = SeedSearchEngine.CreateSeed(SeedBranch.PublicBeta, index);
    var reference = engine.Inspect(
        seed,
        SeedBranch.PublicBeta,
        $"{SeedSearchEngine.PinnedGameApiVersion}|Ironclad|A0|Plain|");
    var fast = SeedSearchEngine.EvaluateFastCandidate(
        SeedBranch.PublicBeta,
        index,
        RunCharacter.Ironclad,
        0,
        withMap: true);
    if (fast.Act1MapId != int.Parse(reference.Act1MapId) ||
        fast.Boss1Id != reference.Boss1Id ||
        fast.Boss2Id != reference.Boss2Id ||
        fast.Boss3Id != reference.Boss3Id ||
        fast.Boss3BId != reference.Boss3BId ||
        fast.Ancient2Id != reference.Ancient2Id ||
        fast.Ancient3Id != reference.Ancient3Id ||
        fast.Ancient2OfferId != reference.Ancient2OfferId ||
        fast.Ancient3OfferId != reference.Ancient3OfferId ||
        fast.NeowOfferId != reference.NeowOfferId ||
        fast.NeowGrantAId != reference.NeowGrantAId ||
        fast.NeowGrantBId != reference.NeowGrantBId ||
        fast.EliteCount != reference.EliteCount ||
        fast.ShopCount != reference.ShopCount ||
        fast.RestSiteCount != reference.RestSiteCount)
    {
        throw new InvalidOperationException(
            $"Fast evaluator diverged from Inspect for {seed}: " +
            $"boss1 {fast.Boss1Id} vs {reference.Boss1Id}, " +
            $"neow {fast.NeowOfferId} vs {reference.NeowOfferId}, " +
            $"elites {fast.EliteCount} vs {reference.EliteCount}");
    }
}

// Fast search path must select the same seeds as the full Inspect reference
// for layout, Neow and map-count constraints.
var fastDifferentialSpecs = new[]
{
    "act=0",
    "neow=0",
    "bonus=kaleidoscope",
    "boss1=vantom",
    "boss2=kaisercrab",
    "ancient3=darv",
};
foreach (var hiddenSpec in fastDifferentialSpecs)
{
    var differentialQuery = new SeedQuery(
        SeedBranch.PublicBeta,
        GameApiVersion: SeedSearchEngine.PinnedGameApiVersion,
        Character: RunCharacter.Any,
        Ascension: 0,
        RunMode: RunMode.Plain,
        StopAfter: 5,
        StartOffset: 5000,
        MaxCandidates: 20_000,
        MinimumElites: 0,
        MinimumShops: 0,
        MinimumRestSites: 0,
        NeowFilter: NeowFilter.Any,
        AncientFilter: "Any",
        BossFilter: "Any",
        HiddenSpec: hiddenSpec);
    var fastSeeds = engine.Search(differentialQuery, CancellationToken.None)
        .Select(match => match.Seed)
        .ToArray();
    var referenceSeeds = SequentialSearchReference(engine, differentialQuery, CancellationToken.None)
        .Select(match => match.Seed)
        .ToArray();
    if (!fastSeeds.SequenceEqual(referenceSeeds))
    {
        throw new InvalidOperationException(
            $"Fast search path diverged for {hiddenSpec}: " +
            $"fast={string.Join(",", fastSeeds)}, reference={string.Join(",", referenceSeeds)}");
    }
}

var countDifferentialQuery = new SeedQuery(
    SeedBranch.PublicBeta,
    GameApiVersion: SeedSearchEngine.PinnedGameApiVersion,
    Character: RunCharacter.Any,
    Ascension: 0,
    RunMode: RunMode.Plain,
    StopAfter: 5,
    StartOffset: 5000,
    MaxCandidates: 20_000,
    MinimumElites: 1,
    MinimumShops: 0,
    MinimumRestSites: 0,
    NeowFilter: NeowFilter.Any,
    AncientFilter: "Any",
    BossFilter: "Any",
    HiddenSpec: "");
var fastCountSeeds = engine.Search(countDifferentialQuery, CancellationToken.None)
    .Select(match => match.Seed)
    .ToArray();
var referenceCountSeeds = SequentialSearchReference(engine, countDifferentialQuery, CancellationToken.None)
    .Select(match => match.Seed)
    .ToArray();
if (!fastCountSeeds.SequenceEqual(referenceCountSeeds))
{
    throw new InvalidOperationException(
        $"Fast map-count path diverged: fast={string.Join(",", fastCountSeeds)}, " +
        $"reference={string.Join(",", referenceCountSeeds)}");
}

// [issue-3] SearchTheSpire's board exposes none + A1..A10 only. The model,
// engine context parsing and UI all share MaxAscension so a generic-looking
// STS2 A0-A20 range cannot regress again (it already did twice).
var ascensionValues = SearchTheSpireCatalog.AscensionValues;
if (!ascensionValues.SequenceEqual(Enumerable.Range(0, SearchTheSpireBoardState.MaxAscension + 1)) ||
    SearchTheSpireCatalog.MaxAscension != SearchTheSpireBoardState.MaxAscension ||
    ascensionValues.Any(value => value > SearchTheSpireBoardState.MaxAscension) ||
    SearchTheSpireBoardState.Empty.WithAscension(15).Ascension != SearchTheSpireBoardState.MaxAscension ||
    SearchTheSpireBoardState.Empty.WithAscension(25).Ascension != SearchTheSpireBoardState.MaxAscension ||
    engine.Inspect("000000000000", SeedBranch.PublicBeta, $"{SeedSearchEngine.PinnedGameApiVersion}|Ironclad|A20|Plain|").Ascension != SearchTheSpireBoardState.MaxAscension)
{
    throw new InvalidOperationException("Ascension surface regressed from SearchTheSpire's A0-A10 range or no longer clamps at A10.");
}

if (OptionArtRouter.For(new SearchTheSpireOption("bash", "Bash", "Ironclad cards · rare")) != OptionArtKind.Card ||
    OptionArtRouter.For(new SearchTheSpireOption("anchor", "Anchor", "relic rewards")) != OptionArtKind.Relic ||
    OptionArtRouter.For(new SearchTheSpireOption("anchor", "Anchor", "any character")) != OptionArtKind.Relic ||
    OptionArtRouter.For(new SearchTheSpireOption("anchor", "Anchor", "Ironclad — picks the character")) != OptionArtKind.Relic ||
    OptionArtRouter.For(new SearchTheSpireOption("vantom", "Vantom", "Overgrowth bosses")) != OptionArtKind.Boss ||
    OptionArtRouter.For(new SearchTheSpireOption("selfhelpbook", "Self Help Book", "act 1 event")) != OptionArtKind.None ||
    OptionArtRouter.For(new SearchTheSpireOption("neowsbones", "Neow's Bones", "cursed offer")) != OptionArtKind.Relic ||
    OptionArtRouter.For(new SearchTheSpireOption("neowstorment", "Neow's Torment", "bonus offer")) != OptionArtKind.Relic ||
    OptionArtRouter.For(new SearchTheSpireOption("glasseye", "Glass Eye", "ancient offers")) != OptionArtKind.Relic)
{
    throw new InvalidOperationException("Picker art routing does not distinguish cards, relics and bosses.");
}

Console.WriteLine("Ascension A0-A10 and picker art routing checks passed.");

var query = new SeedQuery(
    SeedBranch.PublicBeta,
    GameApiVersion: SeedSearchEngine.PinnedGameApiVersion,
    Character: RunCharacter.Any,
    Ascension: 0,
    RunMode: RunMode.Plain,
    StopAfter: 3,
    StartOffset: 0,
    MaxCandidates: 10_000,
    MinimumElites: 0,
    MinimumShops: 0,
    MinimumRestSites: 0,
    NeowFilter: NeowFilter.HasBlessing,
    AncientFilter: "Any",
    BossFilter: "Any");
var matches = engine.Search(query, CancellationToken.None);

if (matches.Count > query.StopAfter || matches.Any(match => !match.Snapshot.HasBlessing))
{
    throw new InvalidOperationException("Seed search returned a result outside the query constraints.");
}

Console.WriteLine($"Seed search core checks passed: {matches.Count} matches.");

// Map-based minimums force the exact v0.110.1 map generation per candidate;
// at A10 every real map targets 8 elites, so a strict filter must not admit a
// snapshot whose counts came from the old fake map RNG.
var strictMapQuery = new SeedQuery(
    SeedBranch.PublicBeta,
    GameApiVersion: SeedSearchEngine.PinnedGameApiVersion,
    Character: RunCharacter.Any,
    Ascension: 10,
    RunMode: RunMode.Plain,
    StopAfter: 3,
    StartOffset: 0,
    MaxCandidates: 100,
    MinimumElites: 8,
    MinimumShops: 0,
    MinimumRestSites: 0,
    NeowFilter: NeowFilter.Any,
    AncientFilter: "Any",
    BossFilter: "Any");
var strictMapMatches = engine.Search(strictMapQuery, CancellationToken.None);
if (strictMapMatches.Count == 0 ||
    strictMapMatches.Any(match => match.Snapshot.Map == null || match.Snapshot.EliteCount < 8))
{
    throw new InvalidOperationException("A strict map-minimum search did not use the real Act 1 map counts.");
}

Console.WriteLine("Map-minimum search uses real StandardActMap counts.");

// [issue-5] Old Chinese-UI saves can still carry the localized display text
// (`任意`/`任何`) in AncientFilter/BossFilter. They must behave exactly like
// the canonical `Any` token instead of filtering names literally to zero.
if (matches.Count == 0)
{
    throw new InvalidOperationException("The canonical any/any broad query unexpectedly returned no seed.");
}

var chineseLegacyMatches = engine.Search(
    query with { AncientFilter = "任意", BossFilter = "任何" },
    CancellationToken.None);
if (chineseLegacyMatches.Count == 0 || chineseLegacyMatches.Count != matches.Count)
{
    throw new InvalidOperationException(
        $"A saved Chinese `任意/任何` query returned {chineseLegacyMatches.Count} results; " +
        $"expected {matches.Count} like canonical `Any/Any`.");
}

Console.WriteLine("Chinese-era `任意/任何` saved-filter compatibility checks passed.");

// [issue-5] The manifest gate and the engine pin must stay in lockstep so a
// future game update cannot silently drift the search surface.
var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
var manifestVersion = JsonDocument.Parse(File.ReadAllText(Path.Combine(repoRoot, "SeedSearchPrototype.json")))
    .RootElement.GetProperty("min_game_version")
    .GetString();
if (manifestVersion != SeedSearchEngine.PinnedGameApiVersion)
{
    throw new InvalidOperationException(
        $"SeedSearchPrototype.json pins min_game_version `{manifestVersion}`, " +
        $"but the engine pins `{SeedSearchEngine.PinnedGameApiVersion}`.");
}

Console.WriteLine($"Version pin consistency checks passed ({SeedSearchEngine.PinnedGameApiVersion}).");

var betaNeowChecks = new Dictionary<string, (string Cursed, string BonusA, string BonusB, string? GrantA, string? GrantB)>
{
    // Values are derived from the installed v0.110.1 Neow.GenerateInitialOptions:
    // the positive pool has 14 entries (Massive Scroll included), the Bones
    // grants shuffle the same 29-relic pool via the rewards stream, and the
    // Bones curse is the first Niche draw.
    ["000000000000"] = ("largecapsule", "pomander", "scrollboxes", null, null),
    ["000000000001"] = ("dowsingrod", "arcanescroll", "neowstorment", null, null),
    ["000000000002"] = ("neowssacrifice", "goldenpearl", "newleaf", null, null),
    ["000000000003"] = ("neowsbones", "goldenpearl", "newleaf", "leadpaperweight", "phialholster"),
};
foreach (var (seed, expected) in betaNeowChecks)
{
    var snapshot = engine.Inspect(seed, SeedBranch.PublicBeta);
    var summaryParts = snapshot.NeowOffers.Split(" / ", StringSplitOptions.TrimEntries);
    if (snapshot.NeowOfferId != expected.Cursed ||
        summaryParts.Length < 3 ||
        !summaryParts[1].Equals(SearchTheSpireCatalog.DisplayName(expected.BonusA), StringComparison.OrdinalIgnoreCase) ||
        !summaryParts[2].Equals(SearchTheSpireCatalog.DisplayName(expected.BonusB), StringComparison.OrdinalIgnoreCase) ||
        snapshot.NeowGrantAId != (expected.GrantA ?? "") ||
        snapshot.NeowGrantBId != (expected.GrantB ?? ""))
    {
        throw new InvalidOperationException($"Public-beta Neow preview diverged for {seed}: {snapshot.NeowOffers} / {snapshot.NeowGrantAId}+{snapshot.NeowGrantBId}");
    }
}

var bonesCurseId = engine.Inspect("000000000003", SeedBranch.PublicBeta).DetailSpec
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .FirstOrDefault(fragment => fragment.StartsWith("bones_curse=", StringComparison.Ordinal))?
    .Split('=', 2)[1];
if (bonesCurseId != "decay")
{
    throw new InvalidOperationException(
        $"Neow's Bones curse must come from the Niche stream (v0.110.1 AfterObtained), got bones_curse={bonesCurseId ?? "<missing>"}.");
}

var neowOfferOptions = SearchTheSpireBoardState.Empty.OptionsFor("neowOffer");
if (!neowOfferOptions.Any(option => option.Id == "massivescroll" && option.Section == "bonus offer"))
{
    throw new InvalidOperationException("The Neow picker is missing v0.110.1's Massive Scroll bonus offer.");
}
Console.WriteLine("SearchTheSpire public-beta Neow parity checks passed.");

var board = SearchTheSpireBoardState.Empty;
var neowOptions = board.OptionsFor("neowOffer");
if (!neowOptions.Any(option => option.Id == "neowsbones"))
{
    throw new InvalidOperationException("The Neow picker is missing Neow's Bones.");
}

board = board.Select("neowOffer", "neowsbones");
var bonesChildren = board.VisibleChildren("neowOffer").Select(slot => slot.Id).ToHashSet();
if (!bonesChildren.Contains("bonesGrantA") || !bonesChildren.Contains("bonesGrantB") || !bonesChildren.Contains("bonesCurse"))
{
    throw new InvalidOperationException("Selecting Neow's Bones did not expose its child slots.");
}

if (bonesChildren.Contains("tabletCard"))
{
    throw new InvalidOperationException("Hefty Tablet's child slot leaked into an unrelated Neow offer.");
}

board = board.Select("bonesGrantA", "heftytablet");
if (!board.VisibleChildren("neowOffer").Any(slot => slot.Id == "bonesTabletCard"))
{
    throw new InvalidOperationException("Selecting Hefty Tablet did not expose its nested card constraint.");
}

var grantB = board.OptionsFor("bonesGrantB");
if (!grantB.Any(option => option.Id == "heftytablet" && option.Blocked))
{
    throw new InvalidOperationException("The second Bones grant did not block a duplicate relic.");
}

var filteredPicker = SearchTheSpirePicker.Filter(grantB, "tablet");
if (!filteredPicker.Any(option => option.Id == "heftytablet"))
{
    throw new InvalidOperationException("Picker search did not find a nested relic option.");
}

Console.WriteLine("SearchTheSpire hidden-slot checks passed.");

var hiddenCandidate = Enumerable.Range(0, 25_000)
    .Select(index => (Index: index, Snapshot: engine.Inspect(
        SeedSearchEngine.CreateSeed(SeedBranch.PublicBeta, index),
        SeedBranch.PublicBeta,
        generateMap: false)))
    .First(candidate => candidate.Snapshot.NeowOfferId == "neowsbones" &&
                        candidate.Snapshot.NeowGrantAId.Length > 0 &&
                        candidate.Snapshot.NeowGrantBId.Length > 0);
var hiddenQuery = query with
{
    Character = RunCharacter.Ironclad,
    StopAfter = 1,
    StartOffset = hiddenCandidate.Index,
    MaxCandidates = 1,
    HiddenSpec = $"char=ironclad,neowOffer=neowsbones,bonesGrantA={hiddenCandidate.Snapshot.NeowGrantAId},bonesGrantB={hiddenCandidate.Snapshot.NeowGrantBId}",
};
var hiddenMatches = engine.Search(hiddenQuery, CancellationToken.None);
if (hiddenMatches.Any(match =>
        match.Snapshot.NeowOfferId != "neowsbones" ||
        match.Snapshot.NeowGrantAId != hiddenCandidate.Snapshot.NeowGrantAId ||
        match.Snapshot.NeowGrantBId != hiddenCandidate.Snapshot.NeowGrantBId ||
        !match.Snapshot.NeowOffers.Contains("Neow's Bones", StringComparison.Ordinal)))
{
    throw new InvalidOperationException("Extended Neow pins and their result labels were not applied by the search engine.");
}

Console.WriteLine("SearchTheSpire hidden-filter checks passed.");

var bonesCurse = hiddenCandidate.Snapshot.DetailSpec
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .FirstOrDefault(fragment => fragment.StartsWith("bones_curse=", StringComparison.Ordinal))?
    .Split('=', 2)[1];
if (string.IsNullOrWhiteSpace(bonesCurse))
{
    throw new InvalidOperationException("Neow's Bones did not produce a deterministic curse detail.");
}

var detailQuery = hiddenQuery with
{
    MinimumElites = 0,
    MinimumShops = 0,
    MinimumRestSites = 0,
    NeowFilter = NeowFilter.Any,
    HiddenSpec = $"char=ironclad,neowOffer=neowsbones,bones_curse={bonesCurse}",
};
if (engine.Search(detailQuery, CancellationToken.None).Count != 1)
{
    throw new InvalidOperationException("A matching nested Neow detail was not accepted by the search engine.");
}

var wrongDetailQuery = detailQuery with { HiddenSpec = "char=ironclad,neowOffer=neowsbones,bones_curse=not-a-real-curse" };
if (engine.Search(wrongDetailQuery, CancellationToken.None).Count != 0)
{
    throw new InvalidOperationException("An incorrect nested Neow detail was silently accepted.");
}

Console.WriteLine("SearchTheSpire nested-detail checks passed.");

var advanced = SearchTheSpireBoardState.Empty.WithCharacter(RunCharacter.Ironclad).WithAscension(10);
if (!SearchTheSpireCatalog.ContainsSlot("act") ||
    !SearchTheSpireCatalog.ContainsSlot("boss1") ||
    !SearchTheSpireCatalog.ContainsSlot("ancient2") ||
    !SearchTheSpireCatalog.ContainsSlot("rewardPick1") ||
    !SearchTheSpireCatalog.ContainsSlot("shopPick1") ||
    !SearchTheSpireCatalog.ContainsSlot("bagPick1") ||
    !SearchTheSpireCatalog.ContainsSlot("eventPick1"))
{
    throw new InvalidOperationException("The board is missing SearchTheSpire's top-level run-start slots.");
}

advanced = advanced.Select("act", "1");
var bossOptions = advanced.OptionsFor("boss1");
if (!bossOptions.Any(option => option.Id == "waterfallgiant") ||
    bossOptions.Any(option => option.Id == "ceremonialbeast" && !option.Blocked))
{
    throw new InvalidOperationException("Act 1 boss options did not respect the selected map.");
}

advanced = advanced.Select("boss1", "waterfallgiant");
if (!advanced.ToSpec().Contains("act=1", StringComparison.Ordinal) ||
    !advanced.ToSpec().Contains("boss1=waterfallgiant", StringComparison.Ordinal))
{
    throw new InvalidOperationException("The act/boss board did not emit SearchTheSpire spec keys.");
}

advanced = advanced.Select("ancient2", "pael");
if (!advanced.VisibleChildren("ancient2").Any(slot => slot.Id == "ancient2Offers"))
{
    throw new InvalidOperationException("Selecting an ancient did not reveal its offer picker.");
}

advanced = advanced.Select("ancient2Offers", "paelsblood");
advanced = advanced.Select("rewardWithin", "3");
advanced = advanced.Select("rewardPick1", "bash");
advanced = advanced.Select("rewardPick2", "bash");
advanced = advanced.Select("shopPick1", "anchor");
advanced = advanced.Select("bagPick1", "anchor");
advanced = advanced.Select("eventPick1", "selfhelpbook");
var advancedSpec = advanced.ToSpec();
if (!advancedSpec.Contains("ancient2_offers=paelsblood", StringComparison.Ordinal) ||
    !advancedSpec.Contains("reward_within=3", StringComparison.Ordinal) ||
    !advancedSpec.Contains("reward_cards=bash+bash", StringComparison.Ordinal) ||
    !advancedSpec.Contains("shop_relic=anchor", StringComparison.Ordinal) ||
    !advancedSpec.Contains("bag_relic=anchor", StringComparison.Ordinal) ||
    !advancedSpec.Contains("event_in1=selfhelpbook", StringComparison.Ordinal))
{
    throw new InvalidOperationException("The extended board did not emit the SearchTheSpire package keys.");
}

if (advanced.OptionsFor("shopPick2").Any(option => option.Id == "anchor" && !option.Blocked) ||
    advanced.OptionsFor("bagPick2").Any(option => option.Id == "anchor" && !option.Blocked))
{
    throw new InvalidOperationException("Repeated shop/bag picks did not block duplicates.");
}

if (!SearchTheSpireCatalog.IsEnabled(SearchTheSpireCatalog.GetSlot("boss3b"), advanced) ||
    SearchTheSpireCatalog.IsEnabled(SearchTheSpireCatalog.GetSlot("boss3b"), advanced.WithAscension(0)))
{
    throw new InvalidOperationException("Act 3 second boss child state was not exposed at A10.");
}

Console.WriteLine("SearchTheSpire advanced-slot checks passed.");

var package = SearchTheSpireBoardState.Empty
    .WithCharacter(RunCharacter.Ironclad)
    .Select("rewardPick1", "bash")
    .Select("rewardPick2", "bash")
    .Select("rewardOrdered", "true");
var packageSpec = package.ToSpec();
if (!packageSpec.Contains("reward_within=2", StringComparison.Ordinal) ||
    !packageSpec.Contains("reward_cards=bash+bash", StringComparison.Ordinal) ||
    !packageSpec.Contains("reward_ordered", StringComparison.Ordinal))
{
    throw new InvalidOperationException("Reward package picks did not raise the window or emit ordered mode.");
}

var scarceSpec = package.WithAscension(7).ToSpec();
if (!scarceSpec.Contains("scarcity", StringComparison.Ordinal))
{
    throw new InvalidOperationException("Ascension 7 did not emit SearchTheSpire's scarcity modifier.");
}

var exactRewardSpec = SearchTheSpireBoardState.Empty
    .WithCharacter(RunCharacter.Ironclad)
    .Select("reward1", "bash")
    .ToSpec();
if (!exactRewardSpec.Contains("reward1_card=bash", StringComparison.Ordinal))
{
    throw new InvalidOperationException("Exact fight reward did not use SearchTheSpire's rewardN_card key.");
}

Console.WriteLine("SearchTheSpire package-compile checks passed.");

var capsuleState = SearchTheSpireBoardState.Empty
    .WithCharacter(RunCharacter.Ironclad)
    .Select("neowOffer", "neowsbones")
    .Select("bonesGrantA", "largecapsule");
var capsuleChildren = capsuleState.VisibleChildren("neowOffer").Select(slot => slot.Id).ToHashSet();
if (!capsuleChildren.Contains("bonesCapsuleSet1") ||
    !capsuleChildren.Contains("bonesCapsuleSet2") ||
    capsuleChildren.Contains("bonesCapsuleSet3"))
{
    throw new InvalidOperationException("Large Capsule did not expose exactly two Bones capsule pulls.");
}

var ancientConditional = SearchTheSpireBoardState.Empty
    .Select("ancient2", "pael");
var paelOptions = ancientConditional.OptionsFor("ancient2Offers");
var claw = paelOptions.FirstOrDefault(option => option.Id == "paelsclaw");
if (claw is null || !claw.IsConditional || claw.SpecKey != "ancient2_offers_if" ||
    !claw.Description!.Contains("Goopy", StringComparison.Ordinal))
{
    throw new InvalidOperationException("Conditional ancient offers were not presented with their predicate metadata.");
}

var conditionalSpec = ancientConditional.Select("ancient2Offers", "paelsclaw").ToSpec();
if (!conditionalSpec.Contains("ancient2_offers_if=paelsclaw", StringComparison.Ordinal))
{
    throw new InvalidOperationException("Conditional ancient offer did not compile to the _offers_if key.");
}

var eventMapState = SearchTheSpireBoardState.Empty
    .Select("eventPick1", "abyssalbaths");
var eventOptions = eventMapState.OptionsFor("eventPick2");
if (eventOptions.Any(option => option.Id == "aromaofchaos") ||
    !eventMapState.OptionsFor("act").Any(option => option.Id == "0" && option.Blocked))
{
    var eventMap = SearchTheSpireCatalog.EffectiveEventMap(eventMapState)?.ToString() ?? "null";
    var act0 = eventMapState.OptionsFor("act").First(option => option.Id == "0");
    throw new InvalidOperationException($"A map-exclusive event did not lock the event picker and Act 1 map choices: map={eventMap}, hasOvergrowthEvent={eventOptions.Any(option => option.Id == "aromaofchaos")}, act0Blocked={act0.Blocked}");
}

Console.WriteLine("SearchTheSpire dependency-gating checks passed.");

var engineDetail = engine.Inspect("000000000000", SeedBranch.PublicBeta, $"{SeedSearchEngine.PinnedGameApiVersion}|Ironclad|A0|Plain|");
var rewardOne = engineDetail.DetailSpec
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .First(fragment => fragment.StartsWith("reward1_card=", StringComparison.Ordinal))
    .Split('=', 2)[1];
var engineDetailQuery = query with
{
    Character = RunCharacter.Ironclad,
    StopAfter = 1,
    StartOffset = 0,
    MaxCandidates = 1,
    MinimumElites = 0,
    MinimumShops = 0,
    MinimumRestSites = 0,
    NeowFilter = NeowFilter.Any,
    AncientFilter = "Any",
    BossFilter = "Any",
    HiddenSpec = $"reward_within=1,reward_cards={rewardOne},reward1_card={rewardOne}",
};
if (engine.Search(engineDetailQuery, CancellationToken.None).Count != 1)
{
    throw new InvalidOperationException("The search engine did not understand SearchTheSpire's package and exact reward keys.");
}

var scarcityQuery = engineDetailQuery with { HiddenSpec = "scarcity" };
if (engine.Search(scarcityQuery, CancellationToken.None).Count != 0)
{
    throw new InvalidOperationException("A scarcity filter matched an A0 snapshot.");
}

Console.WriteLine("SearchTheSpire engine-key checks passed.");

var ironcladCards = SearchTheSpireBoardState.Empty
    .WithCharacter(RunCharacter.Ironclad)
    .OptionsFor("rewardPick1");
if (ironcladCards.Count < 80 ||
    !ironcladCards.Any(option => option.Id == "aggression" && option.Rarity == "Rare") ||
    ironcladCards.Any(option => option.Id is "bash" or "strike" or "defend" or "neutralize"))
{
    throw new InvalidOperationException("The character card picker did not use the versioned Ironclad pool.");
}

var sharedShop = SearchTheSpireBoardState.Empty.OptionsFor("shopPick1");
if (!sharedShop.Any(option => option.Id == "bread" && option.Section == "any character") ||
    !sharedShop.Any(option => option.OwnerCharacter == "Ironclad" && option.Id == "brimstone"))
{
    throw new InvalidOperationException("The charless shop picker did not expose shared and character-specific relic sections.");
}

Console.WriteLine("SearchTheSpire pool-section checks passed.");

// [issue-2] Neow card slots must only expose their real pools: Lost Coffer,
// New Leaf and Leafy Poultice roll from the versioned character pool, while
// Hefty Tablet and Arcane Scroll only ever offer that character's Rare cards.
var cofferState = SearchTheSpireBoardState.Empty
    .WithCharacter(RunCharacter.Ironclad)
    .Select("neowOffer", "lostcoffer");
var cofferOptions = cofferState.OptionsFor("cofferCard");
if (!cofferOptions.Any(option => option.Id == "aggression") ||
    cofferOptions.Any(option => option.Id is "strike" or "defend" or "bash" or "neutralize"))
{
    throw new InvalidOperationException("Lost Coffer offered impossible basic or cross-character cards.");
}

var tabletOptions = SearchTheSpireBoardState.Empty
    .WithCharacter(RunCharacter.Ironclad)
    .Select("neowOffer", "heftytablet")
    .OptionsFor("tabletCard");
var arcaneOptions = SearchTheSpireBoardState.Empty
    .WithCharacter(RunCharacter.Ironclad)
    .Select("neowOffer", "arcanescroll")
    .OptionsFor("arcaneCard");
if (!tabletOptions.Any(option => option.Id == "aggression") ||
    !tabletOptions.Any(option => option.Id == "darkembrace") ||
    tabletOptions.Any(option => option.Rarity != "Rare") ||
    !arcaneOptions.Any(option => option.Id == "aggression") ||
    arcaneOptions.Any(option => option.Rarity != "Rare"))
{
    throw new InvalidOperationException("Hefty Tablet / Arcane Scroll offered cards outside the character's Rare pool.");
}

var newleafOptions = SearchTheSpireBoardState.Empty
    .WithCharacter(RunCharacter.Ironclad)
    .Select("neowOffer", "newleaf")
    .OptionsFor("newleafCard");
var poulticeOptions = SearchTheSpireBoardState.Empty
    .WithCharacter(RunCharacter.Ironclad)
    .Select("neowOffer", "leafypoultice")
    .OptionsFor("poulticeCard1");
if (!newleafOptions.Any(option => option.Id == "aggression") ||
    newleafOptions.Any(option => option.Id is "strike" or "defend" or "bash" or "neutralize") ||
    !poulticeOptions.Any(option => option.Id == "aggression") ||
    poulticeOptions.Any(option => option.Id is "strike" or "defend" or "bash" or "neutralize"))
{
    throw new InvalidOperationException("New Leaf / Leafy Poultice offered impossible basic or cross-character cards.");
}

Console.WriteLine("SearchTheSpire Neow card-pool checks passed.");

var characterSwitch = SearchTheSpireBoardState.Empty
    .WithCharacter(RunCharacter.Ironclad)
    .Select("shopPick1", "brimstone")
    .Select("largeRelicA", "ruinedhelmet")
    .Select("rewardPick1", "aggression")
    .WithCharacter(RunCharacter.Silent);
if (characterSwitch.Selected("shopPick1") != null ||
    characterSwitch.Selected("largeRelicA") != null ||
    characterSwitch.Selected("rewardPick1") != null)
{
    throw new InvalidOperationException("Changing character kept selections that are not valid in the new character pools.");
}

var sharedAfterCharacterSwitch = characterSwitch
    .Select("shopPick1", "bread")
    .Selected("shopPick1");
if (sharedAfterCharacterSwitch != "bread")
{
    throw new InvalidOperationException("Changing character incorrectly removed a shared relic selection path.");
}

Console.WriteLine("SearchTheSpire character-switch checks passed.");

var charlessCapsule = SearchTheSpireBoardState.Empty.OptionsFor("largeRelicA");
if (charlessCapsule.Any(option => option.Blocked) ||
    !charlessCapsule.Any(option => option.Section == "any character" && option.Id == "anchor") ||
    !charlessCapsule.Any(option => option.OwnerCharacter == "Ironclad" && option.Id == "ruinedhelmet"))
{
    throw new InvalidOperationException("A charless capsule picker did not expose shared and character-specific relic sections.");
}

var inferredCapsuleCharacter = SearchTheSpireBoardState.Empty.Select("largeRelicA", "ruinedhelmet");
if (inferredCapsuleCharacter.Character != RunCharacter.Ironclad)
{
    throw new InvalidOperationException("Picking a character-specific capsule relic did not infer its character.");
}

Console.WriteLine("SearchTheSpire charless capsule checks passed.");

var groupedDetails = SearchTheSpireBoardState.Empty
    .WithCharacter(RunCharacter.Ironclad)
    .Select("neowOffer", "kaleidoscope")
    .Select("kaleidoCard1", "alignment");
if (groupedDetails.OptionsFor("kaleidoCard2").Any(option => option.Id == "alignment" && !option.Blocked))
{
    throw new InvalidOperationException("Kaleidoscope's second card picker allowed a duplicate card.");
}

var scrollboxDetails = SearchTheSpireBoardState.Empty
    .WithCharacter(RunCharacter.Ironclad)
    .Select("neowOffer", "scrollboxes")
    .Select("scrollboxCard1", "anger")
    .Select("scrollboxCard2", "armaments");
var scrollboxThird = scrollboxDetails.OptionsFor("scrollboxCard3");
if (scrollboxThird.Any(option => option.Id == "ironwave") ||
    !scrollboxThird.Any(option => option.Id == "ashenstrike"))
{
    throw new InvalidOperationException("Scroll Boxes did not enforce the one-bundle 2-common/1-uncommon constraint.");
}

var bonesCapsuleDetails = SearchTheSpireBoardState.Empty
    .WithCharacter(RunCharacter.Ironclad)
    .Select("neowOffer", "neowsbones")
    .Select("bonesGrantA", "largecapsule")
    .Select("bonesCapsuleSet1", "anchor");
if (bonesCapsuleDetails.OptionsFor("bonesCapsuleSet2").Any(option => option.Id == "anchor" && !option.Blocked))
{
    throw new InvalidOperationException("Bones capsule pulls allowed the same relic twice.");
}

Console.WriteLine("SearchTheSpire grouped-detail checks passed.");

var silentKaleidoBoard = SearchTheSpireBoardState.Empty
    .WithCharacter(RunCharacter.Silent)
    .Select("neowOffer", "kaleidoscope")
    .Select("kaleidoCard1", "rage")
    .Select("kaleidoCard2", "ironwave");
var silentKaleidoOptions = silentKaleidoBoard.OptionsFor("kaleidoCard1");
if (!silentKaleidoOptions.Any(option => option.Id == "rage" && option.OwnerCharacter == "other character") ||
    silentKaleidoBoard.OptionsFor("kaleidoCard2").Any(option => option.Id == "ironwave" && option.Blocked))
{
    throw new InvalidOperationException("Kaleidoscope pickers did not expose the other-character card pools.");
}

var silentKaleidoSpec = silentKaleidoBoard.ToSpec();
if (!silentKaleidoSpec.Contains("bonus=kaleidoscope", StringComparison.Ordinal) ||
    !silentKaleidoSpec.Contains("kaleido_distinct=rage+ironwave", StringComparison.Ordinal))
{
    throw new InvalidOperationException("Silent Kaleidoscope cards did not compile to the SearchTheSpire spec.");
}

var kaleidoFixture = engine.Inspect("PT0000000000", SeedBranch.PublicBeta, $"{SeedSearchEngine.PinnedGameApiVersion}|Silent|A10|Plain|");
var kaleidoDetail = kaleidoFixture.DetailSpec
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .FirstOrDefault(fragment => fragment.StartsWith("kaleido_distinct=", StringComparison.Ordinal))?
    .Split('=', 2)[1];
if (kaleidoDetail == null ||
    !kaleidoDetail.Split('+', StringSplitOptions.RemoveEmptyEntries).Contains("rage") ||
    !kaleidoDetail.Split('+', StringSplitOptions.RemoveEmptyEntries).Contains("ironwave"))
{
    throw new InvalidOperationException($"SearchTheSpire kaleidoscope detail diverged: {kaleidoFixture.DetailSpec}");
}

var kaleidoQuery = new SeedQuery(
    SeedBranch.PublicBeta,
    GameApiVersion: SeedSearchEngine.PinnedGameApiVersion,
    Character: RunCharacter.Silent,
    Ascension: 10,
    RunMode: RunMode.Plain,
    StopAfter: 1,
    StartOffset: 941L,
    MaxCandidates: 1,
    MinimumElites: 0,
    MinimumShops: 0,
    MinimumRestSites: 0,
    NeowFilter: NeowFilter.Any,
    AncientFilter: "Any",
    BossFilter: "Any",
    HiddenSpec: silentKaleidoSpec);
var kaleidoMatches = engine.Search(kaleidoQuery, CancellationToken.None);
if (kaleidoMatches.Count != 1 || kaleidoMatches[0].Seed != "PT0000000000")
{
    throw new InvalidOperationException("SearchTheSpire kaleidoscope query did not resolve the parity fixture.");
}

var kaleidoZeroMatches = engine.Search(kaleidoQuery with
{
    StartOffset = 0,
    MaxCandidates = 50_000,
}, CancellationToken.None);
var kaleidoZeroDetail = (kaleidoZeroMatches.FirstOrDefault()?.Snapshot.DetailSpec ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .FirstOrDefault(fragment => fragment.StartsWith("kaleido_distinct=", StringComparison.Ordinal))?
    .Split('=', 2)[1];
var kaleidoZeroCards = kaleidoZeroDetail?
    .Split('+', StringSplitOptions.RemoveEmptyEntries)
    .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
if (kaleidoZeroMatches.Count == 0 ||
    !kaleidoZeroCards.Contains("rage") ||
    !kaleidoZeroCards.Contains("ironwave"))
{
    throw new InvalidOperationException("A zero-offset Silent Kaleidoscope search returned no valid match.");
}

var kaleidoExact = engine.Inspect("00000001L0WX", SeedBranch.PublicBeta, $"{SeedSearchEngine.PinnedGameApiVersion}|Silent|A10|Plain|");
if (!kaleidoExact.DetailSpec.Contains(
        "kaleido_distinct=glacier+countdown+bludgeon+orbit+gofortheeyes+dansemacabre",
        StringComparison.Ordinal))
{
    throw new InvalidOperationException($"Kaleidoscope six-offer parity diverged: {kaleidoExact.DetailSpec}");
}

Console.WriteLine("SearchTheSpire kaleidoscope parity checks passed.");

var freshReward = SearchTheSpireBoardState.Empty
    .WithCharacter(RunCharacter.Ironclad)
    .Select("neowOffer", "kaleidoscope")
    .Select("rares", "3");
if (!freshReward.ToSpec().Contains("rares=3", StringComparison.Ordinal) ||
    SearchTheSpireBoardState.Empty.Select("rares", "3").ToSpec().Contains("rares=3", StringComparison.Ordinal))
{
    throw new InvalidOperationException("Fresh Neow reward rarity controls were not gated and compiled correctly.");
}

Console.WriteLine("SearchTheSpire fresh-reward checks passed.");

var rarePackage = SearchTheSpireBoardState.Empty
    .WithCharacter(RunCharacter.Ironclad)
    .Select("rewardPick1", "aggression");
if (rarePackage.Selected("rewardWithin") != "2" ||
    !rarePackage.WithAscension(7).ToSpec().Contains("reward_within=3", StringComparison.Ordinal))
{
    throw new InvalidOperationException($"Rare reward package floors did not follow {SeedSearchEngine.PinnedGameApiVersion} pity tables.");
}

var bagPackage = SearchTheSpireBoardState.Empty
    .WithCharacter(RunCharacter.Ironclad)
    .Select("bagPick1", "anchor")
    .Select("bagPick2", "beatingremnant");
if (!bagPackage.ToSpec().Contains("bag_within=1", StringComparison.Ordinal))
{
    throw new InvalidOperationException($"Bag picks from different rarity deques incorrectly competed for one floor: window={bagPackage.Selected("bagWithin")}, floor={SearchTheSpireCatalog.BagFloor(bagPackage)}");
}

var eventPackage = SearchTheSpireBoardState.Empty
    .Select("eventPick1", "selfhelpbook")
    .Select("eventPick2", "brainleech");
if (eventPackage.Selected("eventWithin") != "3" ||
    !eventPackage.ToSpec().Contains("event_in3=selfhelpbook", StringComparison.Ordinal))
{
    throw new InvalidOperationException("Multi-event picks did not apply SearchTheSpire's buffered question-room window.");
}

var capsuleSpec = capsuleState
    .Select("bonesCapsuleSet1", "anchor")
    .Select("bonesCapsuleSet2", "beatingremnant")
    .ToSpec();
if (!capsuleSpec.Contains("bones_capsule_set=anchor+beatingremnant", StringComparison.Ordinal))
{
    throw new InvalidOperationException("Bones capsule pulls did not compile as one ordered joint fragment.");
}

if (!SearchTheSpireBoardState.Empty.Select("ancient2", "darv").OptionsFor("ancient2Offers").Any(option => option.Id == "ectoplasm") ||
    SearchTheSpireBoardState.Empty.Select("ancient3", "darv").OptionsFor("ancient3Offers").Any(option => option.Id == "ectoplasm"))
{
    throw new InvalidOperationException("Darv's ancient offer pool did not respect its act-specific table.");
}

Console.WriteLine("SearchTheSpire package-floor checks passed.");

var restoredQueryState = SearchTheSpireBoardState.FromSpec(
    RunCharacter.Ironclad,
    10,
    "char=ironclad,neow=3,large_relic=ruinedhelmet+anchor,reward_within=3,reward_cards=aggression+bash,reward_ordered,shop_within=2,shop_relic=anchor,event_in2=selfhelpbook,ancient2=orobas,ancient2_offers=glasseye");
if (restoredQueryState.Selected("neowOffer") != "largecapsule" ||
    restoredQueryState.Selected("largeRelicA") != "ruinedhelmet" ||
    restoredQueryState.Selected("largeRelicB") != "anchor" ||
    restoredQueryState.Selected("rewardWithin") != "3" ||
    restoredQueryState.Selected("rewardPick1") != "aggression" ||
    restoredQueryState.Selected("rewardPick2") != "bash" ||
    restoredQueryState.Selected("rewardOrdered") != "true" ||
    restoredQueryState.Selected("shopPick1") != "anchor" ||
    restoredQueryState.Selected("shopWithin") != "2" ||
    restoredQueryState.Selected("eventPick1") != "selfhelpbook" ||
    restoredQueryState.Selected("eventWithin") != "2" ||
    restoredQueryState.Selected("ancient2") != "orobas" ||
    restoredQueryState.Selected("ancient2Offers") != "glasseye")
{
    throw new InvalidOperationException("Saved SearchTheSpire specs did not restore their board selections.");
}

Console.WriteLine("SearchTheSpire saved-query restore checks passed.");

var layout = new MapLayout(
    new List<MapNode>
    {
        new(3, 0, "ancient"),
        new(2, 1, "monster"),
        new(3, 15, "boss"),
    },
    new List<MapEdge> { new(0, 1), new(1, 2) },
    BossId: "vantom",
    AncientId: "neow");
var layoutJson = JsonSerializer.Serialize(layout);
var layoutBack = JsonSerializer.Deserialize<MapLayout>(layoutJson);
if (layoutBack == null ||
    !layoutBack.Nodes.SequenceEqual(layout.Nodes) ||
    !layoutBack.Edges.SequenceEqual(layout.Edges) ||
    layoutBack.BossId != "vantom" ||
    layoutBack.AncientId != "neow")
{
    throw new InvalidOperationException("Map layout JSON round-trip regressed.");
}

Console.WriteLine("Map layout JSON round-trip checks passed.");

var parityRows = JsonSerializer.Deserialize<List<LayoutParityRow>>(
    File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "SeedLayoutParity.json")))!;
var parityMismatches = new List<string>();
foreach (var row in parityRows)
{
    var snap = engine.Inspect(
        row.Seed,
        SeedBranch.PublicBeta,
        $"{SeedSearchEngine.PinnedGameApiVersion}|Any|A0|Plain|");
    var fields = new (string Name, string Actual, string Expected)[]
    {
        ("act1_map", snap.Act1MapId, row.Act1Map.ToString()),
        ("boss1", snap.Boss1Id, row.Boss1),
        ("boss2", snap.Boss2Id, row.Boss2),
        ("boss3", snap.Boss3Id, row.Boss3),
        ("boss3b", snap.Boss3BId, row.Boss3B),
        ("ancient2", snap.Ancient2Id, row.Ancient2),
        ("ancient3", snap.Ancient3Id, row.Ancient3),
    };
    fields = fields.Append(("elite", snap.EliteCount.ToString(), row.Elite.ToString()))
        .Append(("shop", snap.ShopCount.ToString(), row.Shop.ToString()))
        .Append(("rest", snap.RestSiteCount.ToString(), row.Rest.ToString()))
        .Append(("nodes", snap.Map?.Nodes.Count.ToString() ?? "null", row.Nodes.ToString()))
        .ToArray();
    var bad = fields
        .Where(pair => Norm(pair.Actual) != Norm(pair.Expected))
        .Select(pair => $"{pair.Name}:{Norm(pair.Actual)}!={Norm(pair.Expected)}")
        .ToArray();
    if (bad.Length > 0)
    {
        parityMismatches.Add($"{row.Seed}: {string.Join(", ", bad)}");
    }

    if (snap.Map == null || MapLayoutDigest(snap.Map) != row.LayoutDigest)
    {
        parityMismatches.Add($"{row.Seed}: layout_digest does not match the frozen v0.110.1 layout");
    }

    if (snap.Map == null ||
        !string.Equals(Norm(snap.Map.BossId), Norm(row.Boss1), StringComparison.Ordinal) ||
        !string.Equals(Norm(snap.Map.AncientId), "neow", StringComparison.Ordinal))
    {
        parityMismatches.Add(
            $"{row.Seed}: layout boss/ancient identity {snap.Map?.BossId}/{snap.Map?.AncientId} " +
            $"does not match {row.Boss1}/neow");
    }
}

if (parityMismatches.Count > 0)
{
    throw new InvalidOperationException(
        $"SearchTheSpire {SeedSearchEngine.PinnedGameApiVersion} layout parity failed for {parityMismatches.Count}/{parityRows.Count} seeds:\n" +
        string.Join("\n", parityMismatches.Take(8)));
}

var summaryMismatches = parityRows
    .Select(row =>
    {
        var snapshot = engine.Inspect(
            row.Seed,
            SeedBranch.PublicBeta,
            $"{SeedSearchEngine.PinnedGameApiVersion}|Any|A0|Plain|");
        var mapName = snapshot.Act1MapId == "1" ? "Underdocks" : "Overgrowth";
        var expected = $"{mapName} · {row.Nodes} nodes · {row.Elite}E / {row.Shop}$ / {row.Rest}R";
        return (Seed: row.Seed, Snapshot: snapshot, Expected: expected);
    })
    .Where(item => item.Snapshot.Act1Map != item.Expected)
    .Select(item => $"{item.Seed}: {item.Snapshot.Act1Map} != {item.Expected}")
    .ToArray();
if (summaryMismatches.Length > 0)
{
    throw new InvalidOperationException(
        "Reference Act 1 map summary diverged from the runtime summary shape:\n" +
        string.Join("\n", summaryMismatches.Take(5)));
}

Console.WriteLine("SearchTheSpire public-beta layout parity checks passed (map generation ported from v0.110.1).");

// [decompile audit] Act 1 event pools must contain exactly the act's AllEvents
// plus the shared events whose EventModel.IsAllowed returns true at act index 0.
// ModelDb.AllSharedEvents has 18 entries; the 10 omitted ones are act-locked
// (PotionCourier/Symbiote need act>0, RelicTrader/RanwidTheElder need act>0,
// CrystalSphere needs act>0 + 100 gold, StoneOfAllTime/WelcomeToWongos/DollRoom
// only appear in act 1, FakeMerchant needs act>=1, WarHistorianRepy never).
var overgrowthEvents = new HashSet<string>(new[]
{
    "aromaofchaos", "byrdonisnest", "densevegetation", "junglemazeadventure", "luminouschoir",
    "morphicgrove", "sapphireseed", "sunkenstatue", "tabletoftruth", "unrestsite", "wellspring",
    "whisperinghollow", "woodcarvings", "brainleech", "roomfullofcheese", "selfhelpbook",
    "slipperybridge", "teamaster", "thefutureofpotions", "thelegendsweretrue", "thisorthat",
});
var underdocksEvents = new HashSet<string>(new[]
{
    "abyssalbaths", "drowningbeacon", "endlessconveyor", "punchoff", "spiralingwhirlpool",
    "sunkenstatue", "sunkentreasury", "doorsoflightanddark", "trashheap", "waterloggedscriptorium",
    "brainleech", "roomfullofcheese", "selfhelpbook", "slipperybridge", "teamaster",
    "thefutureofpotions", "thelegendsweretrue", "thisorthat",
});
var actLockedSharedEvents = new[]
{
    "crystalsphere", "dollroom", "fakemerchant", "potioncourier", "ranwidtheelder", "relictrader",
    "stoneofalltime", "symbiote", "warhistorianrepy", "welcometowongos",
};
if (SeedSearchEngine.OvergrowthEvents.Length != overgrowthEvents.Count ||
    SeedSearchEngine.UnderdocksEvents.Length != underdocksEvents.Count ||
    SeedSearchEngine.OvergrowthEvents.Any(id => !overgrowthEvents.Contains(id)) ||
    SeedSearchEngine.UnderdocksEvents.Any(id => !underdocksEvents.Contains(id)) ||
    SeedSearchEngine.OvergrowthEvents.Any(actLockedSharedEvents.Contains) ||
    SeedSearchEngine.UnderdocksEvents.Any(actLockedSharedEvents.Contains))
{
    throw new InvalidOperationException(
        "Act 1 event pools diverged from the v0.110.1 decompiled AllEvents + IsAllowed sets.");
}

Console.WriteLine("Act 1 event pools match v0.110.1 decompiled eligibility sets.");

// The real StandardActMap target for A1+ is 8 elite rooms (SwarmingElites),
// while A0 keeps 5. The old reference engine drew these counts from an
// unrelated fake map RNG, so A10 searches could surface maps the game would
// never generate.
var a0Snapshot = engine.Inspect("000000000000", SeedBranch.PublicBeta, $"{SeedSearchEngine.PinnedGameApiVersion}|Any|A0|Plain|");
var a10Snapshot = engine.Inspect("000000000000", SeedBranch.PublicBeta, $"{SeedSearchEngine.PinnedGameApiVersion}|Any|A10|Plain|");
if (a0Snapshot.EliteCount != 5 ||
    a10Snapshot.EliteCount != 8 ||
    a10Snapshot.ShopCount != 3 ||
    a10Snapshot.RestSiteCount is < 6 or > 14)
{
    throw new InvalidOperationException(
        $"SwarmingElites map targets are wrong: A0 {a0Snapshot.EliteCount}E/{a0Snapshot.ShopCount}$/{a0Snapshot.RestSiteCount}R, " +
        $"A10 {a10Snapshot.EliteCount}E/{a10Snapshot.ShopCount}$/{a10Snapshot.RestSiteCount}R");
}

if (!a0Snapshot.HasCurse || !a0Snapshot.HasBlessing)
{
    throw new InvalidOperationException(
        "Neow blessing/curse category flags no longer report the always-present offers.");
}

// "猎人 a10 · 树叶药膏 · 肾上腺素 + 腐蚀波" must find seeds whose reference
// Act 1 map is the real game map, not the old fake route summary.
var exampleQuery = new SeedQuery(
    SeedBranch.PublicBeta,
    GameApiVersion: SeedSearchEngine.PinnedGameApiVersion,
    Character: RunCharacter.Silent,
    Ascension: 10,
    RunMode: RunMode.Plain,
    StopAfter: 3,
    StartOffset: 0,
    MaxCandidates: 200_000,
    MinimumElites: 0,
    MinimumShops: 0,
    MinimumRestSites: 0,
    NeowFilter: NeowFilter.Any,
    AncientFilter: "Any",
    BossFilter: "Any",
    HiddenSpec: "char=silent,neowOffer=leafypoultice,poultice_set=adrenaline+corrosivewave");
var exampleMatches = engine.Search(exampleQuery, CancellationToken.None);
if (exampleMatches.Count == 0)
{
    throw new InvalidOperationException("Silent A10 Leafy Poultice (Adrenaline + Corrosive Wave) found no seed.");
}

foreach (var match in exampleMatches)
{
    var map = match.Snapshot.Map;
    var poulticeSet = match.Snapshot.DetailSpec
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .FirstOrDefault(fragment => fragment.StartsWith("poultice_set=", StringComparison.Ordinal))?
        .Split('=', 2)[1];
    var poulticeCards = poulticeSet?
        .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    if (map == null ||
        match.Snapshot.EliteCount != 8 ||
        match.Snapshot.ShopCount != 3 ||
        match.Snapshot.RestSiteCount is < 6 or > 14 ||
        map.Nodes.Count <= 50 ||
        poulticeCards == null ||
        !poulticeCards.Contains("adrenaline") ||
        !poulticeCards.Contains("corrosivewave"))
    {
        throw new InvalidOperationException($"Example seed {match.Seed} has a non-game map: {match.Snapshot.Act1Map}");
    }
}

Console.WriteLine("SwarmingElites, Neow flags and Silent A10 Leafy Poultice example checks passed.");

// [issue-1] Parallel search must keep the same observable semantics as the
// sequential reference engine: identical seed sequence, exact StopAfter cap,
// monotonic progress and cancellation handling.
var issue1Query = new SeedQuery(
    SeedBranch.PublicBeta,
    GameApiVersion: SeedSearchEngine.PinnedGameApiVersion,
    Character: RunCharacter.Any,
    Ascension: 0,
    RunMode: RunMode.Plain,
    StopAfter: 5,
    StartOffset: 1000,
    MaxCandidates: 20_000,
    MinimumElites: 0,
    MinimumShops: 0,
    MinimumRestSites: 0,
    NeowFilter: NeowFilter.HasBlessing,
    AncientFilter: "Any",
    BossFilter: "Any");
var sequentialReference = SequentialSearchReference(engine, issue1Query, CancellationToken.None);
var parallelMatches = engine.Search(issue1Query, CancellationToken.None);
if (!sequentialReference.Select(match => match.Seed).SequenceEqual(parallelMatches.Select(match => match.Seed)) ||
    sequentialReference.Count != parallelMatches.Count)
{
    throw new InvalidOperationException(
        $"Parallel seed search diverged from the sequential reference: " +
        $"sequential={string.Join(",", sequentialReference.Select(match => match.Seed))}, " +
        $"parallel={string.Join(",", parallelMatches.Select(match => match.Seed))}");
}

if (parallelMatches.Count != issue1Query.StopAfter ||
    parallelMatches.Any(match => !match.Snapshot.HasBlessing))
{
    throw new InvalidOperationException("Parallel seed search did not honor StopAfter with filtered matches.");
}

var progressSnapshots = new List<SearchProgress>();
var progressMatches = engine.Search(
    issue1Query with { MaxCandidates = 20_000 },
    CancellationToken.None,
    progressSnapshots.Add);
long lastChecked = -1;
int lastMatchCount = -1;
var progressMonotonic = true;
foreach (var snapshot in progressSnapshots)
{
    progressMonotonic &= snapshot.Checked >= lastChecked && snapshot.MatchCount >= lastMatchCount;
    lastChecked = snapshot.Checked;
    lastMatchCount = snapshot.MatchCount;
}

if (progressSnapshots.Count == 0 ||
    !progressMonotonic ||
    progressSnapshots[^1].MatchCount != progressMatches.Count ||
    progressSnapshots[^1].Checked <= 0 ||
    progressSnapshots[^1].Checked > issue1Query.MaxCandidates)
{
    throw new InvalidOperationException("Parallel seed search progress is not monotonic or its final report is wrong.");
}

var fullScanProgress = new List<SearchProgress>();
var fullScanMatches = engine.Search(
    issue1Query with
    {
        StopAfter = 1000,
        MinimumElites = 0,
        MinimumShops = 0,
        MinimumRestSites = 0,
        NeowFilter = NeowFilter.Any,
        HiddenSpec = "boss1=__never__",
    },
    CancellationToken.None,
    fullScanProgress.Add);
if (fullScanProgress.Count == 0 ||
    fullScanProgress[^1].Checked != issue1Query.MaxCandidates ||
    fullScanMatches.Count != 0)
{
    throw new InvalidOperationException("Parallel seed search progress did not report a full-budget scan.");
}

var canceledToken = new CancellationToken(canceled: true);
var canceled = false;
try
{
    engine.Search(issue1Query with { MaxCandidates = 1 }, canceledToken);
}
catch (OperationCanceledException)
{
    canceled = true;
}

if (!canceled)
{
    throw new InvalidOperationException("Parallel seed search did not honor cancellation.");
}

Console.WriteLine("issue-1 parallel search parity/StopAfter/progress checks passed.");

var benchBase = new SeedQuery(
    SeedBranch.PublicBeta,
    GameApiVersion: SeedSearchEngine.PinnedGameApiVersion,
    Character: RunCharacter.Any,
    Ascension: 0,
    RunMode: RunMode.Plain,
    StopAfter: 1000,
    StartOffset: 0,
    MaxCandidates: 1,
    MinimumElites: 0,
    MinimumShops: 0,
    MinimumRestSites: 0,
    NeowFilter: NeowFilter.Any,
    AncientFilter: "Any",
    BossFilter: "Any",
    HiddenSpec: "boss1=__never__");
foreach (var budget in new long[] { 100_000L, 1_000_000L })
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var benchMatches = engine.Search(benchBase with { MaxCandidates = budget }, CancellationToken.None);
    stopwatch.Stop();
    Console.WriteLine($"BENCH {budget}: {stopwatch.Elapsed.TotalSeconds:F3}s {stopwatch.Elapsed.TotalMilliseconds / budget * 1000.0:F2}us/seed");
}

static string Norm(string value) =>
    new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

static string MapLayoutDigest(MapLayout map)
{
    var nodes = string.Join('|', map.Nodes.Select(node => $"{node.Col},{node.Row}:{node.Kind}"));
    var edges = string.Join(',', map.Edges.Select(edge => $"{edge.From}>{edge.To}"));
    return $"{nodes};;{edges}";
}

static IReadOnlyList<SeedMatch> SequentialSearchReference(
    SeedSearchEngine engine,
    SeedQuery query,
    CancellationToken cancellationToken)
{
    var matches = new List<SeedMatch>();
    var stopAfter = Math.Clamp(query.StopAfter, 1, 1000);
    var budget = Math.Max(1, query.MaxCandidates);
    var start = Math.Max(0, query.StartOffset);
    var context = $"{query.GameApiVersion}|{query.Character}|A{query.Ascension}|{query.RunMode}|{query.HiddenSpec}";
    var needsMap = query.MinimumElites > 0 || query.MinimumShops > 0 || query.MinimumRestSites > 0;

    for (long offset = 0; offset < budget && matches.Count < stopAfter; offset++)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var seed = SeedSearchEngine.CreateSeed(query.Branch, start + offset);
        var snapshot = engine.Inspect(seed, query.Branch, context, needsMap);
        if (SeedSearchEngine.Matches(query, snapshot))
        {
            var full = snapshot.Map == null ? engine.Inspect(seed, query.Branch, context) : snapshot;
            matches.Add(new SeedMatch(seed, full));
        }
    }

    return matches;
}

internal sealed record LayoutParityRow(
    [property: JsonPropertyName("seed")] string Seed,
    [property: JsonPropertyName("act1_map")] int Act1Map,
    [property: JsonPropertyName("boss1")] string Boss1,
    [property: JsonPropertyName("boss2")] string Boss2,
    [property: JsonPropertyName("boss3")] string Boss3,
    [property: JsonPropertyName("boss3b")] string Boss3B,
    [property: JsonPropertyName("ancient2")] string Ancient2,
    [property: JsonPropertyName("ancient3")] string Ancient3,
    [property: JsonPropertyName("elite")] int Elite,
    [property: JsonPropertyName("shop")] int Shop,
    [property: JsonPropertyName("rest")] int Rest,
    [property: JsonPropertyName("nodes")] int Nodes,
    [property: JsonPropertyName("layout_digest")] string LayoutDigest = "");
