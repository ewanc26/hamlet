namespace Hamlet.Sim.Core;

/// <summary>
/// Marker interface for every player/AI instruction the sim accepts (§4).
/// Commands are plain data — the entire game is a deterministic command stream
/// layered over a seeded world, which is what makes input-replay saves work.
/// </summary>
public interface ISimCommand
{
}

/// <summary>Found a settler household with a home, a job site, and a pantry (§6.4).</summary>
public sealed class FoundHousehold : ISimCommand
{
    public int HomeX { get; init; }
    public int HomeY { get; init; }
    public int WorkX { get; init; }
    public int WorkY { get; init; }
    public int Members { get; init; } = 1;
    public int Meals { get; init; } = Agent.Household.DefaultPantryMeals;
}

/// <summary>Found a shop counter with meal stock and its own ledger (§7.3).</summary>
public sealed class FoundShop : ISimCommand
{
    public int X { get; init; }
    public int Y { get; init; }
    public int Meals { get; init; } = Economy.Shop.DefaultStock;
    public long OpeningCash { get; init; }
}

/// <summary>Found an employer with a workplace and a payroll ledger (§7.3).</summary>
public sealed class FoundFirm : ISimCommand
{
    public int X { get; init; }
    public int Y { get; init; }
    public long OpeningCash { get; init; }
}
