using Hamlet.Sim;
using Hamlet.Sim.Agent;
using Hamlet.Sim.City;
using Hamlet.Sim.Core;
using Hamlet.Sim.Economy;
using Xunit;

namespace Hamlet.Sim.Tests;

/// <summary>
/// Household bodies — the first merge tier (§6.6): compatible family trips
/// merge into one body with mass, and a merge → split → merge round trip is
/// lossless. The M2 exit criterion is the round-trip property test.
/// </summary>
public class HouseholdBodyTests
{
    [Fact]
    public void MergeSplitMergeRoundTripsLosslessly()
    {
        for (var seed = 1; seed <= 8; seed++)
        {
            var random = new DeterministicRandom((ulong)seed);
            var bodies = new AgentBodies();
            var count = random.Next(2, 9);
            var ids = new List<long>();
            for (var i = 0; i < count; i++)
            {
                var citizen = bodies.SpawnCitizen();
                // scatter the need vector so the histogram has something to say
                for (var kind = 0; kind < Needs.Count; kind++)
                    if (random.Next(0, 2) == 0)
                        citizen.Needs.Decay((NeedKind)kind, random.Next(0, 60));
                ids.Add(citizen.Id);
            }

            var shuffled = Shuffle(ids, random);
            var first = bodies.Merge(shuffled);

            // Capture the full body state: member order plus the need histogram.
            var members = first.Members.ToArray();
            var histogram = new int[Needs.Count * 101];
            for (var kind = 0; kind < Needs.Count; kind++)
                for (var v = 0; v <= 100; v++)
                    histogram[kind * 101 + v] = first.HistogramAt((NeedKind)kind, v);

            var split = bodies.DeMerge(first.BodyId);
            Assert.Equal(members, split); // split is lossless

            var again = bodies.Merge(Shuffle(ids, random)); // different input order
            Assert.Equal(members, again.Members); // merge order never matters
            Assert.Equal(first.Mass, again.Mass);
            for (var kind = 0; kind < Needs.Count; kind++)
                for (var v = 0; v <= 100; v++)
                    Assert.Equal(histogram[kind * 101 + v], again.HistogramAt((NeedKind)kind, v));
        }
    }

    private static List<long> Shuffle(List<long> ids, DeterministicRandom random)
    {
        var list = new List<long>(ids);
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = random.Next(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    private static Simulation FamilyTown(int members)
    {
        var simulation = new Simulation(4242, 64);
        for (var x = 8; x <= 24; x++)
            simulation.Enqueue(new PlaceRoad { X = x, Y = 10 });
        simulation.Enqueue(new FoundShop { X = 12, Y = 10, OpeningCash = 600 });
        simulation.Enqueue(new FoundFirm { X = 24, Y = 10, OpeningCash = 3_000 });
        simulation.Enqueue(new FoundHousehold
        {
            HomeX = 8,
            HomeY = 10,
            WorkX = 24,
            WorkY = 10,
            Members = members,
            Meals = 30,
        });
        simulation.TickOnce();
        simulation.ClearEvents();
        return simulation;
    }

    [Fact]
    public void CompatibleHouseholdMembersMergeIntoOneBody()
    {
        var simulation = FamilyTown(members: 3);
        // Spawned co-located with the same plan: the family merges on the first
        // tick boundary.
        var body = Assert.Single(simulation.Bodies.Bodies);
        Assert.Equal(3, body.Mass);
        Assert.Equal(3, simulation.Bodies.Citizens.Count(c => simulation.Bodies.BodyOf(c.Id) == body.BodyId));
    }

    [Fact]
    public void OversizedHouseholdsNeverMerge()
    {
        // The first merge tier is family trips; bigger households stay
        // individual until the city-stage cohort bodies (§6.6).
        var simulation = FamilyTown(members: AgentBodies.MaxBodyMembers + 1);
        Assert.Empty(simulation.Bodies.Bodies);
    }

    [Fact]
    public void RidersFollowTheRepresentative()
    {
        var simulation = FamilyTown(members: 2);
        // Run past the morning replans until the family is travelling to work
        // as one body.
        var ticked = 0;
        while (simulation.Clock.Hour < 12 && ticked < 1200)
        {
            simulation.TickOnce();
            simulation.ClearEvents();
            ticked++;
            var bodies = simulation.Bodies.Bodies;
            if (bodies.Count == 1)
            {
                var body = bodies[0];
                var rep = simulation.Bodies.FindCitizen(body.RepresentativeId)!;
                if (rep.Plan is { Intent: ActivityKind.Work, Phase: PlanPhase.Travel })
                {
                    foreach (var member in body.Agents)
                        Assert.Equal((rep.X, rep.Y), (member.X, member.Y));
                    return;
                }
            }
        }
        Assert.Fail("the family never merged into a travelling body");
    }

    [Fact]
    public void AShopErrandBuysForTheWholeMass()
    {
        var ledgers = new LedgerBook();
        var household = new Household(1, 8, 10, 24, 10, meals: 0);
        household.Wallet = ledgers.OpenLedger(240);
        var shop = new ShopBook(ledgers).Found(12, 10, stock: 60, openingCash: 600);
        household.CornerShop = shop;

        var members = new List<Citizen>
        {
            new() { Id = 1, HouseholdId = 1, X = 12, Y = 10 },
            new() { Id = 2, HouseholdId = 1, X = 12, Y = 10 },
        };
        var actor = members[0];
        actor.Plan = new Plan
        {
            Intent = ActivityKind.Shop,
            Phase = PlanPhase.Perform,
            TargetX = 12,
            TargetY = 10,
        };

        var clock = new Clock();
        var rules = NeedRules.Default;
        // Perform takes ShopTicks ticks; run them.
        for (var i = 0; i <= rules.ShopTicks; i++)
            ActivityRunner.Tick(actor, household, clock, DaySchedule.Default, rules, 64, bodyMembers: members);

        Assert.Equal(rules.ShopBundleMeals * 2, household.MealsStocked);
        Assert.Equal(60 - rules.ShopBundleMeals * 2, shop.MealsInStock);
        // Money moved, not minted: wallet down, shop up by the same amount.
        Assert.Equal(240 - (long)rules.ShopBundleMeals * 2 * Shop.PricePerMeal, household.Wallet.Balance);
        Assert.Equal(600 + (long)rules.ShopBundleMeals * 2 * Shop.PricePerMeal, shop.Ledger.Balance);
        Assert.Equal(240 + 600, ledgers.TotalMoney);
    }

    [Fact]
    public void AShiftPaysEveryMemberOfTheBody()
    {
        var ledgers = new LedgerBook();
        var household = new Household(1, 8, 10, 24, 10, meals: 10);
        household.Wallet = ledgers.OpenLedger(240);
        var firm = new FirmBook(ledgers).Found(24, 10, openingCash: 3_000);
        household.Employer = firm;

        var members = new List<Citizen>
        {
            new() { Id = 1, HouseholdId = 1, X = 24, Y = 10 },
            new() { Id = 2, HouseholdId = 1, X = 24, Y = 10 },
        };
        var actor = members[0];
        actor.Plan = new Plan
        {
            Intent = ActivityKind.Work,
            Phase = PlanPhase.Perform,
            TargetX = 24,
            TargetY = 10,
        };

        var clock = new Clock();
        for (var minute = 0; minute < 17 * Clock.MinutesPerHour; minute++)
            clock.Advance(); // 17:00 — shift end

        ActivityRunner.Tick(actor, household, clock, DaySchedule.Default, NeedRules.Default, 64, bodyMembers: members);

        Assert.Equal(clock.Day, members[0].LastWorkDay);
        Assert.Equal(clock.Day, members[1].LastWorkDay);
        Assert.Equal(240 + 2 * Firm.WagePerShift, household.Wallet.Balance);
        Assert.Equal(3_000 - 2 * Firm.WagePerShift, firm.Ledger.Balance);
    }

    [Fact]
    public void AMemberCrisisSplitsTheBodyLosslessly()
    {
        var simulation = FamilyTown(members: 2);
        var body = Assert.Single(simulation.Bodies.Bodies);

        // One member starves while the body is asleep: the body must split.
        var starving = body.Agents[1];
        starving.Needs.Decay(NeedKind.Hunger, starving.Needs[NeedKind.Hunger]); // hunger 0
        simulation.TickOnce();
        simulation.ClearEvents();

        Assert.Empty(simulation.Bodies.Bodies);
        Assert.Null(simulation.Bodies.BodyOf(body.Agents[0].Id));
        Assert.Null(simulation.Bodies.BodyOf(body.Agents[1].Id));
        Assert.Equal(2, simulation.Bodies.Citizens.Count);
    }

    [Fact]
    public void BodiesArePartOfTheDeterministicHash()
    {
        Simulation Run()
        {
            var simulation = FamilyTown(members: 3);
            for (var i = 0; i < 3000; i++)
            {
                simulation.TickOnce();
                simulation.ClearEvents();
            }
            return simulation;
        }

        var a = Run();
        var b = Run();
        Assert.Equal(a.ComputeStateHash().Value, b.ComputeStateHash().Value);
        Assert.Equal(a.Bodies.Bodies.Count, b.Bodies.Bodies.Count);
    }
}
