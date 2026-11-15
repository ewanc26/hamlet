using Hamlet.Sim.Core;

namespace Hamlet.Sim.City;

public enum TerrainLayer : byte
{
    Elevation,
    Water,
    Fertility,
    Ore,
}

/// <summary>
/// The world as layered grids (§3.3). Every layer lives on the shared 0-100
/// scale, quantised to bytes on write — the world never persists floats (§5.1).
/// </summary>
public sealed class WorldGrid
{
    public const int LayerCount = (int)TerrainLayer.Ore + 1;

    private readonly byte[][] _layers;
    private readonly bool[] _roads;

    public int Size { get; }
    public int RoadCount { get; private set; }

    public WorldGrid(int size)
    {
        Size = size;
        _layers = new byte[LayerCount][];
        for (var layer = 0; layer < LayerCount; layer++)
            _layers[layer] = new byte[size * size];
        _roads = new bool[size * size];
    }

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Size && y < Size;

    public int Get(TerrainLayer layer, int x, int y) => _layers[(int)layer][y * Size + x];

    public bool IsRoad(int x, int y) => _roads[y * Size + x];

    public bool IsRoadAt(int x, int y) => InBounds(x, y) && _roads[y * Size + x];

    /// <summary>Player road placement; idempotent so replayed commands stay safe.</summary>
    public bool PlaceRoad(int x, int y)
    {
        if (!InBounds(x, y) || _roads[y * Size + x])
            return false;
        _roads[y * Size + x] = true;
        RoadCount++;
        return true;
    }

    internal void Set(TerrainLayer layer, int x, int y, int value)
    {
        _layers[(int)layer][y * Size + x] = (byte)Math.Clamp(value, 0, 100);
    }

    public StateHash ComputeTerrainHash()
    {
        var hash = new StateHash();
        for (var layer = 0; layer < LayerCount; layer++)
        {
            var values = _layers[layer];
            for (var i = 0; i < values.Length; i++)
                hash.Mix(values[i]);
        }
        return hash;
    }
}
