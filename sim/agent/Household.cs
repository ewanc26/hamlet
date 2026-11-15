using Hamlet.Sim.Economy;

namespace Hamlet.Sim.Agent;

/// <summary>
/// The household is the unit that owns housing, the pantry, and the wallet
/// (§6.4). It is paid wages, buys meals from its corner shop, and feeds its
/// members from the pantry.
/// </summary>
public sealed class Household
{
    public const int DefaultPantryMeals = 21;

    public long Id { get; init; }
    public int HomeX { get; init; }
    public int HomeY { get; init; }
    public int WorkX { get; init; }
    public int WorkY { get; init; }
    public int MealsStocked { get; private set; }

    /// <summary>Money account, opened when the sim founds the household (§7.3).</summary>
    public Ledger? Wallet { get; set; }

    /// <summary>The shop this household walks to when the pantry runs low.</summary>
    public Shop? CornerShop { get; set; }

    /// <summary>The employer that pays this household's shifts.</summary>
    public Firm? Employer { get; set; }

    public Household(long id, int homeX, int homeY, int workX, int workY, int meals)
    {
        Id = id;
        HomeX = homeX;
        HomeY = homeY;
        WorkX = workX;
        WorkY = workY;
        MealsStocked = Math.Max(0, meals);
    }

    /// <summary>Consume one meal; false when the pantry is empty.</summary>
    public bool TryConsumeMeal()
    {
        if (MealsStocked <= 0)
            return false;
        MealsStocked--;
        return true;
    }

    /// <summary>
    /// Buy up to <paramref name="meals"/> from the corner shop with the
    /// household wallet; the shop reports how many were actually affordable.
    /// </summary>
    public bool TryBuyMeals(int meals)
    {
        if (Wallet is null || CornerShop is null)
            return false;
        if (!CornerShop.TrySell(Wallet, meals, out var sold))
            return false;
        MealsStocked += sold;
        return true;
    }

    /// <summary>Collect one shift's wage from the employer; returns what was paid.</summary>
    public long CollectWage() =>
        Wallet is null || Employer is null ? 0 : Employer.PayShift(Wallet);

    /// <summary>The presentation view of this household, captured at a tick boundary (§4, rule 3).</summary>
    public HouseholdSnapshot Snapshot() => new(Id, HomeX, HomeY, WorkX, WorkY, MealsStocked);
}

/// <summary>Owns every household; founding order fixes identity (§7.3).</summary>
public sealed class HouseholdBook
{
    private readonly List<Household> _households = new();
    private readonly Dictionary<long, Household> _byId = new();
    private long _nextId = 1;

    public IReadOnlyList<Household> Households => _households;

    public Household Found(int homeX, int homeY, int workX, int workY, int meals = Household.DefaultPantryMeals)
    {
        var household = new Household(_nextId++, homeX, homeY, workX, workY, meals);
        _households.Add(household);
        _byId[household.Id] = household;
        return household;
    }

    public Household? Find(long householdId) =>
        _byId.TryGetValue(householdId, out var household) ? household : null;
}
