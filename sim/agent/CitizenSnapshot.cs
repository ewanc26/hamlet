namespace Hamlet.Sim.Agent;

/// <summary>
/// Presentation-facing view of one citizen (§4, rule 3): plain values only,
/// captured at a tick boundary, never a live sim reference. The host marshals
/// these to GDScript in one batch.
/// </summary>
public readonly record struct CitizenSnapshot(
    long Id,
    int X,
    int Y,
    ActivityKind Activity,
    int Hunger,
    int Energy);
