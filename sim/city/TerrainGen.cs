using Hamlet.Sim.Core;

namespace Hamlet.Sim.City;

/// <summary>
/// Deterministic terrain generation from the world seed. Consumes
/// DeterministicRandom in a fixed order so identical seeds yield identical
/// terrain bytes — no noise-library dependency, no platform drift.
/// </summary>
public static class TerrainGen
{
    public static void Generate(WorldGrid grid, DeterministicRandom random)
    {
        var size = grid.Size;
        var elevation = new int[size * size];
        for (var i = 0; i < elevation.Length; i++)
            elevation[i] = random.Next(101);

        elevation = Smooth(elevation, size);
        elevation = Smooth(elevation, size);

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var i = y * size + x;
                var height = elevation[i];
                grid.Set(TerrainLayer.Elevation, x, y, height);
                grid.Set(TerrainLayer.Water, x, y, height < 25 ? 100 - height * 4 : 0);
                grid.Set(TerrainLayer.Fertility, x, y, Math.Min(100, 100 - height / 2 + random.Next(20)));
                grid.Set(TerrainLayer.Ore, x, y, random.Chance(5) ? 60 + random.Next(41) : random.Next(20));
            }
        }
    }

    private static int[] Smooth(int[] elevation, int size)
    {
        var smoothed = new int[elevation.Length];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var sum = 0;
                var count = 0;
                for (var dy = -1; dy <= 1; dy++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var nx = x + dx;
                        var ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= size || ny >= size)
                            continue;
                        sum += elevation[ny * size + nx];
                        count++;
                    }
                }
                smoothed[y * size + x] = sum / count;
            }
        }
        return smoothed;
    }
}
