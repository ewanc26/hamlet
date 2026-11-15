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
    public long LastWorkDay { get; set; } = -1;

    public void Die() => IsAlive = false;
}
