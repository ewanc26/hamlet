using Hamlet.Sim.Core;

namespace Hamlet.Sim.City;

/// <summary>
/// Command atom: place one road tile. Commands are immutable data — the
/// save/replay format is just (seed, ordered command list) (§4).
/// </summary>
public sealed class PlaceRoad : ISimCommand
{
    public int X { get; init; }
    public int Y { get; init; }
}
