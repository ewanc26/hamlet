namespace Hamlet.Sim.Economy;

/// <summary>
/// A firm is a real employer (§7.3): a workplace with its own payroll ledger.
/// It pays its workers out of that account, so wages are transfers from real
/// money — a firm that cannot afford a shift pays nothing rather than minting.
/// </summary>
public sealed class Firm
{
    public const int WagePerShift = 30;

    private readonly LedgerBook _ledgers;

    internal Firm(long id, int x, int y, Ledger ledger, LedgerBook ledgers)
    {
        Id = id;
        X = x;
        Y = y;
        Ledger = ledger;
        _ledgers = ledgers;
    }

    public long Id { get; }
    public int X { get; }
    public int Y { get; }
    public Ledger Ledger { get; }

    /// <summary>Pay one shift into <paramref name="wallet"/>; returns what was actually paid.</summary>
    public long PayShift(Ledger wallet)
    {
        var paid = Math.Min(WagePerShift, Ledger.Balance);
        if (paid <= 0)
            return 0;
        _ledgers.Transfer(Ledger, wallet, paid);
        return paid;
    }
}

/// <summary>Owns every firm; founding order fixes identity (§7.3).</summary>
public sealed class FirmBook
{
    private readonly List<Firm> _firms = new();
    private readonly Dictionary<long, Firm> _byId = new();
    private readonly LedgerBook _ledgers;
    private long _nextId = 1;

    public FirmBook(LedgerBook ledgers) => _ledgers = ledgers;

    public IReadOnlyList<Firm> Firms => _firms;

    public Firm Found(int x, int y, long openingCash = 0)
    {
        var firm = new Firm(_nextId++, x, y, _ledgers.OpenLedger(openingCash), _ledgers);
        _firms.Add(firm);
        _byId[firm.Id] = firm;
        return firm;
    }

    /// <summary>Closest firm by Manhattan distance; ties by id, so the employer is fixed.</summary>
    public Firm? Nearest(int x, int y)
    {
        Firm? best = null;
        var bestDistance = int.MaxValue;
        foreach (var firm in _firms)
        {
            var distance = Math.Abs(firm.X - x) + Math.Abs(firm.Y - y);
            if (distance >= bestDistance)
                continue;
            bestDistance = distance;
            best = firm;
        }
        return best;
    }

    public Firm? Find(long firmId) => _byId.TryGetValue(firmId, out var firm) ? firm : null;
}
