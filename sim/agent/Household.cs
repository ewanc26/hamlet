namespace Hamlet.Sim.Agent;

/// <summary>
/// The household is the unit that owns housing, the pantry, and later rent
/// (§6.4). M1 founds one household per settler group; shops and wages arrive
/// with the M2 economy, so the pantry is stocked at founding.
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
