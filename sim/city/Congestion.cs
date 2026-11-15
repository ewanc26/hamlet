namespace Hamlet.Sim.City;

/// <summary>
/// Road-tile congestion (§7.2): how many citizens are on each tile right now.
/// The pathfinder prices a step at base + load, so routing adapts to jams
/// without re-planning the whole city — and because the load is rebuilt from
/// the fixed citizen order, the traffic picture is deterministic.
/// </summary>
public sealed class Congestion
{
    private readonly int[] _load;

    public Congestion(int size)
    {
        Size = size;
        _load = new int[size * size];
    }

    public int Size { get; }

    public void Reset() => Array.Clear(_load);

    public void Add(int x, int y) => _load[y * Size + x]++;

    public int LoadAt(int x, int y) => _load[y * Size + x];

    public int LoadAtTile(int tile) => _load[tile];
}
