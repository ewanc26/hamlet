using Hamlet.Sim.City;
using Xunit;

namespace Hamlet.Sim.Tests;

/// <summary>
/// Contract of the M2 road pathfinder (§7.2): shortest routes over road tiles,
/// congestion-weighted detours, deterministic repeats, and a clean refusal
/// whenever a route does not exist or does not fit the citizen's buffer.
/// </summary>
public class PathfinderTests
{
    private static WorldGrid GridWithSize(int size) => new(size);

    private static void LayRoad(WorldGrid grid, int x, int y) =>
        Assert.True(grid.PlaceRoad(x, y), $"road ({x},{y}) should be placeable");

    [Fact]
    public void FindsTheShortestLShapedRoute()
    {
        var grid = GridWithSize(16);
        for (var x = 1; x <= 7; x++)
            LayRoad(grid, x, 5);
        for (var y = 1; y <= 5; y++)
            if (y != 5)
                LayRoad(grid, 7, y);

        var pathfinder = new Pathfinder(grid);
        var route = new RoadPath();

        Assert.True(pathfinder.Find(1, 5, 7, 1, new Congestion(16), route));

        Assert.Equal(10, route.Length); // six east steps, four north steps
        Assert.Equal(1 * 16 + 7, route.TileAt(route.Length - 1));
        for (var step = 0; step < route.Length; step++)
            Assert.True(grid.IsRoad(route.TileAt(step) % 16, route.TileAt(step) / 16));
    }

    [Fact]
    public void RefusesWhenTheRoadsDoNotConnect()
    {
        var grid = GridWithSize(16);
        LayRoad(grid, 1, 1);
        LayRoad(grid, 5, 5);

        var route = new RoadPath();
        Assert.False(new Pathfinder(grid).Find(1, 1, 5, 5, new Congestion(16), route));
        Assert.Equal(0, route.Length);
    }

    [Fact]
    public void RefusesWhenAnEndpointIsNotOnARoad()
    {
        var grid = GridWithSize(16);
        LayRoad(grid, 1, 1);

        var route = new RoadPath();
        Assert.False(new Pathfinder(grid).Find(1, 1, 5, 5, new Congestion(16), route));
        Assert.False(new Pathfinder(grid).Find(9, 9, 1, 1, new Congestion(16), route));
    }

    [Fact]
    public void TakesTheClearDetourAroundAJammedStreet()
    {
        var grid = GridWithSize(16);
        for (var x = 1; x <= 5; x++)
            LayRoad(grid, x, 5);
        LayRoad(grid, 1, 4);
        LayRoad(grid, 1, 3);
        for (var x = 2; x <= 5; x++)
            LayRoad(grid, x, 3);
        LayRoad(grid, 5, 4);

        var congestion = new Congestion(16);
        for (var load = 0; load < Pathfinder.MaxCongestionLoad; load++)
            congestion.Add(3, 5);

        var route = new RoadPath();
        Assert.True(new Pathfinder(grid).Find(1, 5, 5, 5, congestion, route));

        Assert.Equal(8, route.Length); // the eight-tile arch, not the four-tile street
        Assert.DoesNotContain(5 * 16 + 3, Tiles(route)); // (3,5) is the jammed tile
    }

    [Fact]
    public void TheSameQueryAlwaysYieldsTheSameRoute()
    {
        var grid = GridWithSize(24);
        for (var x = 2; x <= 20; x++)
            LayRoad(grid, x, 12);
        for (var y = 2; y <= 20; y++)
            if (y != 12)
                LayRoad(grid, 12, y);
        var congestion = new Congestion(24);
        var pathfinder = new Pathfinder(grid);

        var first = new RoadPath();
        var second = new RoadPath();
        Assert.True(pathfinder.Find(2, 12, 12, 2, congestion, first));
        Assert.True(pathfinder.Find(2, 12, 12, 2, congestion, second));

        Assert.Equal(first.Length, second.Length);
        for (var step = 0; step < first.Length; step++)
            Assert.Equal(first.TileAt(step), second.TileAt(step));
    }

    [Fact]
    public void RefusesARouteLongerThanTheCitizensBuffer()
    {
        var grid = GridWithSize(16);
        for (var x = 1; x <= 10; x++)
            LayRoad(grid, x, 5);

        var route = new RoadPath(capacity: 4);
        Assert.False(new Pathfinder(grid).Find(1, 5, 10, 5, new Congestion(16), route));
        Assert.Equal(0, route.Length);
    }

    private static List<int> Tiles(RoadPath route)
    {
        var tiles = new List<int>(route.Length);
        for (var step = 0; step < route.Length; step++)
            tiles.Add(route.TileAt(step));
        return tiles;
    }
}
