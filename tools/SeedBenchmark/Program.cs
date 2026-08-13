using System.Diagnostics;
using SeedSearchPrototype;

var engine = new SeedSearchEngine();
ulong sink = 0;

void Report(string line)
{
    Console.WriteLine(line);
    Console.Out.Flush();
}

Report("starting benchmark");

const int hashCount = 500_000;
var stopwatch = Stopwatch.StartNew();
for (var index = 0; index < hashCount; index++)
{
    sink ^= SeedSearchEngine.HashCode64(
        SeedSearchEngine.CreateSeed(SeedBranch.PublicBeta, 1_000_000L + index));
}

stopwatch.Stop();
Report(
    $"hash+codec string x{hashCount}: {stopwatch.Elapsed.TotalSeconds:F3}s " +
    $"{stopwatch.Elapsed.TotalSeconds / hashCount * 1e9:F0}ns/seed");

const int inspectCount = 200;
stopwatch.Restart();
for (var index = 0; index < inspectCount; index++)
{
    var seed = SeedSearchEngine.CreateSeed(SeedBranch.PublicBeta, 1_000_000L + index);
    var snapshot = engine.Inspect(
        seed,
        SeedBranch.PublicBeta,
        $"{SeedSearchEngine.PinnedGameApiVersion}|Any|A0|Plain|");
    sink ^= (ulong)StringComparer.Ordinal.GetHashCode(snapshot.Boss1Id + snapshot.NeowOfferId);
}

stopwatch.Stop();
Report(
    $"inspect full x{inspectCount}: {stopwatch.Elapsed.TotalSeconds:F3}s " +
    $"{stopwatch.Elapsed.TotalSeconds / inspectCount * 1e9:F0}ns/seed");

static SeedQuery Query(string hidden = "", string ancient = "Any", string boss = "Any") =>
    new(
        SeedBranch.PublicBeta,
        SeedSearchEngine.PinnedGameApiVersion,
        RunCharacter.Any,
        0,
        RunMode.Plain,
        StopAfter: 20,
        StartOffset: 1_000_000,
        MaxCandidates: 1_000_000,
        MinimumElites: 0,
        MinimumShops: 0,
        MinimumRestSites: 0,
        NeowFilter: NeowFilter.Any,
        AncientFilter: ancient,
        BossFilter: boss,
        HiddenSpec: hidden);

var cases = new (string Name, SeedQuery Query)[]
{
    ("empty filters", Query()),
    ("neow has-curse", Query() with { NeowFilter = NeowFilter.HasCurse }),
    ("char never (hash only)", Query(hidden: "char=__never__")),
    ("neow index never", Query(hidden: "neow=999")),
    ("boss1 never (hidden)", Query(hidden: "boss1=__never__")),
    ("detail never (hidden)", Query(hidden: "tablet_card=__never__")),
};

foreach (var (name, query) in cases)
{
    Report($"search {name}: starting");
    stopwatch.Restart();
    var matches = engine.Search(query, CancellationToken.None);
    stopwatch.Stop();
    Report($"search {name}: done");
    var scanned = matches.Count >= query.StopAfter ? query.StopAfter : query.MaxCandidates;
    Report(
        $"search {name}: {stopwatch.Elapsed.TotalSeconds:F3}s " +
        $"{stopwatch.Elapsed.TotalSeconds / scanned * 1e6:F2}us/seed " +
        $"{scanned / stopwatch.Elapsed.TotalSeconds:N0} runs/s matches={matches.Count}");
}

if (sink == 1)
{
    Report(sink.ToString());
}
