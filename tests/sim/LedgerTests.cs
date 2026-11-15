using Hamlet.Sim.Economy;
using Xunit;

namespace Hamlet.Sim.Tests;

/// <summary>
/// The ledger principle (§3.2): money is conserved. TotalMoney must never
/// change after world creation, no matter the transfer activity.
/// </summary>
public class LedgerTests
{
    [Fact]
    public void OpeningBalancesMintTheWorldMoneySupply()
    {
        var book = new LedgerBook();
        book.OpenLedger(1000);
        book.OpenLedger(0);
        book.OpenLedger(500);
        Assert.Equal(1500, book.TotalMoney);
    }

    [Fact]
    public void TransfersConserveMoney()
    {
        var book = new LedgerBook();
        var a = book.OpenLedger(1000);
        var b = book.OpenLedger(0);
        var c = book.OpenLedger(500);

        for (var i = 0; i < 100; i++)
            book.Transfer(a, b, 3);
        book.Transfer(b, c, 200);

        Assert.Equal(1500, book.TotalMoney);
        Assert.Equal(700, a.Balance);
        Assert.Equal(100, b.Balance);
        Assert.Equal(700, c.Balance);
    }

    [Fact]
    public void OverdraftsAreRejected()
    {
        var book = new LedgerBook();
        var a = book.OpenLedger(50);
        var b = book.OpenLedger(0);
        Assert.Throws<InvalidOperationException>(() => book.Transfer(a, b, 10_000));
    }
}
