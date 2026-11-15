namespace Hamlet.Sim.City;

/// <summary>
/// A* over the road graph with congestion-weighted edges (§7.2). Movement is
/// 4-neighbour over road tiles only; the route cost is base + the tile's
/// current load, so a congested street is avoided whenever a clear detour is
/// cheaper. Integer-only and tie-broken by tile index, so the same query on
/// the same roads always yields the same route — replay stays bit-identical.
///
/// Scratch arrays are sized once with the world and stamped per query, so a
/// path query allocates nothing in the tick loop (§8).
/// </summary>
public sealed class Pathfinder
{
    /// <summary>Cost of one clear road tile, in integer units.</summary>
    public const int BaseStepCost = 10;

    /// <summary>Extra cost per citizen currently on a tile.</summary>
    public const int CongestionStepCost = 40;

    /// <summary>A jam deeper than this saturates rather than diverging.</summary>
    public const int MaxCongestionLoad = 8;

    private readonly WorldGrid _grid;
    private readonly int[] _cost;
    private readonly int[] _cameFrom;
    private readonly int[] _seen;
    private readonly int[] _closed;
    private readonly int[] _heapTiles;
    private readonly int[] _heapKeys;
    private int _heapCount;
    private int _generation;
    private int _goalX;
    private int _goalY;
    private int _startTile;

    public Pathfinder(WorldGrid grid)
    {
        _grid = grid;
        var cells = grid.Size * grid.Size;
        _cost = new int[cells];
        _cameFrom = new int[cells];
        _seen = new int[cells];
        _closed = new int[cells];
        _heapTiles = new int[cells + 1];
        _heapKeys = new int[cells + 1];
    }

    /// <summary>Nodes the last query closed — diagnostics for tests and perf probes.</summary>
    public int LastExpanded { get; private set; }

    /// <summary>
    /// Fill <paramref name="into"/> with the cheapest road route from start to
    /// end. Returns false (and leaves the route empty) when either end is not
    /// on a road or the roads do not connect — the caller walks directly.
    /// </summary>
    public bool Find(int startX, int startY, int endX, int endY, Congestion congestion, RoadPath into)
    {
        into.Clear();
        LastExpanded = 0;
        if (!_grid.InBounds(startX, startY) || !_grid.InBounds(endX, endY))
            return false;
        if (!_grid.IsRoad(startX, startY) || !_grid.IsRoad(endX, endY))
            return false;

        var start = startY * _grid.Size + startX;
        var end = endY * _grid.Size + endX;
        if (start == end)
            return true;

        _startTile = start;
        _goalX = endX;
        _goalY = endY;
        _generation++;
        _heapCount = 0;
        _seen[start] = _generation;
        _cost[start] = 0;
        _cameFrom[start] = -1;
        Push(start, Heuristic(startX, startY));

        while (_heapCount > 0)
        {
            var node = Pop();
            if (_closed[node] == _generation)
                continue;
            _closed[node] = _generation;
            if (node == end)
                return Reconstruct(end, into);

            LastExpanded++;
            var x = node % _grid.Size;
            var y = node / _grid.Size;
            Expand(node, x, y - 1, congestion);
            Expand(node, x, y + 1, congestion);
            Expand(node, x - 1, y, congestion);
            Expand(node, x + 1, y, congestion);
        }

        return false;
    }

    private void Expand(int fromNode, int x, int y, Congestion congestion)
    {
        if (!_grid.IsRoadAt(x, y))
            return;
        var neighbour = y * _grid.Size + x;
        if (_closed[neighbour] == _generation)
            return;
        var load = Math.Min(congestion.LoadAtTile(neighbour), MaxCongestionLoad);
        Relax(neighbour, fromNode, _cost[fromNode] + BaseStepCost + CongestionStepCost * load);
    }

    private void Relax(int node, int fromNode, int cost)
    {
        if (_seen[node] == _generation && cost >= _cost[node])
            return;
        _seen[node] = _generation;
        _cost[node] = cost;
        _cameFrom[node] = fromNode;
        Push(node, cost + Heuristic(node % _grid.Size, node / _grid.Size));
    }

    private bool Reconstruct(int end, RoadPath into)
    {
        var node = end;
        while (true)
        {
            if (!into.Push(node))
            {
                into.Clear(); // route too long for this citizen's buffer: walk directly
                return false;
            }
            if (node == _startTile)
                break;
            node = _cameFrom[node];
        }
        into.ReverseAll();
        into.TrimFirst(); // the citizen already stands on the start tile
        return true;
    }

    private int Heuristic(int x, int y) =>
        (Math.Abs(x - _goalX) + Math.Abs(y - _goalY)) * BaseStepCost;

    private void Push(int tile, int key)
    {
        var index = _heapCount++;
        _heapTiles[index] = tile;
        _heapKeys[index] = key;
        while (index > 0)
        {
            var parent = (index - 1) / 2;
            if (!HeapLess(index, parent))
                break;
            HeapSwap(index, parent);
            index = parent;
        }
    }

    private int Pop()
    {
        var top = _heapTiles[0];
        _heapCount--;
        if (_heapCount > 0)
        {
            _heapTiles[0] = _heapTiles[_heapCount];
            _heapKeys[0] = _heapKeys[_heapCount];
            var index = 0;
            while (true)
            {
                var left = 2 * index + 1;
                if (left >= _heapCount)
                    break;
                var smallest = left;
                var right = left + 1;
                if (right < _heapCount && HeapLess(right, left))
                    smallest = right;
                if (!HeapLess(smallest, index))
                    break;
                HeapSwap(index, smallest);
                index = smallest;
            }
        }
        return top;
    }

    /// <summary>Lower f first; ties by tile index so the heap order never drifts.</summary>
    private bool HeapLess(int a, int b) =>
        _heapKeys[a] < _heapKeys[b]
        || (_heapKeys[a] == _heapKeys[b] && _heapTiles[a] < _heapTiles[b]);

    private void HeapSwap(int a, int b)
    {
        (_heapTiles[a], _heapTiles[b]) = (_heapTiles[b], _heapTiles[a]);
        (_heapKeys[a], _heapKeys[b]) = (_heapKeys[b], _heapKeys[a]);
    }
}
