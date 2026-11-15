namespace Hamlet.Sim.Agent;

public enum PlanPhase : byte
{
    Travel,
    Perform,
}

/// <summary>
/// A compiled intent (§6.2): the utility winner plus the primitive steps the
/// runner executes — travel to the target, then perform it. The shape is fixed
/// and the type is a value struct, so re-planning allocates nothing in the tick
/// loop (§8).
/// </summary>
public readonly struct Plan
{
    public ActivityKind Intent { get; init; }
    public PlanPhase Phase { get; init; }
    public int TargetX { get; init; }
    public int TargetY { get; init; }
    public int PhaseTicks { get; init; }

    public bool IsNone => Intent == ActivityKind.None;

    public Plan Advance() => new()
    {
        Intent = Intent,
        Phase = Phase,
        TargetX = TargetX,
        TargetY = TargetY,
        PhaseTicks = PhaseTicks + 1,
    };

    public Plan StartPerforming() => new()
    {
        Intent = Intent,
        Phase = PlanPhase.Perform,
        TargetX = TargetX,
        TargetY = TargetY,
        PhaseTicks = 0,
    };
}
