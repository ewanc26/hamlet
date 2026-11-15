using Hamlet.Sim.City;
using Hamlet.Sim.Core;
using Xunit;

namespace Hamlet.Sim.Tests;

public class WorldGridTests
{
    [Fact]
    public void GeneratedTerrainStaysOnTheHundredScale()
    {
        var random = new DeterministicRandom(42);
        var grid = new WorldGrid(64);
        TerrainGen.Generate(grid, random);

        for (var y = 0; y < grid.Size; y++)
        {
            for (var x = 0; x < grid.Size; x++)
            {
                Assert.InRange(grid.Get(TerrainLayer.Elevation, x, y), 0, 100);
                Assert.InRange(grid.Get(TerrainLayer.Water, x, y), 0, 100);
                Assert.InRange(grid.Get(TerrainLayer.Fertility, x, y), 0, 100);
                Assert.InRange(grid.Get(TerrainLayer.Ore, x, y), 0, 100);
            }
        }
    }

    [Fact]
    public void TerrainGenerationIsDeterministicPerSeed()
    {
        static ulong HashWithSeed(ulong seed)
        {
            var grid = new WorldGrid(64);
            TerrainGen.Generate(grid, new DeterministicRandom(seed));
            return grid.ComputeTerrainHash().Value;
        }

        Assert.Equal(HashWithSeed(7), HashWithSeed(7));
        Assert.NotEqual(HashWithSeed(7), HashWithSeed(8));
    }

    [Fact]
    public void RoadPlacementIsIdempotent()
    {
        var grid = new WorldGrid(16);
        Assert.True(grid.PlaceRoad(5, 5));
        Assert.False(grid.PlaceRoad(5, 5)); // replay-safe
        Assert.Equal(1, grid.RoadCount);
        Assert.True(grid.IsRoadAt(5, 5));
        Assert.False(grid.IsRoadAt(5, 6));
    }

    [Fact]
    public void OutOfBoundsQueriesNeverThrow()
    {
        var grid = new WorldGrid(16);
        Assert.False(grid.IsRoadAt(-1, -1));
        Assert.False(grid.IsRoadAt(16, 16));
        Assert.False(grid.PlaceRoad(-3, 99));
        Assert.Equal(0, grid.RoadCount);
    }
}
