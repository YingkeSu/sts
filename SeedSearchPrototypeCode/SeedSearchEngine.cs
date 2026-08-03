using System.Text;

namespace SeedSearchPrototype;

/// <summary>
/// Deterministic seed engine for the first in-game UI pass.
///
/// This deliberately has a small, isolated seam so the current game's exact RNG
/// implementation can replace it without changing the UI and query model.
/// </summary>
public sealed class SeedSearchEngine
{
    private const string BetaAlphabet = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const int BetaSeedLength = 12;

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

        var state = HashSeed($"{branch}|{context}|{seed}");
        var eliteCount = 1 + (int)(Next(ref state) % 3);
        var shopCount = (int)(Next(ref state) % 3);
        var restSiteCount = 1 + (int)(Next(ref state) % 3);
        var hasBlessing = Next(ref state) % 3 != 0;
        var hasCurse = Next(ref state) % 4 == 0;

        var mapSlots = new[] { 'M', 'M', '?', '$', 'R', 'M', 'E', 'T', '?', 'M', 'B' };
        var map = new StringBuilder();
        foreach (var slot in mapSlots)
        {
            var roll = (int)(Next(ref state) % 100);
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

        var neow = hasBlessing
            ? hasCurse ? "Blessing / curse" : "Blessing / gold"
            : hasCurse ? "Curse / gold" : "Gold / relic";
        var ancients = $"Ancient {(char)('A' + (Next(ref state) % 4))}";
        var bossA = $"Boss {(Next(ref state) % 3) + 1}";
        var bossB = $"Boss {(Next(ref state) % 3) + 1}";

        return new SeedSnapshot(
            seed,
            $"{map}  ·  {eliteCount}E / {shopCount}$ / {restSiteCount}R",
            neow,
            ancients,
            $"{bossA} / {bossB}",
            eliteCount,
            shopCount,
            restSiteCount,
            hasBlessing,
            hasCurse);
    }

    private static string BuildContext(SeedQuery query) =>
        $"{query.GameApiVersion}|{query.Character}|A{query.Ascension}|{query.RunMode}";

    private static bool Matches(SeedQuery query, SeedSnapshot snapshot)
    {
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
               MatchesNamedFilter(snapshot.Ancients, query.AncientFilter) &&
               MatchesNamedFilter(snapshot.Bosses, query.BossFilter);
    }

    private static bool MatchesNamedFilter(string value, string filter) =>
        string.IsNullOrWhiteSpace(filter) || filter.Equals("Any", StringComparison.OrdinalIgnoreCase) || value.Contains(filter, StringComparison.OrdinalIgnoreCase);

    private static ulong HashSeed(string seed)
    {
        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        var hash = offset;
        foreach (var character in seed)
        {
            hash ^= character;
            hash *= prime;
        }

        return Mix(hash);
    }

    private static ulong Next(ref ulong state)
    {
        state += 0x9E3779B97F4A7C15UL;
        return Mix(state);
    }

    private static ulong Mix(ulong value)
    {
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }

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
}
