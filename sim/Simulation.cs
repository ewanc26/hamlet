using Hamlet.Sim.Agent;
using Hamlet.Sim.City;
using Hamlet.Sim.Core;
using Hamlet.Sim.Economy;

namespace Hamlet.Sim;

/// <summary>
/// The whole simulation (§4). Owns clock, world, agents, and economy; accepts
/// a command queue; emits a batched event list per tick. Engine-agnostic by
/// construction: nothing here knows Godot exists.
/// </summary>
public sealed class Simulation
{
    public Clock Clock { get; } = new();
    public DeterministicRandom Random { get; }
    public WorldGrid Grid { get; }
    public LedgerBook LedgerBook { get; } = new();
    public AgentBodies Bodies { get; } = new();

    private readonly Queue<ISimCommand> _inbox = new();
    private readonly List<SimEvent> _outbox = new();
    private long _commandsApplied;

    public Simulation(ulong seed, int gridSize = 128)
    {
        Random = new DeterministicRandom(seed);
        Grid = new WorldGrid(gridSize);
        TerrainGen.Generate(Grid, Random);
    }

    public void Enqueue(ISimCommand command) => _inbox.Enqueue(command);

    public IReadOnlyList<SimEvent> PendingEvents => _outbox;

    public void ClearEvents() => _outbox.Clear();

    /// <summary>One fixed tick: drain commands, advance time, emit batched events.</summary>
    public void TickOnce()
    {
        while (_inbox.Count > 0)
        {
            var command = _inbox.Dequeue();
            Apply(command);
            _commandsApplied++;
        }

        Clock.Advance();
        _outbox.Add(new SimEvent { Kind = SimEventKind.TickAdvanced, Tick = Clock.Tick });
    }

    /// <summary>Rolls up everything that defines the world into one fingerprint (§7.6).</summary>
    public StateHash ComputeStateHash()
    {
        var hash = new StateHash();
        hash.Mix((ulong)Clock.Tick);
        hash.Mix(Grid.RoadCount);
        hash.Mix(Grid.ComputeTerrainHash().Value);
        hash.Mix((ulong)_commandsApplied);
        return hash;
    }

    private void Apply(ISimCommand command)
    {
        switch (command)
        {
            case PlaceRoad placeRoad:
                if (Grid.PlaceRoad(placeRoad.X, placeRoad.Y))
                    _outbox.Add(new SimEvent { Kind = SimEventKind.RoadPlaced, Tick = Clock.Tick, X = placeRoad.X, Y = placeRoad.Y });
                break;
            default:
                throw new NotSupportedException($"unknown command: {command.GetType()}");
        }
    }
}
