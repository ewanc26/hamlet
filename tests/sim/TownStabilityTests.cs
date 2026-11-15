using Hamlet.Sim;
using Hamlet.Sim.Agent;
using Hamlet.Sim.City;
using Hamlet.Sim.Core;
using Hamlet.Sim.Economy;
using Xunit;

namespace Hamlet.Sim.Tests;

/// <summary>
/// The M2 exit criterion (§10): a hundred households, ten shops, and ten firms
/// on one street run for ten in-game days with no intervention — the town stays
/// fed and employed, no money is minted or destroyed, and the whole decade
/// replays bit-identically from the seed.
/// </summary>
public class TownStabilityTests
{
    private const int HouseholdCount = 100;
    private const int ShopCount = 10;
    private const int FirmCount = 10;
    private const int Days = 10;
    private const int StreetY = 40;
    private const long HouseholdOpeningCash = 240;
    private const long ShopOpeningCash = 600;
    private const long FirmOpeningCash = 3_000;

    private sealed class Town
    {
        public required Simulation Sim { get; init; }
        public required long OpeningMoney { get; init; }
        public required long OpeningShopMoney { get; init; }
        public required int LowestHunger { get; init; }
        public required string Hash { get; init; }
    }

    private static Town Run(ulong seed)
    {
        var simulation = new Simulation(seed, 128);
        for (var x = 6; x <= 121; x++)
            simulation.Enqueue(new PlaceRoad { X = x, Y = StreetY });
        for (var i = 0; i < FirmCount; i++)
            simulation.Enqueue(new FoundFirm { X = 10 + i * 10, Y = StreetY, OpeningCash = FirmOpeningCash });
        for (var i = 0; i < ShopCount; i++)
            simulation.Enqueue(new FoundShop { X = 15 + i * 10, Y = StreetY, OpeningCash = ShopOpeningCash });
        for (var i = 0; i < HouseholdCount; i++)
        {
            simulation.Enqueue(new FoundHousehold
            {
                HomeX = 7 + i,
                HomeY = StreetY,
                WorkX = 10 + i / 10 * 10,
                WorkY = StreetY,
                Members = 1,
                Meals = 5, // a thin pantry, so the town has to shop from day one
            });
        }

        // Founding is a command: the town exists once the first tick drains it.
        simulation.TickOnce();
        simulation.ClearEvents();

        var openingMoney = simulation.LedgerBook.TotalMoney;
        var openingShopMoney = SumShopMoney(simulation);
        var lowestHunger = 100;

        for (var tick = 0; tick < Days * Clock.TicksPerDay; tick++)
        {
            simulation.TickOnce();
            simulation.ClearEvents();

            foreach (var citizen in simulation.Bodies.Citizens)
                if (citizen.IsAlive)
                    lowestHunger = Math.Min(lowestHunger, citizen.Needs[NeedKind.Hunger]);
        }

        return new Town
        {
            Sim = simulation,
            OpeningMoney = openingMoney,
            OpeningShopMoney = openingShopMoney,
            LowestHunger = lowestHunger,
            Hash = simulation.ComputeStateHash().Value.ToString("X"),
        };
    }

    private static long SumShopMoney(Simulation simulation)
    {
        var total = 0L;
        foreach (var shop in simulation.Shops.Shops)
            total += shop.Ledger.Balance;
        return total;
    }

    private static readonly Lazy<Town> SeedTown = new(() => Run(12345));

    [Fact]
    public void ATownOfAHundredHouseholdsStaysFedAndEmployedForTenDays()
    {
        var town = SeedTown.Value;
        var simulation = town.Sim;

        Assert.Equal(HouseholdCount, simulation.Households.Households.Count);
        Assert.All(simulation.Bodies.Citizens, citizen => Assert.True(citizen.IsAlive));
        Assert.All(simulation.Households.Households, household => Assert.NotNull(household.CornerShop));
        Assert.All(simulation.Shops.Shops, shop => Assert.NotNull(shop.Supplier));

        // Nobody starved: hunger never entered the crisis band, even though
        // every pantry starts thin and the shops must restock to keep up.
        Assert.True(town.LowestHunger >= 15, $"hunger fell to {town.LowestHunger}");

        // The town ends the decade fed: every pantry holds meals again.
        Assert.All(simulation.Households.Households, household =>
            Assert.True(household.MealsStocked > 0, $"household {household.Id} ended with a dry pantry"));

        // The town stays employed: most citizens closed a shift within the
        // last two days (a hungry worker quitting early for a meal is fine —
        // the wage is only paid for a shift worked to its end). All measured
        // needs stayed on the shared 0-100 scale throughout.
        var employed = 0;
        foreach (var citizen in simulation.Bodies.Citizens)
        {
            if (citizen.LastWorkDay >= Days - 2)
                employed++;
            for (var kind = 0; kind < Needs.Count; kind++)
                Assert.InRange(citizen.Needs[(NeedKind)kind], 0, 100);
        }
        Assert.True(employed >= 90, $"only {employed}/{HouseholdCount} citizens worked in the last two days");

        // The economy is a closed loop: founding mints once, then every wage,
        // meal, and wholesale order is a transfer.
        Assert.Equal(town.OpeningMoney, simulation.LedgerBook.TotalMoney);
        Assert.True(SumShopMoney(simulation) > town.OpeningShopMoney, "the shops took no retail money");

        var walletTotal = 0L;
        foreach (var household in simulation.Households.Households)
            walletTotal += household.Wallet?.Balance ?? 0;
        Assert.True(
            walletTotal > HouseholdCount * HouseholdOpeningCash,
            $"wages should out-earn grocery bills, but wallets hold {walletTotal}");
    }

    [Fact]
    public void TheSameSeedReplaysTheSameTownAndAnotherSeedDoesNot()
    {
        Assert.Equal(SeedTown.Value.Hash, Run(12345).Hash);
        Assert.NotEqual(SeedTown.Value.Hash, Run(12346).Hash);
    }
}
