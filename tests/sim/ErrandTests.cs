using Hamlet.Sim;
using Hamlet.Sim.Agent;
using Hamlet.Sim.City;
using Hamlet.Sim.Core;
using Xunit;

namespace Hamlet.Sim.Tests;

/// <summary>
/// The M2 shopping loop (§6.2, §7.3): a household whose pantry runs low plans a
/// shop errand, walks the road to the counter, and restocks with money that
/// moves between real ledgers.
/// </summary>
public class ErrandTests
{
    private static Simulation RoadWorld(int startingMeals)
    {
        var simulation = new Simulation(seed: 4242, gridSize: 48);
        for (var x = 5; x <= 40; x++)
            simulation.Enqueue(new PlaceRoad { X = x, Y = 7 });
        simulation.Enqueue(new FoundShop { X = 12, Y = 7 });
        simulation.Enqueue(new FoundHousehold
        {
            HomeX = 10,
            HomeY = 7,
            WorkX = 30,
            WorkY = 7,
            Members = 1,
            Meals = startingMeals,
        });

        // Founding is a command: drain it before the day begins.
        simulation.TickOnce();
        simulation.ClearEvents();
        return simulation;
    }

    [Fact]
    public void ALowPantrySendsTheHouseholdToTheShop()
    {
        var simulation = RoadWorld(startingMeals: 2);
        var household = Assert.Single(simulation.Households.Households);
        var openingMoney = simulation.LedgerBook.TotalMoney;
        var sawShopActivity = false;

        for (var tick = 0; tick < Clock.TicksPerDay; tick++)
        {
            simulation.TickOnce();
            foreach (var simEvent in simulation.PendingEvents)
                sawShopActivity |= simEvent.Kind == SimEventKind.ActivityChanged
                    && (ActivityKind)simEvent.Activity == ActivityKind.Shop;
            simulation.ClearEvents();
        }

        Assert.True(sawShopActivity, "a household low on meals should plan a restock errand");
        Assert.True(household.MealsStocked > 2, "the errand should have restocked the pantry");
        Assert.Equal(openingMoney, simulation.LedgerBook.TotalMoney); // transfers only
        Assert.True(household.Wallet!.Balance < 240, "the groceries should have cost the household");
    }

    [Fact]
    public void AStockedPantryMeansNoErrand()
    {
        var simulation = RoadWorld(startingMeals: Household.DefaultPantryMeals);
        var sawShopActivity = false;

        for (var tick = 0; tick < Clock.TicksPerDay; tick++)
        {
            simulation.TickOnce();
            foreach (var simEvent in simulation.PendingEvents)
                sawShopActivity |= simEvent.Kind == SimEventKind.ActivityChanged
                    && (ActivityKind)simEvent.Activity == ActivityKind.Shop;
            simulation.ClearEvents();
        }

        Assert.False(sawShopActivity);
    }
}
