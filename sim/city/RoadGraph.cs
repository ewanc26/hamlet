namespace Hamlet.Sim.City;

/// <summary>
/// Road topology over the grid: a tile is a junction when its road-neighbour
/// count is anything but 2. The hierarchical router (M2 target) consumes
/// junctions, never raw tiles, so graphs stay small at every scale (§6.3);
/// the first-cut router walks the road tiles themselves.
/// </summary>
public static class RoadGraph
{
    public static int CountRoadNeighbours(WorldGrid grid, int x, int y)
    {
        var count = 0;
        if (grid.IsRoadAt(x, y - 1)) count++;
        if (grid.IsRoadAt(x, y + 1)) count++;
        if (grid.IsRoadAt(x - 1, y)) count++;
        if (grid.IsRoadAt(x + 1, y)) count++;
        return count;
    }

    public static bool IsJunction(WorldGrid grid, int x, int y) =>
        grid.IsRoad(x, y) && CountRoadNeighbours(grid, x, y) != 2;

    public static List<(int X, int Y)> Junctions(WorldGrid grid)
    {
        var junctions = new List<(int X, int Y)>();
        for (var y = 0; y < grid.Size; y++)
            for (var x = 0; x < grid.Size; x++)
                if (IsJunction(grid, x, y))
                    junctions.Add((x, y));
        return junctions;
    }
}
