using Hamlet.Sim;
using Hamlet.Sim.Agent;
using Hamlet.Sim.City;
using Hamlet.Sim.Core;
using Hamlet.Sim.Economy;
using Xunit;

namespace Hamlet.Sim.Tests;

/// <summary>
/// The M2 wage loop (§7.3): completing a shift credits the household from the
/// employer's ledger, and the day's total money is unchanged.
/// </summary>
public class WageTests
{
    private static Simulation RoadWorld()
    {
        var simulation = new Simulation(seed: 909, gridSize: 48);
        for (var x = 5; x <= 40; x++)
            simulation.Enqueue(new PlaceRoad { X = x, Y = 7 });
        simulation.Enqueue(new FoundShop { X = 12, Y = 7 });
        simulation.Enqueue(new FoundFirm { X = 30, Y = 7, OpeningCash = 3_000 });
        simulation.Enqueue(new FoundHousehold
        {
            HomeX = 10,
            HomeY = 7,
            WorkX = 30,
            WorkY = 7,
            Members = 1,
        });

        simulation.TickOnce(); // drain the founding commands
        simulation.ClearEvents();
        return simulation;
    }

    [Fact]
    public void ACompletedShiftPaysTheHouseholdFromTheFirm()
    {
        var simulation = RoadWorld();
        var household = Assert.Single(simulation.Households.Households);
        var firm = Assert.Single(simulation.Firms.Firms);
        var citizen = Assert.Single(simulation.Bodies.Citizens);
        var openingMoney = simulation.LedgerBook.TotalMoney;

        for (var tick = 0; tick < Clock.TicksPerDay && citizen.LastWorkDay < 0; tick++)
        {
            simulation.TickOnce();
            simulation.ClearEvents();
        }

        Assert.True(citizen.LastWorkDay >= 0, "the shift should have completed within the day");
        Assert.Equal(openingMoney, simulation.LedgerBook.TotalMoney); // wage is a transfer
        Assert.Equal(3_000 - Firm.WagePerShift, firm.Ledger.Balance);
        Assert.True(household.Wallet!.Balance > 240, "the wage should have raised the wallet");
    }
}
