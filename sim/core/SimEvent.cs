namespace Hamlet.Sim.Core;

public enum SimEventKind
{
    TickAdvanced,
    RoadPlaced,
    SettlementFounded,
}

/// <summary>
/// One sim→presentation notification, drained in batch at tick boundaries
/// (§7.6). Events are plain data; the host marshals them to GDScript as a
/// single array per tick — never per-agent per-frame signalling.
/// </summary>
public readonly struct SimEvent
{
    public SimEventKind Kind { get; init; }
    public long Tick { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
}
