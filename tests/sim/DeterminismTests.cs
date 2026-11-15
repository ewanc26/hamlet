using Hamlet.Sim;
using Hamlet.Sim.City;
using Hamlet.Sim.Core;
using Xunit;

namespace Hamlet.Sim.Tests;

/// <summary>
/// THE gate (§7.6): the same seed and the same command stream must produce a
/// bit-identical state hash, and different seeds must diverge. If this test
/// fails, the save/replay promise is broken and the PR does not merge.
/// </summary>
public class DeterminismTests
{
    private static (ulong Hash, int Roads, int Citizens) RunWorld(ulong seed)
    {
        var simulation = new Simulation(seed, 64);
        for (var i = 0; i < 50; i++)
            simulation.Enqueue(new PlaceRoad { X = 5 + i, Y = 7 });
        simulation.Enqueue(new FoundHousehold { HomeX = 10, HomeY = 10, WorkX = 30, WorkY = 10, Members = 2 });
        for (long tick = 0; tick < 2_000; tick++)
        {
            simulation.TickOnce();
            simulation.ClearEvents();
        }
        return (simulation.ComputeStateHash().Value, simulation.Grid.RoadCount, simulation.Bodies.Citizens.Count);
    }

    [Fact]
    public void SameSeedYieldsIdenticalStateHash()
    {
        var first = RunWorld(12345);
        var second = RunWorld(12345);
        Assert.Equal(first.Hash, second.Hash);
        Assert.Equal(first.Roads, second.Roads);
        Assert.Equal(first.Citizens, second.Citizens);
    }

    [Fact]
    public void DifferentSeedsDiverge()
    {
        var first = RunWorld(12345);
        var second = RunWorld(54321);
        Assert.NotEqual(first.Hash, second.Hash);
    }

    [Fact]
    public void ReplayedCommandStreamIsIdempotent()
    {
        // Replaying a recorded command stream over the same seed must reproduce
        // the same world — the input-replay save format (§4) depends on it.
        var first = RunWorld(777);
        var second = RunWorld(777);
        Assert.Equal(first.Hash, second.Hash);
    }
}
