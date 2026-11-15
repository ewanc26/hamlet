using System.Diagnostics;
using Hamlet.Sim;
using Hamlet.Sim.City;

// Headless harness: seed, tick count, and grid size from argv; prints a
// one-line smoke report with the state hash. usage: dotnet run -- 12345 10000 128
var seed = args.Length > 0 && ulong.TryParse(args[0], out var parsedSeed) ? parsedSeed : 12345;
var ticks = args.Length > 1 && long.TryParse(args[1], out var parsedTicks) ? parsedTicks : 10_000;
var gridSize = args.Length > 2 && int.TryParse(args[2], out var parsedGrid) ? parsedGrid : 128;

var simulation = new Simulation(seed, gridSize);

// A deterministic strip of road placements, as a player command stream would be.
for (var i = 0; i < 100; i++)
    simulation.Enqueue(new PlaceRoad { X = 5 + i, Y = 7 });

var stopwatch = Stopwatch.StartNew();
for (long tick = 0; tick < ticks; tick++)
    simulation.TickOnce();
stopwatch.Stop();

var hash = simulation.ComputeStateHash();
Console.WriteLine(
    $"seed={seed} grid={gridSize} ticks={simulation.Clock.Tick} roads={simulation.Grid.RoadCount} hash={hash}");
Console.WriteLine($"{simulation.Clock.Year:0000}-{simulation.Clock.Month:00}-{simulation.Clock.DayOfMonth:00}");
Console.WriteLine($"throughput={ticks / Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001):F0} ticks/s");
