using Hamlet.Sim.Agent;
using Hamlet.Sim.City;
using Hamlet.Sim.Core;
using Hamlet.Sim.Economy;

namespace Hamlet.Sim;

/// <summary>
/// The whole simulation (§4). Owns clock, world, agents, households, and
/// economy; accepts a command queue; emits a batched event list per tick.
/// Engine-agnostic by construction: nothing here knows Godot exists.
/// </summary>
public sealed class Simulation
{
    private const int ReplanInterval = 60; // an hourly reconsideration (§6.2)

    public Clock Clock { get; } = new();
    public DeterministicRandom Random { get; }
    public WorldGrid Grid { get; }
    public Pathfinder Pathfinder { get; }
    public Congestion Congestion { get; }
    public LedgerBook LedgerBook { get; } = new();
    public ShopBook Shops { get; }
    public FirmBook Firms { get; }
    public AgentBodies Bodies { get; } = new();
    public HouseholdBook Households { get; } = new();
    public NeedRules Rules { get; } = NeedRules.Default;
    public DaySchedule Schedule { get; } = DaySchedule.Default;

    private const long HouseholdOpeningCash = 240;

    private readonly Queue<ISimCommand> _inbox = new();
    private readonly List<SimEvent> _outbox = new();
    private long _commandsApplied;

    public Simulation(ulong seed, int gridSize = 128)
    {
        Random = new DeterministicRandom(seed);
        Grid = new WorldGrid(gridSize);
        Pathfinder = new Pathfinder(Grid);
        Congestion = new Congestion(gridSize);
        Shops = new ShopBook(LedgerBook);
        Firms = new FirmBook(LedgerBook);
        TerrainGen.Generate(Grid, Random);
    }

    public void Enqueue(ISimCommand command) => _inbox.Enqueue(command);

    public IReadOnlyList<SimEvent> PendingEvents => _outbox;

    public void ClearEvents() => _outbox.Clear();

    /// <summary>One fixed tick: drain commands, advance time, run agents, emit batched events.</summary>
    public void TickOnce()
    {
        RebuildCongestion();

        while (_inbox.Count > 0)
        {
            var command = _inbox.Dequeue();
            Apply(command);
            _commandsApplied++;
        }

        Clock.Advance();
        _outbox.Add(new SimEvent { Kind = SimEventKind.TickAdvanced, Tick = Clock.Tick });

        foreach (var citizen in Bodies.Citizens)
            TickCitizen(citizen);
    }

    /// <summary>
    /// Rebuild road load from the fixed citizen order (§7.2): the traffic
    /// picture is a pure function of positions, so routing stays replayable.
    /// </summary>
    private void RebuildCongestion()
    {
        Congestion.Reset();
        foreach (var citizen in Bodies.Citizens)
            if (citizen.IsAlive && Grid.IsRoadAt(citizen.X, citizen.Y))
                Congestion.Add(citizen.X, citizen.Y);
    }

    private void TickCitizen(Citizen citizen)
    {
        if (!citizen.IsAlive)
            return;
        var household = Households.Find(citizen.HouseholdId);
        if (household is null)
            return; // citizens spawned outside a household are not simulated yet

        NeedsSystem.Decay(citizen, Clock.Tick);

        var previous = citizen.Plan.Intent;
        if ((Clock.Tick + citizen.Id) % ReplanInterval == 0 || InCrisis(citizen))
            Replan(citizen, household, allowHysteresis: true);

        if (ActivityRunner.Tick(citizen, household, Clock, Schedule, Rules, Grid.Size))
            Replan(citizen, household, allowHysteresis: false);

        if (citizen.Plan.Intent == previous)
            return;

        _outbox.Add(new SimEvent
        {
            Kind = SimEventKind.ActivityChanged,
            Tick = Clock.Tick,
            CitizenId = citizen.Id,
            Activity = (byte)citizen.Plan.Intent,
            X = citizen.X,
            Y = citizen.Y,
        });
    }

    private static bool InCrisis(Citizen citizen) =>
        (citizen.Needs.InCrisis(NeedKind.Hunger) && citizen.Plan.Intent != ActivityKind.Eat)
        || (citizen.Needs.InCrisis(NeedKind.Energy) && citizen.Plan.Intent != ActivityKind.Sleep);

    private void Replan(Citizen citizen, Household household, bool allowHysteresis)
    {
        var best = UtilityScorer.Best(citizen, household, Clock, Schedule);
        var current = citizen.Plan;

        if (allowHysteresis && !current.IsNone && current.Intent != best)
        {
            // Sleep is only interrupted by a need crisis: a moderate need at
            // 3 a.m. is not worth waking for (§6.2 plan robustness).
            if (current.Intent == ActivityKind.Sleep && Schedule.IsNight(Clock.Hour) && !InCrisis(citizen))
                return;

            var currentScore = UtilityScorer.Score(current.Intent, citizen, household, Clock, Schedule);
            var bestScore = UtilityScorer.Score(best, citizen, household, Clock, Schedule);
            if (bestScore <= currentScore + UtilityScorer.SwitchMargin)
                return;
        }

        citizen.Plan = IntentPlanner.Compile(best, citizen, household, Pathfinder, Congestion);
    }

    /// <summary>Rolls up everything that defines the world into one fingerprint (§7.6).</summary>
    public StateHash ComputeStateHash()
    {
        var hash = new StateHash();
        hash.Mix((ulong)Clock.Tick);
        hash.Mix(Grid.RoadCount);
        hash.Mix(Grid.ComputeTerrainHash().Value);
        hash.Mix((ulong)_commandsApplied);

        foreach (var household in Households.Households)
        {
            hash.Mix(household.Id);
            hash.Mix(household.HomeX);
            hash.Mix(household.HomeY);
            hash.Mix(household.WorkX);
            hash.Mix(household.WorkY);
            hash.Mix(household.MealsStocked);
            hash.Mix(household.Wallet?.Balance ?? 0);
            hash.Mix(household.CornerShop?.Id ?? 0);
            hash.Mix(household.Employer?.Id ?? 0);
        }

        foreach (var shop in Shops.Shops)
        {
            hash.Mix(shop.Id);
            hash.Mix(shop.X);
            hash.Mix(shop.Y);
            hash.Mix(shop.MealsInStock);
            hash.Mix(shop.Ledger.Balance);
        }

        foreach (var firm in Firms.Firms)
        {
            hash.Mix(firm.Id);
            hash.Mix(firm.X);
            hash.Mix(firm.Y);
            hash.Mix(firm.Ledger.Balance);
        }

        foreach (var citizen in Bodies.Citizens)
        {
            hash.Mix(citizen.Id);
            hash.Mix(citizen.HouseholdId);
            hash.Mix(citizen.IsAlive ? 1 : 0);
            hash.Mix(citizen.X);
            hash.Mix(citizen.Y);
            hash.Mix(citizen.LastWorkDay);
            hash.Mix(citizen.Traits.Diligence);
            for (var kind = 0; kind < Needs.Count; kind++)
            {
                hash.Mix(citizen.Needs[(NeedKind)kind]);
                hash.Mix(citizen.Traits.Weight((NeedKind)kind));
                hash.Mix(citizen.Traits.DecayEveryTicks((NeedKind)kind));
            }

            var plan = citizen.Plan;
            hash.Mix((int)plan.Intent);
            hash.Mix((int)plan.Phase);
            hash.Mix(plan.TargetX);
            hash.Mix(plan.TargetY);
            hash.Mix(plan.PhaseTicks);

            hash.Mix(citizen.Route.Length);
            hash.Mix(citizen.Route.Cursor);
            for (var step = citizen.Route.Cursor; step < citizen.Route.Length; step++)
                hash.Mix(citizen.Route.TileAt(step));
        }

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
            case FoundHousehold found:
                Found(found);
                break;
            case FoundShop foundShop:
                FoundShop(foundShop);
                break;
            case FoundFirm foundFirm:
                FoundFirm(foundFirm);
                break;
            default:
                throw new NotSupportedException($"unknown command: {command.GetType()}");
        }
    }

    private void Found(FoundHousehold command)
    {
        if (!Grid.InBounds(command.HomeX, command.HomeY) || !Grid.InBounds(command.WorkX, command.WorkY))
            throw new ArgumentOutOfRangeException(nameof(command), "household coordinates must be inside the world");

        var household = Households.Found(
            command.HomeX, command.HomeY, command.WorkX, command.WorkY, command.Meals);
        household.Wallet = LedgerBook.OpenLedger(HouseholdOpeningCash);
        household.CornerShop = Shops.Nearest(household.HomeX, household.HomeY);
        household.Employer = Firms.Nearest(household.WorkX, household.WorkY);
        var members = Math.Max(1, command.Members);
        for (var i = 0; i < members; i++)
        {
            var citizen = Bodies.SpawnCitizen(CitizenTraits.Roll(Random, Rules));
            citizen.HouseholdId = household.Id;
            citizen.X = household.HomeX;
            citizen.Y = household.HomeY;
            citizen.Plan = IntentPlanner.Compile(
                UtilityScorer.Best(citizen, household, Clock, Schedule), citizen, household, Pathfinder, Congestion);
            _outbox.Add(new SimEvent
            {
                Kind = SimEventKind.ActivityChanged,
                Tick = Clock.Tick,
                CitizenId = citizen.Id,
                Activity = (byte)citizen.Plan.Intent,
                X = citizen.X,
                Y = citizen.Y,
            });
        }

        _outbox.Add(new SimEvent
        {
            Kind = SimEventKind.HouseholdFounded,
            Tick = Clock.Tick,
            HouseholdId = household.Id,
            X = household.HomeX,
            Y = household.HomeY,
        });
    }

    private void FoundShop(FoundShop command)
    {
        if (!Grid.InBounds(command.X, command.Y))
            throw new ArgumentOutOfRangeException(nameof(command), "shop coordinates must be inside the world");

        var shop = Shops.Found(command.X, command.Y, command.Meals, command.OpeningCash);
        foreach (var household in Households.Households)
            household.CornerShop ??= Shops.Nearest(household.HomeX, household.HomeY);

        _outbox.Add(new SimEvent
        {
            Kind = SimEventKind.ShopFounded,
            Tick = Clock.Tick,
            X = shop.X,
            Y = shop.Y,
        });
    }

    private void FoundFirm(FoundFirm command)
    {
        if (!Grid.InBounds(command.X, command.Y))
            throw new ArgumentOutOfRangeException(nameof(command), "firm coordinates must be inside the world");

        var firm = Firms.Found(command.X, command.Y, command.OpeningCash);
        foreach (var household in Households.Households)
            household.Employer ??= Firms.Nearest(household.WorkX, household.WorkY);

        _outbox.Add(new SimEvent
        {
            Kind = SimEventKind.FirmFounded,
            Tick = Clock.Tick,
            X = firm.X,
            Y = firm.Y,
        });
    }
}
