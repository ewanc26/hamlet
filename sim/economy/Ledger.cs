namespace Hamlet.Sim.Economy;

/// <summary>
/// One account. The ledger principle (§3.2): money is never created or
/// destroyed outside world creation — it only moves between ledgers, so
/// transfers always sum to zero and the economy is auditable by construction.
/// </summary>
public sealed class Ledger
{
    public long OwnerId { get; init; }
    public long Balance { get; private set; }

    internal Ledger(long ownerId, long openingBalance)
    {
        OwnerId = ownerId;
        Balance = openingBalance;
    }

    internal void Credit(long amount) => Balance += amount;

    internal void Debit(long amount) => Balance -= amount;
}

/// <summary>
/// Owns every ledger in the world. Opening balances are the only minting
/// moment; after that, TotalMoney never changes — conservation is asserted
/// in tests and CI.
/// </summary>
public sealed class LedgerBook
{
    private readonly Dictionary<long, Ledger> _ledgers = new();
    private long _nextOwnerId = 1;

    public long TotalMoney => _ledgers.Values.Sum(ledger => ledger.Balance);

    public Ledger OpenLedger(long openingBalance = 0)
    {
        var ledger = new Ledger(_nextOwnerId++, openingBalance);
        _ledgers[ledger.OwnerId] = ledger;
        return ledger;
    }

    public void Transfer(Ledger from, Ledger to, long amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "transfers must be non-negative");
        if (from.Balance < amount)
            throw new InvalidOperationException($"ledger {from.OwnerId} cannot overdraw {amount} from {from.Balance}");
        from.Debit(amount);
        to.Credit(amount);
    }
}
