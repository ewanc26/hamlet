namespace Hamlet.Sim.Agent;

/// <summary>
/// The primitive activities utility scoring can choose (§6.2). None is the
/// unplanned state; every other kind compiles into a travel → perform plan.
/// </summary>
public enum ActivityKind : byte
{
    None,
    Sleep,
    Eat,
    Work,
}
