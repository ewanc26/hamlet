namespace Hamlet.Sim.Agent;

/// <summary>
/// The camera focus square that splits citizens into LOD tiers (§6.5): inside
/// it they run full (per-tick stepping), outside they run standard (staggered
/// replanning already applies to all; movement goes coarse). The focus arrives
/// as a <c>SetCameraFocus</c> command, so it is part of the deterministic
/// command stream, never a frame-clock read.
/// </summary>
public sealed class LodFocus
{
    /// <summary>Chebyshev (square) radius in tiles around the centre.</summary>
    public int Radius { get; }

    public int CenterX { get; }
    public int CenterY { get; }

    public LodFocus(int centerX, int centerY, int radius)
    {
        CenterX = centerX;
        CenterY = centerY;
        Radius = Math.Max(0, radius);
    }

    public bool Contains(int x, int y) =>
        Math.Abs(x - CenterX) <= Radius && Math.Abs(y - CenterY) <= Radius;
}
