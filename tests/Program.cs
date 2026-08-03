using SeedSearchPrototype;

var engine = new SeedSearchEngine();
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

var hiddenQuery = query with
{
    Character = RunCharacter.Ironclad,
    StopAfter = 5,
    MaxCandidates = 25_000,
    HiddenSpec = "char=ironclad,neowOffer=neowsbones,bonesGrantA=heftytablet,bonesGrantB=arcanescroll",
};
var hiddenMatches = engine.Search(hiddenQuery, CancellationToken.None);
if (hiddenMatches.Any(match =>
        match.Snapshot.NeowOfferId != "neowsbones" ||
        match.Snapshot.NeowGrantAId != "heftytablet" ||
        match.Snapshot.NeowGrantBId != "arcanescroll" ||
        !match.Snapshot.NeowOffers.Contains("Neow's Bones", StringComparison.Ordinal)))
{
    throw new InvalidOperationException("Extended Neow pins and their result labels were not applied by the search engine.");
}

Console.WriteLine("SearchTheSpire hidden-filter checks passed.");
