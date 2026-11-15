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
    private const int RestockHour = 6;     // shops order wholesale stock before opening

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

    /// <summary>The camera focus square (§6.5); null means everything runs full LOD.</summary>
    public LodFocus? Focus { get; private set; }

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

        if (Clock.Hour == RestockHour && Clock.Minute == 0)
            RestockShops();

        SplitBrokenBodies();

        foreach (var citizen in Bodies.Citizens)
            TickCitizen(citizen);

        MergeHouseholdTrips();
    }

    /// <summary>Shops order from their supplier once a day, before the doors open (§7.3).</summary>
    private void RestockShops()
    {
        foreach (var shop in Shops.Shops)
            if (shop.MealsInStock < Shop.LowStockAt)
                shop.TryRestock(Shop.RestockBatch, out _);
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

        var body = Bodies.BodyOf(citizen.Id) is { } bodyId ? Bodies.GetBody(bodyId) : null;
        if (body is not null && citizen.Id != body.RepresentativeId)
        {
            // Riding a household body (§6.6): needs still decay, the position
            // follows the representative, and there is no plan of its own.
            var representative = Bodies.FindCitizen(body.RepresentativeId)!;
            citizen.X = representative.X;
            citizen.Y = representative.Y;
            return;
        }

        var previous = citizen.Plan.Intent;
        if ((Clock.Tick + citizen.Id) % ReplanInterval == 0 || InCrisis(citizen))
            Replan(citizen, household, allowHysteresis: true);

        if (ActivityRunner.Tick(citizen, household, Clock, Schedule, Rules, Grid.Size,
                coarse: !IsFullLod(citizen), bodyMembers: body?.Agents))
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

    /// <summary>
    /// Split bodies whose shared trip is over or whose members diverged or
    /// entered crisis (§6.6 de-merge triggers). Lossless: members keep their
    /// own records and replan individually from here.
    /// </summary>
    private void SplitBrokenBodies()
    {
        var bodies = Bodies.Bodies;
        for (var i = bodies.Count - 1; i >= 0; i--)
        {
            var body = bodies[i];
            var representative = Bodies.FindCitizen(body.RepresentativeId);
            if (representative is null || representative.Plan.IsNone)
            {
                Bodies.DeMerge(body.BodyId);
                continue;
            }
            var split = false;
            for (var m = 0; m < body.Agents.Count; m++)
            {
                var member = body.Agents[m];
                if (!member.IsAlive || InCrisis(member) || member.Plan.Intent != representative.Plan.Intent)
                {
                    split = true;
                    break;
                }
            }
            if (split)
                Bodies.DeMerge(body.BodyId);
        }
    }

    private readonly List<Citizen> _mergeScratch = new();

    /// <summary>
    /// First merge tier (§6.6): at the tick boundary, a household's co-located
    /// members with the same intent and target merge into one body. Family
    /// sized only — larger households stay individual until the city stage.
    /// </summary>
    private void MergeHouseholdTrips()
    {
        foreach (var household in Households.Households)
        {
            _mergeScratch.Clear();
            foreach (var citizen in Bodies.Citizens)
                if (citizen.IsAlive && citizen.HouseholdId == household.Id && Bodies.BodyOf(citizen.Id) is null)
                    _mergeScratch.Add(citizen);
            if (_mergeScratch.Count < 2 || _mergeScratch.Count > AgentBodies.MaxBodyMembers)
                continue;

            var first = _mergeScratch[0];
            if (first.Plan.IsNone || InCrisis(first))
                continue;
            var compatible = true;
            for (var i = 1; i < _mergeScratch.Count; i++)
            {
                var other = _mergeScratch[i];
                if (other.Plan.Intent != first.Plan.Intent
                    || other.Plan.TargetX != first.Plan.TargetX
                    || other.Plan.TargetY != first.Plan.TargetY
                    || other.X != first.X || other.Y != first.Y
                    || InCrisis(other))
                {
                    compatible = false;
                    break;
                }
            }
            if (!compatible)
                continue;

            var ids = new List<long>(_mergeScratch.Count); // allocates only when a merge actually forms
            foreach (var member in _mergeScratch)
                ids.Add(member.Id);
            Bodies.Merge(ids);
        }
    }

    private static bool InCrisis(Citizen citizen) =>
        (citizen.Needs.InCrisis(NeedKind.Hunger) && citizen.Plan.Intent != ActivityKind.Eat)
        || (citizen.Needs.InCrisis(NeedKind.Energy) && citizen.Plan.Intent != ActivityKind.Sleep);

    /// <summary>Full LOD on/near the camera focus, standard LOD elsewhere (§6.5).</summary>
    public bool IsFullLod(Citizen citizen) => Focus is null || Focus.Contains(citizen.X, citizen.Y);

    private void Replan(Citizen citizen, Household household, bool allowHysteresis)
    {
        var best = UtilityScorer.Best(citizen, household, Clock, Schedule);
        var current = citizen.Plan;

        if (allowHysteresis && !current.IsNone && current.Intent != best)
        {
            // Sleep at night is only interrupted by a need crisis or by hunger
            // falling due (below the meal threshold): a moderate want at 3 a.m.
            // is not worth waking for (§6.2 plan robustness).
            if (current.Intent == ActivityKind.Sleep && Schedule.IsNight(Clock.Hour)
                && !InCrisis(citizen) && citizen.Needs[NeedKind.Hunger] >= UtilityScorer.MealThreshold)
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
        hash.Mix(Focus?.CenterX ?? -1);
        hash.Mix(Focus?.CenterY ?? -1);
        hash.Mix(Focus?.Radius ?? -1);

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
            hash.Mix(shop.Supplier?.Id ?? 0);
        }

        foreach (var firm in Firms.Firms)
        {
            hash.Mix(firm.Id);
            hash.Mix(firm.X);
            hash.Mix(firm.Y);
            hash.Mix(firm.Ledger.Balance);
        }

        hash.Mix(Bodies.Bodies.Count);
        foreach (var body in Bodies.Bodies)
        {
            hash.Mix(body.BodyId);
            hash.Mix(body.Mass);
            foreach (var memberId in body.Members)
                hash.Mix(memberId);
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
            case SetCameraFocus setFocus:
                Focus = new LodFocus(setFocus.X, setFocus.Y, setFocus.Radius);
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
        shop.Supplier = Firms.Nearest(shop.X, shop.Y);
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
        foreach (var shop in Shops.Shops)
            shop.Supplier ??= Firms.Nearest(shop.X, shop.Y);

        _outbox.Add(new SimEvent
        {
            Kind = SimEventKind.FirmFounded,
            Tick = Clock.Tick,
            X = firm.X,
            Y = firm.Y,
        });
    }
}
