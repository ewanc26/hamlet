namespace Hamlet.Sim.City;

/// <summary>
/// Presentation-facing capture of the static terrain (§4, rule 3): plain
/// packed bytes, taken once when the world loads, and never a live grid
/// reference. The capture copies the layers, so the sim thread may keep
/// mutating its own grid while the host marshals this to GDScript.
/// </summary>
public sealed class TerrainSnapshot
{
    private readonly byte[][] _layers;

    public TerrainSnapshot(int size, byte[][] layers)
    {
        Size = size;
        _layers = layers;
    }

    /// <summary>Tiles per side; every layer is Size × Size, row-major.</summary>
    public int Size { get; }

    /// <summary>One 0-100 terrain layer, row-major (y × Size + x).</summary>
    public byte[] Layer(TerrainLayer layer) => _layers[(int)layer];
}
