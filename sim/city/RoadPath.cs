namespace Hamlet.Sim.City;

/// <summary>
/// One citizen's route over the road graph (§7.2): a fixed-capacity buffer of
/// tile indices walked forward by a cursor, so following a route allocates
/// nothing in the tick loop. The buffer is sized once per citizen and reused
/// on every re-plan; a route that would overrun it is refused rather than
/// silently truncated, and the caller falls back to a direct walk.
/// </summary>
public sealed class RoadPath
{
    public const int DefaultCapacity = 512;

    private readonly int[] _tiles;

    public RoadPath(int capacity = DefaultCapacity) => _tiles = new int[capacity];

    /// <summary>Tiles in the route, including the destination tile.</summary>
    public int Length { get; private set; }

    /// <summary>How many route tiles have already been reached.</summary>
    public int Cursor { get; private set; }

    public bool HasNext => Cursor < Length;

    public int NextTile => _tiles[Cursor];

    public int Remaining => Length - Cursor;

    public void Clear()
    {
        Length = 0;
        Cursor = 0;
    }

    /// <summary>Append a tile; false when the buffer is full (route refused).</summary>
    public bool Push(int tile)
    {
        if (Length >= _tiles.Length)
            return false;
        _tiles[Length++] = tile;
        return true;
    }

    public void Advance()
    {
        if (Cursor < Length)
            Cursor++;
    }

    /// <summary>Reverse the whole buffer — A* reconstructs destination-first.</summary>
    public void ReverseAll()
    {
        Array.Reverse(_tiles, 0, Length);
        Cursor = 0;
    }

    /// <summary>Drop the first tile (the citizen already stands on it).</summary>
    public void TrimFirst()
    {
        if (Length == 0)
            return;
        Array.Copy(_tiles, 1, _tiles, 0, Length - 1);
        Length--;
        if (Cursor > 0)
            Cursor--;
    }

    /// <summary>Expose the tile at a route position (tests and the state hash).</summary>
    public int TileAt(int index) => _tiles[index];
}
