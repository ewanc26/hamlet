namespace Hamlet.Sim.Agent;

/// <summary>
/// Data-driven need rates (§6.1). One tick is one sim minute, so decay is an
/// integer tick interval — fractional rate state never persists between ticks.
/// This table is the single source of truth for every need rate; activities
/// read their restore amounts from the same atom.
/// </summary>
public sealed class NeedRules
{
    public static NeedRules Default { get; } = new();

    private readonly int[] _decayEveryTicks =
    {
        12,  // Hunger — one point every 12 minutes (5 per hour).
        15,  // Energy — one point every 15 minutes awake (4 per hour).
        120, // Health.
        240, // Safety.
        60,  // Comfort.
        45,  // Social.
        40,  // Leisure.
        0,   // Wealth — a constraint, never a decaying need (§6.1).
    };

    /// <summary>Zero means the need does not decay on its own (wealth).</summary>
    public int DecayEveryTicks(NeedKind kind) => _decayEveryTicks[(int)kind];

    public int SleepTicksPerEnergyPoint => 2;
    public int MealTicks => 20;
    public int MealHungerRestore => 45;
}
