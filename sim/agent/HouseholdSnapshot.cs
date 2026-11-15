namespace Hamlet.Sim.Agent;

/// <summary>
/// Presentation-facing view of one household (§4, rule 3): the home and job
/// site the greybox draws as buildings, plus the pantry the HUD shows. Plain
/// values captured at a tick boundary, never a live sim reference.
/// </summary>
public readonly record struct HouseholdSnapshot(
    long Id,
    int HomeX,
    int HomeY,
    int WorkX,
    int WorkY,
    int MealsStocked);
