namespace Hamlet.Sim.Agent;

public enum NeedKind : byte
{
    Hunger,
    Energy,
    Health,
    Safety,
    Comfort,
    Social,
    Leisure,
    Wealth,
}

/// <summary>
/// The citizen need vector (§3.1), all on the shared 0-100 scale. Values are
/// integers; float math is allowed mid-computation and quantised on write so
/// the world never persists floats (§5.1).
/// </summary>
public readonly struct Needs
{
    public const int Count = (int)NeedKind.Wealth + 1;

    private readonly int[] _values;

    public Needs(int initial = 100)
    {
        _values = new int[Count];
        Array.Fill(_values, Math.Clamp(initial, 0, 100));
    }

    public int this[NeedKind kind] => _values[(int)kind];

    /// <summary>Reduce a need by a rate (per-tick data values), quantised to the integer scale.</summary>
    public void Decay(NeedKind kind, float amount)
    {
        _values[(int)kind] = Quantise(_values[(int)kind] - amount);
    }

    /// <summary>Restore a need toward 100, quantised to the integer scale.</summary>
    public void Restore(NeedKind kind, float amount)
    {
        _values[(int)kind] = Quantise(_values[(int)kind] + amount);
    }

    public bool InCrisis(NeedKind kind, int threshold = 15) => _values[(int)kind] < threshold;

    private static int Quantise(float value) => Math.Clamp((int)MathF.Round(value), 0, 100);
}
