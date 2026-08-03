using SeedSearchPrototype;

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

if (first != second)
{
    throw new InvalidOperationException("Seed inspection is not deterministic.");
}

if (SeedSearchEngine.CreateSeed(SeedBranch.PublicBeta, 42).Length != 12)
{
    throw new InvalidOperationException("Beta seed codec did not produce a 12-character seed.");
}

var query = new SeedQuery(
    SeedBranch.PublicBeta,
    GameApiVersion: "0.110.1",
    Character: RunCharacter.Any,
    Ascension: 0,
    RunMode: RunMode.Plain,
    StopAfter: 3,
    StartOffset: 0,
    MaxCandidates: 10_000,
    MinimumElites: 2,
    MinimumShops: 1,
    MinimumRestSites: 1,
    NeowFilter: NeowFilter.HasBlessing,
    AncientFilter: "Any",
    BossFilter: "Any");
var matches = engine.Search(query, CancellationToken.None);

if (matches.Count > query.StopAfter || matches.Any(match => match.Snapshot.EliteCount < 2 || match.Snapshot.ShopCount < 1 || !match.Snapshot.HasBlessing))
{
    throw new InvalidOperationException("Seed search returned a result outside the query constraints.");
}

Console.WriteLine($"Seed search core checks passed: {matches.Count} matches.");

var betaNeowChecks = new Dictionary<string, (string Cursed, string BonusA, string BonusB, string? GrantA, string? GrantB)>
{
    ["000000000000"] = ("largecapsule", "newleaf", "phialholster", null, null),
    ["000000000001"] = ("dowsingrod", "arcanescroll", "kaleidoscope", null, null),
    ["000000000002"] = ("neowssacrifice", "boomingconch", "newleaf", null, null),
    ["000000000003"] = ("neowsbones", "pomander", "leadpaperweight", "lavarock", "nutritiousoyster"),
};
foreach (var (seed, expected) in betaNeowChecks)
{
    var snapshot = engine.Inspect(seed, SeedBranch.PublicBeta);
    var summaryParts = snapshot.NeowOffers.Split(" / ", StringSplitOptions.TrimEntries);
    if (snapshot.NeowOfferId != expected.Cursed ||
        summaryParts.Length < 3 ||
        !summaryParts[1].Equals(expected.BonusA, StringComparison.OrdinalIgnoreCase) ||
        !summaryParts[2].Equals(expected.BonusB, StringComparison.OrdinalIgnoreCase) ||
        snapshot.NeowGrantAId != (expected.GrantA ?? "") ||
        snapshot.NeowGrantBId != (expected.GrantB ?? ""))
    {
        throw new InvalidOperationException($"Public-beta Neow preview diverged for {seed}: {snapshot.NeowOffers} / {snapshot.NeowGrantAId}+{snapshot.NeowGrantBId}");
    }
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
    .Select(index => (Index: index, Snapshot: engine.Inspect(SeedSearchEngine.CreateSeed(SeedBranch.PublicBeta, index), SeedBranch.PublicBeta)))
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

var engineDetail = engine.Inspect("000000000000", SeedBranch.PublicBeta, "0.110.1|Ironclad|A0|Plain|");
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
    !ironcladCards.Any(option => option.Id == "bash"))
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
    throw new InvalidOperationException("Rare reward package floors did not follow v0.110.1 pity tables.");
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
