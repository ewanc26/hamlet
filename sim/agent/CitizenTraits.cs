using Hamlet.Sim.Core;

namespace Hamlet.Sim.Agent;

/// <summary>
/// Per-citizen trait modulation (§6.1). Every need has a decay interval and an
/// urgency weight, both integers on the shared 0-100 scale around a 50
/// baseline; diligence weights work. Rolling these at spawn makes a population
/// heterogeneous without per-citizen scripting.
/// </summary>
public sealed class CitizenTraits
{
    public const int Neutral = 50;

    private readonly int[] _weights;
    private readonly int[] _decayEveryTicks;

    /// <summary>Neutral traits: baseline weights and the default rules' intervals.</summary>
    public static CitizenTraits Baseline { get; } = new(NeedRules.Default);

    private CitizenTraits(NeedRules rules)
    {
        _weights = new int[Needs.Count];
        _decayEveryTicks = new int[Needs.Count];
        Array.Fill(_weights, Neutral);
        for (var kind = 0; kind < Needs.Count; kind++)
            _decayEveryTicks[kind] = rules.DecayEveryTicks((NeedKind)kind);
        Diligence = Neutral;
    }

    private CitizenTraits(int[] weights, int[] decayEveryTicks, int diligence)
    {
        _weights = weights;
        _decayEveryTicks = decayEveryTicks;
        Diligence = diligence;
    }

    public int Diligence { get; }

    public int Weight(NeedKind kind) => _weights[(int)kind];

    public int DecayEveryTicks(NeedKind kind) => _decayEveryTicks[(int)kind];

    /// <summary>Randomise every weight and decay interval from the world PRNG.</summary>
    public static CitizenTraits Roll(DeterministicRandom random, NeedRules rules)
    {
        var weights = new int[Needs.Count];
        var decayEveryTicks = new int[Needs.Count];
        for (var kind = 0; kind < Needs.Count; kind++)
        {
            weights[kind] = random.Next(25, 76);
            var baseInterval = rules.DecayEveryTicks((NeedKind)kind);
            if (baseInterval > 0)
            {
                var scale = random.Next(35, 66); // 35 = fast decay, 65 = slow decay
                decayEveryTicks[kind] = Math.Max(1, baseInterval * Neutral / scale);
            }
        }
        return new CitizenTraits(weights, decayEveryTicks, random.Next(25, 76));
    }
}
