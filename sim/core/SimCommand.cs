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
}
