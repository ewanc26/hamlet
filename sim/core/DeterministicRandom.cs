namespace Hamlet.Sim.Core;

/// <summary>
/// The only source of randomness in the simulation (boundary contract, rule 6).
/// SplitMix64 seeding over a xoshiro256** core: integer-only, allocation-free,
/// and bit-exact on every platform. Never substitute System.Random or GD.RandRange.
/// </summary>
public sealed class DeterministicRandom
{
    private ulong _s0;
    private ulong _s1;
    private ulong _s2;
    private ulong _s3;

    public DeterministicRandom(ulong seed)
    {
        _s0 = Mix(seed);
        _s1 = Mix(seed + 0x9E3779B97F4A7C15UL);
        _s2 = Mix(seed + 0x3C6EF372FE94F82AUL);
        _s3 = Mix(seed + 0xDA6675A1CB2A3E0DUL);
    }

    public ulong NextUInt64()
    {
        ulong result = RotateLeft(_s1 * 5, 7) * 9;
        ulong t = _s1 << 17;
        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;
        _s2 ^= t;
        _s3 = RotateLeft(_s3, 45);
        return result;
    }

    public int Next(int maxExclusive) => (int)(NextUInt64() % (ulong)maxExclusive);

    public int Next(int minInclusive, int maxExclusive) => minInclusive + Next(maxExclusive - minInclusive);

    /// <summary>A percent chance on the shared 0-100 scale (§5.1).</summary>
    public bool Chance(int percent) => percent > 0 && Next(100) < percent;

    private static ulong Mix(ulong x)
    {
        x += 0x9E3779B97F4A7C15UL;
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
        return x ^ (x >> 31);
    }

    private static ulong RotateLeft(ulong x, int k) => (x << k) | (x >> (64 - k));
}
