using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Godot;
using Hamlet.Sim;
using Hamlet.Sim.Agent;
using Hamlet.Sim.City;
using Hamlet.Sim.Core;

namespace Hamlet.Host;

/// <summary>
/// The ONLY C# Node in the project (§4, rule 1). It owns the background sim
/// thread; GDScript reaches the simulation exclusively through EnqueueCommand()/
/// the public hooks and the EventsReady signal, batched at tick boundaries —
/// never per-frame per-agent calls across the boundary. Packets are packed
/// arrays, not dictionaries per citizen, so 1,000+ dots stay inside the budget.
/// </summary>
public partial class SimHost : Node
{
    private const int TicksPerSecond = 15;
    private const int GreyboxStripTiles = 32;
    private const int GreyboxCommuteTiles = 30;

    [Signal]
    public delegate void EventsReadyEventHandler(Godot.Collections.Array events);

    /// <summary>One tick-boundary packet: plain values captured on the sim thread.</summary>
    private readonly record struct Frame(
        long Tick,
        CitizenSnapshot[] Citizens,
        HouseholdSnapshot[] Households,
        int[] RoadTiles,
        bool IncludeWorld);

    private Simulation _simulation = null!;
    private TerrainSnapshot _terrain = null!;
    private (int X, int Y) _site;
    private bool _shopFounded;
    private bool _firmFounded;
    private readonly ConcurrentQueue<ISimCommand> _commands = new();
    private readonly ConcurrentQueue<Frame> _frames = new();
    private readonly List<CitizenSnapshot> _citizenBuffer = new();
    private readonly List<HouseholdSnapshot> _householdBuffer = new();
    private readonly List<int> _roadBuffer = new();
    private Thread _thread = null!;
    private volatile bool _running;

    public long Tick => _simulation?.Clock.Tick ?? 0;

    public override void _Ready()
    {
        _simulation = new Simulation(seed: 12345, gridSize: 64);
        _terrain = _simulation.Grid.CaptureTerrain();
        _site = FindSettlementSite(_simulation.Grid);
        FoundGreyboxSettlement(_site, members: 1);

        _thread = new Thread(RunSimulation)
        {
            IsBackground = true,
            Name = "Hamlet.Sim",
        };
        _running = true;
        _thread.Start();
    }

    /// <summary>
    /// Debug hook for the M0 in-engine budget check (§8): more dots walking the
    /// same road strip. Each extra settler is a real citizen in a real household.
    /// </summary>
    public void SpawnSettlers(int count) => FoundGreyboxSettlement(_site, members: Math.Max(1, count));

    /// <summary>
    /// Lay the greybox road strip and found the settler household on it. This is
    /// the stand-in for player zoning until M2 makes placement a player command.
    /// </summary>
    private void FoundGreyboxSettlement((int X, int Y) site, int members)
    {
        for (var i = 0; i <= GreyboxStripTiles; i++)
            _simulation.Enqueue(new PlaceRoad { X = site.X + i, Y = site.Y });
        if (!_shopFounded)
        {
            _simulation.Enqueue(new FoundShop { X = site.X + 2, Y = site.Y, OpeningCash = 600 });
            _shopFounded = true;
        }
        if (!_firmFounded)
        {
            _simulation.Enqueue(new FoundFirm { X = site.X + GreyboxCommuteTiles, Y = site.Y, OpeningCash = 3_000 });
            _firmFounded = true;
        }
        _simulation.Enqueue(new FoundHousehold
        {
            HomeX = site.X,
            HomeY = site.Y,
            WorkX = site.X + GreyboxCommuteTiles,
            WorkY = site.Y,
            Members = members,
        });
    }

    /// <summary>
    /// The first (closest to centre) fully dry, gently elevated row with room
    /// for the strip. A pure scan of the generated grid: same seed, same site.
    /// </summary>
    private static (int X, int Y) FindSettlementSite(WorldGrid grid)
    {
        var bestScore = int.MaxValue;
        var best = (X: grid.Size / 2 - GreyboxStripTiles / 2, Y: grid.Size / 2);
        for (var y = 0; y < grid.Size; y++)
        {
            for (var x = 0; x + GreyboxStripTiles < grid.Size; x++)
            {
                if (!IsSettleableRow(grid, x, y))
                    continue;
                var score = Math.Abs(x + GreyboxStripTiles / 2 - grid.Size / 2) + Math.Abs(y - grid.Size / 2) * 2;
                if (score >= bestScore)
                    continue;
                bestScore = score;
                best = (x, y);
            }
        }
        return best;
    }

    private static bool IsSettleableRow(WorldGrid grid, int x, int y)
    {
        for (var i = 0; i <= GreyboxStripTiles; i++)
        {
            if (grid.Get(TerrainLayer.Water, x + i, y) > 0)
                return false;
            var elevation = grid.Get(TerrainLayer.Elevation, x + i, y);
            if (elevation < 28 || elevation > 62)
                return false;
        }
        return true;
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
            case "set_focus":
                _commands.Enqueue(new SetCameraFocus
                {
                    X = (int)command["x"],
                    Y = (int)command["y"],
                    Radius = (int)command["radius"],
                });
                break;
        }
    }

    public override void _Process(double delta)
    {
        _ = delta;
        // Main thread only: convert thread-safe captures into Godot containers,
        // once per packet, never per agent per frame.
        while (_frames.TryDequeue(out var frame))
        {
            var packet = new Godot.Collections.Dictionary
            {
                ["tick"] = frame.Tick,
                ["roads"] = frame.RoadTiles.Length,
                ["citizens"] = CitizensPacket(frame.Citizens),
                ["households"] = HouseholdsPacket(frame.Households),
                ["road_tiles"] = frame.RoadTiles,
            };
            if (frame.IncludeWorld)
                packet["world"] = WorldPacket();
            EmitSignal(SignalName.EventsReady, new Godot.Collections.Array { packet });
        }
    }

    private static Godot.Collections.Dictionary CitizensPacket(CitizenSnapshot[] citizens)
    {
        var ids = new long[citizens.Length];
        var xs = new int[citizens.Length];
        var ys = new int[citizens.Length];
        var activities = new byte[citizens.Length];
        var hunger = new byte[citizens.Length];
        var energy = new byte[citizens.Length];
        for (var i = 0; i < citizens.Length; i++)
        {
            ids[i] = citizens[i].Id;
            xs[i] = citizens[i].X;
            ys[i] = citizens[i].Y;
            activities[i] = (byte)citizens[i].Activity;
            hunger[i] = (byte)citizens[i].Hunger;
            energy[i] = (byte)citizens[i].Energy;
        }
        return new Godot.Collections.Dictionary
        {
            ["ids"] = ids,
            ["xs"] = xs,
            ["ys"] = ys,
            ["activity"] = activities,
            ["hunger"] = hunger,
            ["energy"] = energy,
        };
    }

    private static Godot.Collections.Dictionary HouseholdsPacket(HouseholdSnapshot[] households)
    {
        var ids = new long[households.Length];
        var homeX = new int[households.Length];
        var homeY = new int[households.Length];
        var workX = new int[households.Length];
        var workY = new int[households.Length];
        var meals = new int[households.Length];
        for (var i = 0; i < households.Length; i++)
        {
            ids[i] = households[i].Id;
            homeX[i] = households[i].HomeX;
            homeY[i] = households[i].HomeY;
            workX[i] = households[i].WorkX;
            workY[i] = households[i].WorkY;
            meals[i] = households[i].MealsStocked;
        }
        return new Godot.Collections.Dictionary
        {
            ["ids"] = ids,
            ["home_x"] = homeX,
            ["home_y"] = homeY,
            ["work_x"] = workX,
            ["work_y"] = workY,
            ["meals"] = meals,
        };
    }

    /// <summary>The static world packet: sent once, on the first tick boundary.</summary>
    private Godot.Collections.Dictionary WorldPacket() => new()
    {
        ["size"] = _terrain.Size,
        ["elevation"] = _terrain.Layer(TerrainLayer.Elevation),
        ["water"] = _terrain.Layer(TerrainLayer.Water),
        ["fertility"] = _terrain.Layer(TerrainLayer.Fertility),
        ["ore"] = _terrain.Layer(TerrainLayer.Ore),
        ["focus_x"] = _site.X + GreyboxStripTiles / 2,
        ["focus_y"] = _site.Y,
    };

    private void RunSimulation()
    {
        // Stopwatch pacing, not engine time: the sim never samples the frame
        // clock (§7.1), so identical command streams stay identical.
        var stopwatch = Stopwatch.StartNew();
        var nextTickAtMs = stopwatch.ElapsedMilliseconds;
        var firstPacket = true;
        while (_running)
        {
            while (stopwatch.ElapsedMilliseconds < nextTickAtMs)
                Thread.Sleep(1);
            nextTickAtMs += 1000 / TicksPerSecond;

            while (_commands.TryDequeue(out var command))
                _simulation.Enqueue(command);

            _simulation.TickOnce();

            if (_simulation.Clock.Tick % TicksPerSecond == 0)
            {
                CaptureFrame(includeWorld: firstPacket);
                firstPacket = false;
            }

            // Snapshot packets carry the presentation state; the outbox is
            // drained so it never grows without bound. Per-event signal
            // forwarding (activity changes) rides with the M4 storyteller.
            _simulation.ClearEvents();
        }
    }

    private void CaptureFrame(bool includeWorld)
    {
        _citizenBuffer.Clear();
        foreach (var citizen in _simulation.Bodies.Citizens)
            _citizenBuffer.Add(citizen.Snapshot());

        _householdBuffer.Clear();
        foreach (var household in _simulation.Households.Households)
            _householdBuffer.Add(household.Snapshot());

        _roadBuffer.Clear();
        var grid = _simulation.Grid;
        for (var y = 0; y < grid.Size; y++)
            for (var x = 0; x < grid.Size; x++)
                if (grid.IsRoad(x, y))
                    _roadBuffer.Add(y * grid.Size + x);

        _frames.Enqueue(new Frame(
            _simulation.Clock.Tick,
            _citizenBuffer.ToArray(),
            _householdBuffer.ToArray(),
            _roadBuffer.ToArray(),
            includeWorld));
    }

    public override void _ExitTree()
    {
        _running = false;
        _thread?.Join(1000);
    }
}
