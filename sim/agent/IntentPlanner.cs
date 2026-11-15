using Hamlet.Sim.City;

namespace Hamlet.Sim.Agent;

/// <summary>
/// Compiles a winning intent into the primitive plan the runner executes
/// (§6.2). M1 plans are the fixed travel → perform pair; M2 adds the road
/// route: when a pathfinder is supplied, travel plans carry the cheapest
/// congestion-aware route, and a world with no connecting road falls back to
/// the direct walk.
/// </summary>
public static class IntentPlanner
{
    public static Plan Compile(ActivityKind intent, Citizen citizen, Household household) =>
        Compile(intent, citizen, household, pathfinder: null, congestion: null);

    public static Plan Compile(
        ActivityKind intent,
        Citizen citizen,
        Household household,
        Pathfinder? pathfinder,
        Congestion? congestion)
    {
        citizen.Route.Clear();
        if (intent == ActivityKind.None)
            return default;

        var (targetX, targetY) = TargetOf(intent, household);
        var phase = citizen.X == targetX && citizen.Y == targetY
            ? PlanPhase.Perform
            : PlanPhase.Travel;

        if (phase == PlanPhase.Travel && pathfinder is not null && congestion is not null)
            pathfinder.Find(citizen.X, citizen.Y, targetX, targetY, congestion, citizen.Route);

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
