namespace Hamlet.Sim.Agent;

/// <summary>
/// Compiles a winning intent into the primitive plan the runner executes
/// (§6.2). M1 plans are the fixed travel → perform pair; the compiler checks
/// the one precondition that matters now — whether the citizen is already
/// standing on the target.
/// </summary>
public static class IntentPlanner
{
    public static Plan Compile(ActivityKind intent, Citizen citizen, Household household)
    {
        if (intent == ActivityKind.None)
            return default;

        var (targetX, targetY) = TargetOf(intent, household);
        var phase = citizen.X == targetX && citizen.Y == targetY
            ? PlanPhase.Perform
            : PlanPhase.Travel;
        return new Plan
        {
            Intent = intent,
            Phase = phase,
            TargetX = targetX,
            TargetY = targetY,
        };
    }

    public static (int X, int Y) TargetOf(ActivityKind activity, Household household) =>
        activity == ActivityKind.Work
            ? (household.WorkX, household.WorkY)
            : (household.HomeX, household.HomeY);
}
