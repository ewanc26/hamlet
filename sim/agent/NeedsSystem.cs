namespace Hamlet.Sim.Agent;

/// <summary>
/// Applies the data-driven need decay (§6.1). Each need ticks on its own
/// trait-modulated integer interval; the citizen id offsets the phase so the
/// whole population never decays on one tick. Nothing fractional persists.
/// </summary>
public static class NeedsSystem
{
    public static void Decay(Citizen citizen, long tick)
    {
        for (var kind = 0; kind < Needs.Count; kind++)
        {
            var interval = citizen.Traits.DecayEveryTicks((NeedKind)kind);
            if (interval <= 0)
                continue;
            if ((tick + citizen.Id) % interval == 0)
                citizen.Needs.Decay((NeedKind)kind, 1f);
        }
    }
}
