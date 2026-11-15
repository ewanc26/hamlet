using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Godot;
using Hamlet.Sim;
using Hamlet.Sim.City;
using Hamlet.Sim.Core;

namespace Hamlet.Host;

/// <summary>
/// The ONLY C# Node in the project (§4, rule 1). It owns the background sim
/// thread; GDScript reaches the simulation exclusively through
/// EnqueueCommand() and the EventsReady signal, batched at tick boundaries —
/// never per-frame per-agent calls across the boundary.
/// </summary>
public partial class SimHost : Node
{
    private const int TicksPerSecond = 15;

    [Signal]
    public delegate void EventsReadyEventHandler(Godot.Collections.Array events);

    private Simulation _simulation = null!;
    private readonly ConcurrentQueue<ISimCommand> _commands = new();
    private readonly ConcurrentQueue<(long Tick, int Roads)> _snapshots = new();
    private Thread _thread = null!;
    private volatile bool _running;

    public long Tick => _simulation?.Clock.Tick ?? 0;

    public override void _Ready()
    {
        _simulation = new Simulation(seed: 12345, gridSize: 64);
        _thread = new Thread(RunSimulation)
        {
            IsBackground = true,
            Name = "Hamlet.Sim",
        };
        _running = true;
        _thread.Start();
    }

    /// <summary>GDScript→sim command entry point, e.g. { "type": "place_road", "x": 10, "y": 4 }.</summary>
    public void EnqueueCommand(Godot.Collections.Dictionary command)
    {
        if (!command.ContainsKey("type"))
            return;
        switch ((string)command["type"])
        {
            case "place_road":
                _commands.Enqueue(new PlaceRoad { X = (int)command["x"], Y = (int)command["y"] });
                break;
        }
    }

    public override void _Process(double delta)
    {
        _ = delta;
        // Main thread only: convert thread-safe snapshots into Godot containers.
        while (_snapshots.TryDequeue(out var snapshot))
        {
            var events = new Godot.Collections.Array
            {
                new Godot.Collections.Dictionary
                {
                    ["tick"] = snapshot.Tick,
                    ["roads"] = snapshot.Roads,
                },
            };
            EmitSignal(SignalName.EventsReady, events);
        }
    }

    private void RunSimulation()
    {
        // Stopwatch pacing, not engine time: the sim never samples the frame
        // clock (§7.1), so identical command streams stay identical.
        var stopwatch = Stopwatch.StartNew();
        var nextTickAtMs = stopwatch.ElapsedMilliseconds;
        while (_running)
        {
            while (stopwatch.ElapsedMilliseconds < nextTickAtMs)
                Thread.Sleep(1);
            nextTickAtMs += 1000 / TicksPerSecond;

            while (_commands.TryDequeue(out var command))
                _simulation.Enqueue(command);

            _simulation.TickOnce();

            if (_simulation.Clock.Tick % TicksPerSecond == 0)
                _snapshots.Enqueue((_simulation.Clock.Tick, _simulation.Grid.RoadCount));
        }
    }

    public override void _ExitTree()
    {
        _running = false;
        _thread?.Join(1000);
    }
}
