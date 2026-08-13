using System.Buffers.Binary;
using System.Text;

namespace SeedSearchPrototype;

/// <summary>
/// Xoshiro256** + XxHash64 port used by the public-beta reference engine.
/// These primitives mirror MegaRandom/Rng/StringHelper from the game assembly
/// (see docs/sts2-decompile/v0.110.1/ for the decompiled reference).
/// </summary>
internal static class Sts2ReferenceRng
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

    public static ulong HashCode64(ReadOnlySpan<byte> bytes)
    {
        var offset = 0;
        var length = bytes.Length;
        ulong hash;

        if (length >= 32)
        {
            var v1 = unchecked(Prime5 + Prime1 + Prime2);
            var v2 = unchecked(Prime5 + Prime2);
            var v3 = Prime5;
            var v4 = unchecked(Prime5 - Prime1);
            var limit = length - 32;
            while (offset <= limit)
            {
                v1 = Round(v1, BinaryPrimitives.ReadUInt64LittleEndian(bytes[offset..]));
                offset += 8;
                v2 = Round(v2, BinaryPrimitives.ReadUInt64LittleEndian(bytes[offset..]));
                offset += 8;
                v3 = Round(v3, BinaryPrimitives.ReadUInt64LittleEndian(bytes[offset..]));
                offset += 8;
                v4 = Round(v4, BinaryPrimitives.ReadUInt64LittleEndian(bytes[offset..]));
                offset += 8;
            }

            hash = RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);
            hash = MergeRound(hash, v1);
            hash = MergeRound(hash, v2);
            hash = MergeRound(hash, v3);
            hash = MergeRound(hash, v4);
        }
        else
        {
            hash = Prime5;
        }

        hash += (ulong)length;
        while (offset + 8 <= length)
        {
            hash ^= Round(0, BinaryPrimitives.ReadUInt64LittleEndian(bytes[offset..]));
            hash = RotateLeft(hash, 27) * Prime1 + Prime4;
            offset += 8;
        }

        if (offset + 4 <= length)
        {
            hash ^= BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]) * Prime1;
            hash = RotateLeft(hash, 23) * Prime2 + Prime3;
            offset += 4;
        }

        while (offset < length)
        {
            hash ^= bytes[offset] * Prime5;
            hash = RotateLeft(hash, 11) * Prime1;
            offset++;
        }

        hash ^= hash >> 33;
        hash *= Prime2;
        hash ^= hash >> 29;
        hash *= Prime3;
        return hash ^ (hash >> 32);
    }

    public static ulong HashCode64(string value) => HashCode64(Encoding.UTF8.GetBytes(value));

    public static RngState Create(ulong preseed)
    {
        var seed = preseed;
        return new RngState(NextState(ref seed), NextState(ref seed), NextState(ref seed), NextState(ref seed));
    }

    public static void Shuffle<T>(ref RngState state, IList<T> values)
    {
        for (var index = values.Count - 1; index > 0; index--)
        {
            var other = NextInt(ref state, index + 1);
            (values[index], values[other]) = (values[other], values[index]);
        }
    }

    public static double NextDouble(ref RngState state) =>
        (Next(ref state) >> 11) * 1.1102230246251565E-16;

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

    private static ulong Round(ulong accumulator, ulong input) =>
        RotateLeft(accumulator + input * Prime2, 31) * Prime1;

    private static ulong MergeRound(ulong accumulator, ulong value) =>
        (accumulator ^ Round(0, value)) * Prime1 + Prime4;

    private static ulong RotateLeft(ulong value, int bits) =>
        (value << bits) | (value >> (64 - bits));

    private const ulong Prime1 = 11400714785074694791UL;
    private const ulong Prime2 = 14029467366897019727UL;
    private const ulong Prime3 = 1609587929392839161UL;
    private const ulong Prime4 = 9650029242287828579UL;
    private const ulong Prime5 = 2870177450012600261UL;

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
