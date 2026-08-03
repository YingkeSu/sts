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

public sealed record SeedQuery(
    SeedBranch Branch,
    int StopAfter,
    long StartOffset,
    long MaxCandidates,
    int MinimumElites,
    int MinimumShops,
    int MinimumRestSites,
    NeowFilter NeowFilter);

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
