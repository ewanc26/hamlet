namespace Hamlet.Sim.Core;

/// <summary>
/// Marker interface for every player/AI instruction the sim accepts (§4).
/// Commands are plain data — the entire game is a deterministic command stream
/// layered over a seeded world, which is what makes input-replay saves work.
/// </summary>
public interface ISimCommand
{
}
