using Hamlet.Sim;
using Hamlet.Sim.Agent;
using Hamlet.Sim.City;
using Hamlet.Sim.Core;
using Xunit;

namespace Hamlet.Sim.Tests;

/// <summary>
/// The M2 exit criterion (§10): a citizen whose home and workplace are joined
/// only by a road that arches around a non-road block must walk the road, not
/// cut across the block — and the whole commute must replay bit-identically.
/// </summary>
public class RoadWalkTests
{
    // Home (2,2) and work (8,2) sit on a road that detours through y = 3; the
    // direct line between them crosses (3..7, 2), which is deliberately bare.
    private static Simulation ArchWorld(ulong seed)
    {
        var simulation = new Simulation(seed, 32);
        for (var x = 2; x <= 8; x++)
            simulation.Enqueue(new PlaceRoad { X = x, Y = 3 });
        simulation.Enqueue(new PlaceRoad { X = 2, Y = 2 });
        simulation.Enqueue(new PlaceRoad { X = 8, Y = 2 });
        simulation.Enqueue(new FoundHousehold
        {
            HomeX = 2,
            HomeY = 2,
            WorkX = 8,
            WorkY = 2,
            Members = 1,
        });
        // Founding is a command: the citizen exists once the first tick drains it.
        simulation.TickOnce();
        simulation.ClearEvents();
        return simulation;
    }

    [Fact]
    public void CitizenWalksTheRoadArchInsteadOfCuttingTheBlock()
    {
        var simulation = ArchWorld(12345);
        var citizen = simulation.Bodies.Citizens[0];
        var usedArch = false;

        for (var tick = 0; tick < Clock.TicksPerDay; tick++)
        {
            simulation.TickOnce();
            simulation.ClearEvents();

            Assert.True(
                simulation.Grid.IsRoad(citizen.X, citizen.Y),
                $"citizen stepped off the road at ({citizen.X},{citizen.Y}) on tick {tick}");
            usedArch |= citizen.Y == 3;
        }

        Assert.True(usedArch, "the commute should have detoured through the arch");
        Assert.True(citizen.LastWorkDay >= 0, "the citizen should have completed a shift across the arch");
    }

    [Fact]
    public void TheSameSeedReplaysTheSameRoutedCommute()
    {
        Assert.Equal(RunTrace(7), RunTrace(7));
        Assert.NotEqual(RunTrace(7), RunTrace(8));
    }

    private static string RunTrace(ulong seed)
    {
        var simulation = ArchWorld(seed);
        var trace = new List<string>();
        for (var tick = 0; tick < 2_000; tick++)
        {
            simulation.TickOnce();
            simulation.ClearEvents();
            var citizen = simulation.Bodies.Citizens[0];
            trace.Add($"{citizen.X},{citizen.Y},{(int)citizen.Plan.Intent},{citizen.Route.Cursor}");
        }
        trace.Add(simulation.ComputeStateHash().Value.ToString("X"));
        return string.Join(";", trace);
    }
}
