using Hamlet.Sim.Agent;
using Hamlet.Sim.Economy;
using Xunit;

namespace Hamlet.Sim.Tests;

/// <summary>
/// Firm contract (§7.3): a shift is paid from the firm's own ledger, never
/// minted. A firm short of money pays what it has; a household with no
/// employer earns nothing.
/// </summary>
public class FirmTests
{
    private static (FirmBook Firms, Firm Firm, Household Household) World(long firmCash, long walletCash = 0)
    {
        var ledgers = new LedgerBook();
        var firms = new FirmBook(ledgers);
        var firm = firms.Found(6, 6, firmCash);
        var household = new Household(id: 1, homeX: 2, homeY: 2, workX: 6, workY: 6, meals: 5)
        {
            Wallet = ledgers.OpenLedger(walletCash),
            Employer = firm,
        };
        return (firms, firm, household);
    }

    [Fact]
    public void AShiftMovesTheWageFromTheFirmToTheHousehold()
    {
        var (_, firm, household) = World(firmCash: 100);

        Assert.Equal(Firm.WagePerShift, household.CollectWage());

        Assert.Equal(Firm.WagePerShift, household.Wallet!.Balance);
        Assert.Equal(100 - Firm.WagePerShift, firm.Ledger.Balance);
    }

    [Fact]
    public void ABrokeFirmPaysOnlyWhatItHas()
    {
        var (_, firm, household) = World(firmCash: 10);

        Assert.Equal(10, household.CollectWage());
        Assert.Equal(10, household.Wallet!.Balance);
        Assert.Equal(0, firm.Ledger.Balance);
        Assert.Equal(0, household.CollectWage()); // nothing left to pay
    }

    [Fact]
    public void AHouseholdWithoutAnEmployerEarnsNothing()
    {
        var (_, _, household) = World(firmCash: 100);
        household.Employer = null;

        Assert.Equal(0, household.CollectWage());
        Assert.Equal(0, household.Wallet!.Balance);
    }

    [Fact]
    public void TheNearestFirmBecomesTheEmployerOnATie()
    {
        var firms = new FirmBook(new LedgerBook());
        var first = firms.Found(5, 5);
        firms.Found(1, 1);

        Assert.Same(first, firms.Nearest(3, 3)); // both four tiles away: id order decides
    }
}
