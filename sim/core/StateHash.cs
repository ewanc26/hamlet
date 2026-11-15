namespace Hamlet.Sim.Core;

/// <summary>
/// Incremental FNV-1a 64-bit state fingerprint. Two identical runs (same seed,
/// same command stream) must produce identical hashes — the determinism gate
/// in CI asserts exactly that.
/// </summary>
public struct StateHash
{
    private const ulong Offset = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    private ulong _hash;

    public StateHash() => _hash = Offset;

    public ulong Value => _hash;

    public void Mix(ulong value)
    {
        _hash ^= value;
        _hash *= Prime;
    }

    public void Mix(int value) => Mix((ulong)value);

    public void Mix(long value) => Mix((ulong)value);

    public override string ToString() => $"0x{_hash:X16}";
}
