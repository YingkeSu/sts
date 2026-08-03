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
    string BossFilter);

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
    bool HasCurse);

public sealed record SeedMatch(string Seed, SeedSnapshot Snapshot);

public sealed record SearchProgress(long Checked, long CandidateBudget, int MatchCount);
