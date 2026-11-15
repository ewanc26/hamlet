using Hamlet.Sim.City;
using Hamlet.Sim.Core;
using Xunit;

namespace Hamlet.Sim.Tests;

/// <summary>
/// The terrain capture is the world's one-time gift to the presentation layer
/// (§4, rule 3): it must carry every layer on the 0-100 scale and be a copy,
/// so the sim thread can keep working while the host marshals it.
/// </summary>
public class TerrainSnapshotTests
{
    [Fact]
    public void CaptureCarriesEveryLayerAndMatchesTheGrid()
    {
        var grid = new WorldGrid(32);
        TerrainGen.Generate(grid, new DeterministicRandom(99));

        var snapshot = grid.CaptureTerrain();

        Assert.Equal(grid.Size, snapshot.Size);
        Assert.Equal(grid.Size * grid.Size, snapshot.Layer(TerrainLayer.Elevation).Length);
        for (var y = 0; y < grid.Size; y++)
        {
            for (var x = 0; x < grid.Size; x++)
            {
                foreach (var layer in Enum.GetValues<TerrainLayer>())
                    Assert.Equal(grid.Get(layer, x, y), snapshot.Layer(layer)[y * grid.Size + x]);
            }
        }
    }

    [Fact]
    public void CaptureIsACopyNotALiveReference()
    {
        var grid = new WorldGrid(16);
        var before = grid.CaptureTerrain();

        // Mutating the grid after the capture must not change what was sent.
        grid.PlaceRoad(1, 1);
        var after = grid.CaptureTerrain();

        Assert.Equal(before.Layer(TerrainLayer.Elevation), after.Layer(TerrainLayer.Elevation));
        Assert.NotSame(before.Layer(TerrainLayer.Elevation), after.Layer(TerrainLayer.Elevation));
    }
}
