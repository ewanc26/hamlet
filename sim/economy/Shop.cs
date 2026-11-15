namespace Hamlet.Sim.Economy;

/// <summary>
/// A shop as a real entity (§7.3): a counter that holds meal stock and its own
/// ledger. Selling moves money household → shop (never minted) and hands over
/// as many meals as the wallet and stock allow, so a poor household still eats
/// a little rather than none at all.
/// </summary>
public sealed class Shop
{
    public const int PricePerMeal = 5;
    public const int DefaultStock = 60;

    private readonly LedgerBook _ledgers;

    internal Shop(long id, int x, int y, int stock, Ledger ledger, LedgerBook ledgers)
    {
        Id = id;
        X = x;
        Y = y;
        MealsInStock = Math.Max(0, stock);
        Ledger = ledger;
        _ledgers = ledgers;
    }

    public long Id { get; }
    public int X { get; }
    public int Y { get; }
    public Ledger Ledger { get; }
    public int MealsInStock { get; private set; }

    public int CostOf(int meals) => Math.Max(0, meals) * PricePerMeal;

    /// <summary>
    /// Sell up to <paramref name="wanted"/> meals to <paramref name="wallet"/>.
    /// Returns false when nothing can be sold; otherwise reports how many meals
    /// actually changed hands.
    /// </summary>
    public bool TrySell(Ledger wallet, int wanted, out int sold)
    {
        sold = Math.Min(Math.Max(0, wanted), MealsInStock);
        if (wallet.Balance < PricePerMeal)
            sold = 0;
        else
            sold = Math.Min(sold, (int)(wallet.Balance / PricePerMeal));
        if (sold <= 0)
            return false;

        _ledgers.Transfer(wallet, Ledger, sold * PricePerMeal);
        MealsInStock -= sold;
        return true;
    }
}

/// <summary>
/// Owns every shop in the world; founding order fixes identity (§7.3).
/// </summary>
public sealed class ShopBook
{
    private readonly List<Shop> _shops = new();
    private readonly Dictionary<long, Shop> _byId = new();
    private readonly LedgerBook _ledgers;
    private long _nextId = 1;

    public ShopBook(LedgerBook ledgers) => _ledgers = ledgers;

    public IReadOnlyList<Shop> Shops => _shops;

    public Shop Found(int x, int y, int stock = Shop.DefaultStock, long openingCash = 0)
    {
        var shop = new Shop(_nextId++, x, y, stock, _ledgers.OpenLedger(openingCash), _ledgers);
        _shops.Add(shop);
        _byId[shop.Id] = shop;
        return shop;
    }

    /// <summary>Closest shop by Manhattan distance; ties by id, so the choice is fixed.</summary>
    public Shop? Nearest(int x, int y)
    {
        Shop? best = null;
        var bestDistance = int.MaxValue;
        foreach (var shop in _shops)
        {
            var distance = Math.Abs(shop.X - x) + Math.Abs(shop.Y - y);
            if (distance >= bestDistance)
                continue;
            bestDistance = distance;
            best = shop;
        }
        return best;
    }

    public Shop? Find(long shopId) => _byId.TryGetValue(shopId, out var shop) ? shop : null;
}
