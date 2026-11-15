using Hamlet.Sim;
using Hamlet.Sim.Agent;
using Hamlet.Sim.Core;
using Xunit;

namespace Hamlet.Sim.Tests;

/// <summary>
/// The M1 exit criterion (§10): one citizen sleeps, eats, and walks to work
/// unprompted through a full day cycle.
/// </summary>
public class DailyRoutineTests
{
    private sealed record DayRun(Simulation Simulation, HashSet<ActivityKind> Seen, int MaxX, string Trace);

    private static DayRun RunDay(ulong seed)
    {
        var simulation = new Simulation(seed, 64);
        simulation.Enqueue(new FoundHousehold
        {
            HomeX = 10,
            HomeY = 10,
            WorkX = 30,
            WorkY = 10,
            Members = 1,
        });

        var seen = new HashSet<ActivityKind>();
        var trace = new List<string>();
        var maxX = 10;
        for (var tick = 0; tick < Clock.TicksPerDay; tick++)
        {
            simulation.TickOnce();
            foreach (var simEvent in simulation.PendingEvents)
            {
                if (simEvent.Kind != SimEventKind.ActivityChanged)
                    continue;
                seen.Add((ActivityKind)simEvent.Activity);
                trace.Add($"{simEvent.Tick}:{(ActivityKind)simEvent.Activity}");
            }
            simulation.ClearEvents();
            maxX = Math.Max(maxX, simulation.Bodies.Citizens[0].X);
        }

        return new DayRun(simulation, seen, maxX, string.Join(";", trace));
    }

    [Fact]
    public void OneCitizenSleepsEatsAndWalksToWorkThroughADay()
    {
        var run = RunDay(12345);

        Assert.Contains(ActivityKind.Sleep, run.Seen);
        Assert.Contains(ActivityKind.Eat, run.Seen);
        Assert.Contains(ActivityKind.Work, run.Seen);
        Assert.Equal(30, run.MaxX); // commuted to the job site unprompted

        var citizen = Assert.Single(run.Simulation.Bodies.Citizens);
        for (var kind = 0; kind < Needs.Count; kind++)
            Assert.InRange(citizen.Needs[(NeedKind)kind], 0, 100);
        Assert.True(citizen.LastWorkDay >= 0, "the day's shift should have completed");
    }

    [Fact]
    public void TheSameSeedReplaysTheSameRoutine()
    {
        Assert.Equal(RunDay(7).Trace, RunDay(7).Trace);
        Assert.NotEqual(RunDay(7).Trace, RunDay(8).Trace);
    }
}
