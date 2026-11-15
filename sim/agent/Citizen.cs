using Hamlet.Sim.City;

namespace Hamlet.Sim.Agent;

/// <summary>
/// One persistent, autonomous citizen (§3): identity, place, trait-modulated
/// needs, and the plan currently executing. Behaviour belongs to the planner;
/// this atom only owns the state everything else reads and writes.
/// </summary>
public sealed class Citizen
{
    public long Id { get; init; }
    public long HouseholdId { get; set; }
    public int AgeYears { get; set; }
    public Needs Needs { get; init; } = new(initial: 80);
    public CitizenTraits Traits { get; init; } = CitizenTraits.Baseline;
    public bool IsAlive { get; private set; } = true;

    public int X { get; set; }
    public int Y { get; set; }
    public Plan Plan { get; set; }

    /// <summary>
    /// The road route being walked (§7.2). Empty means "walk the direct line":
    /// either no road connects the ends, or the citizen is not on the graph.
    /// </summary>
    public RoadPath Route { get; } = new();
    public long LastWorkDay { get; set; } = -1;

    /// <summary>The presentation view of this citizen, captured at a tick boundary (§4, rule 3).</summary>
    public CitizenSnapshot Snapshot() => new(
        Id,
        X,
        Y,
        Plan.Intent,
        Needs[NeedKind.Hunger],
        Needs[NeedKind.Energy]);

    public void Die() => IsAlive = false;
}
