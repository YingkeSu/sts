namespace SeedSearchPrototype;

public enum SeedBranch
{
    PublicBeta,
    Main
}

public enum NeowFilter
{
    Any,
    HasBlessing,
    HasCurse
}

public enum RunCharacter
{
    Any,
    Ironclad,
    Silent,
    Regent,
    Defect,
    Necrobinder
}

public enum RunMode
{
    Plain,
    CustomModifiers
}

public sealed record SeedQuery(
    SeedBranch Branch,
    string GameApiVersion,
    RunCharacter Character,
    int Ascension,
    RunMode RunMode,
    int StopAfter,
    long StartOffset,
    long MaxCandidates,
    int MinimumElites,
    int MinimumShops,
    int MinimumRestSites,
    NeowFilter NeowFilter,
    string AncientFilter,
    string BossFilter,
    string HiddenSpec = "");

public sealed record SeedSnapshot(
    string Seed,
    string Act1Map,
    string NeowOffers,
    string Ancients,
    string Bosses,
    int EliteCount,
    int ShopCount,
    int RestSiteCount,
    bool HasBlessing,
    bool HasCurse,
    string NeowOfferId = "",
    string NeowGrantAId = "",
    string NeowGrantBId = "",
    string Backend = "reference-rng",
    string Act1MapId = "",
    string Boss1Id = "",
    string Boss2Id = "",
    string Boss3Id = "",
    string Boss3BId = "",
    string Ancient2Id = "",
    string Ancient3Id = "",
    string Ancient2OfferId = "",
    string Ancient3OfferId = "",
    string RewardCardIds = "",
    string ShopRelicIds = "",
    string BagRelicIds = "",
    string EventIds = "",
    RunCharacter Character = RunCharacter.Any,
    string DetailSpec = "",
    int Ascension = 0,
    MapLayout? Map = null);

public sealed record MapNode(int Col, int Row, string Kind);

public sealed record MapEdge(int From, int To);

public sealed record MapLayout(List<MapNode> Nodes, List<MapEdge> Edges);

public sealed record SeedMatch(string Seed, SeedSnapshot Snapshot);

public sealed record SearchProgress(long Checked, long CandidateBudget, int MatchCount);
