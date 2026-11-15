namespace Hamlet.Sim.Agent;

/// <summary>
/// One persistent, autonomous citizen (§3). Behaviour comes later (utility
/// scoring over the need vector); M0 establishes identity, mortality, and the
/// need state that everything else reads.
/// </summary>
public sealed class Citizen
{
    public long Id { get; init; }
    public long HouseholdId { get; set; }
    public int AgeYears { get; set; }
    public Needs Needs { get; init; } = new(initial: 80);
    public bool IsAlive { get; private set; } = true;

    public void Die() => IsAlive = false;
}
