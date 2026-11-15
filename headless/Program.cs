using System.Diagnostics;
using Hamlet.Sim;
using Hamlet.Sim.Agent;
using Hamlet.Sim.City;
using Hamlet.Sim.Core;
using ActivityKind = Hamlet.Sim.Agent.ActivityKind;

// Headless harness: seed, tick count, and grid size from argv; prints a
// one-line smoke report with the state hash. usage: dotnet run -- 12345 10000 128
var seed = args.Length > 0 && ulong.TryParse(args[0], out var parsedSeed) ? parsedSeed : 12345;
var ticks = args.Length > 1 && long.TryParse(args[1], out var parsedTicks) ? parsedTicks : 10_000;
var gridSize = args.Length > 2 && int.TryParse(args[2], out var parsedGrid) ? parsedGrid : 128;

var simulation = new Simulation(seed, gridSize);

// A deterministic strip of road placements, as a player command stream would be.
for (var i = 0; i < 100; i++)
    simulation.Enqueue(new PlaceRoad { X = 5 + i, Y = 7 });

// M1: one settler household, home on the strip, job site at the far end.
simulation.Enqueue(new FoundHousehold { HomeX = 10, HomeY = 7, WorkX = 60, WorkY = 7, Members = 1 });

var activityTicks = new long[4];
var transitions = new List<string>();
var lastActivity = ActivityKind.None;
var maxX = 0;

var stopwatch = Stopwatch.StartNew();
for (long tick = 0; tick < ticks; tick++)
{
    simulation.TickOnce();

    foreach (var simEvent in simulation.PendingEvents)
    {
        if (simEvent.Kind != SimEventKind.ActivityChanged)
            continue;
        var activity = (ActivityKind)simEvent.Activity;
        if (activity == lastActivity)
            continue;
        lastActivity = activity;
        if (transitions.Count < 12)
            transitions.Add($"{simulation.Clock.DayOfMonth:00}/{simulation.Clock.Hour:00}:{simulation.Clock.Minute:00} {activity}");
    }
    simulation.ClearEvents();

    foreach (var citizen in simulation.Bodies.Citizens)
    {
        activityTicks[(int)citizen.Plan.Intent]++;
        maxX = Math.Max(maxX, citizen.X);
    }
}
stopwatch.Stop();

var hash = simulation.ComputeStateHash();
Console.WriteLine(
    $"seed={seed} grid={gridSize} ticks={simulation.Clock.Tick} roads={simulation.Grid.RoadCount} hash={hash}");
Console.WriteLine($"{simulation.Clock.Year:0000}-{simulation.Clock.Month:00}-{simulation.Clock.DayOfMonth:00}");
Console.WriteLine($"route={string.Join(" → ", transitions)}");
Console.WriteLine(
    $"activity ticks: idle={activityTicks[0]} sleep={activityTicks[1]} eat={activityTicks[2]} work={activityTicks[3]} maxX={maxX}");

var resident = simulation.Bodies.Citizens.FirstOrDefault();
if (resident is not null)
{
    Console.WriteLine(
        $"resident={resident.Id} at=({resident.X},{resident.Y}) activity={resident.Plan.Intent} " +
        $"hunger={resident.Needs[NeedKind.Hunger]} energy={resident.Needs[NeedKind.Energy]} " +
        $"meals={simulation.Households.Households[0].MealsStocked}");
}
Console.WriteLine($"throughput={ticks / Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001):F0} ticks/s");
