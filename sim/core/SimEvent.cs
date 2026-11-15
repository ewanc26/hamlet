namespace Hamlet.Sim.Core;

public enum SimEventKind
{
    TickAdvanced,
    RoadPlaced,
    SettlementFounded,
    HouseholdFounded,
    ActivityChanged,
}

/// <summary>
/// One sim→presentation notification, drained in batch at tick boundaries
/// (§7.6). Events are plain data; the host marshals them to GDScript as a
/// single array per tick — never per-agent per-frame signalling. Activity is
/// the byte value of <see cref="Agent.ActivityKind"/> so the core atom stays
/// free of presentation enums.
/// </summary>
public readonly struct SimEvent
{
    public SimEventKind Kind { get; init; }
    public long Tick { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public long CitizenId { get; init; }
    public long HouseholdId { get; init; }
    public byte Activity { get; init; }
}
