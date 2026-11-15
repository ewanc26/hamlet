using Hamlet.Sim.Core;

namespace Hamlet.Sim.Agent;

/// <summary>
/// Executes one plan step per tick (§6.2): travel one tile, or perform the
/// activity. Travel follows the road route compiled by <see cref="IntentPlanner"/>
/// when one exists; with no connecting road it falls back to the direct
/// straight-line walk, so road-less worlds still run (§7.2).
/// </summary>
public static class ActivityRunner
{
    public static bool Tick(
        Citizen citizen,
        Household household,
        Clock clock,
        DaySchedule schedule,
        NeedRules rules,
        int gridSize)
    {
        var plan = citizen.Plan;
        if (plan.IsNone)
            return false;

        if (plan.Phase == PlanPhase.Travel)
        {
            if (citizen.X == plan.TargetX && citizen.Y == plan.TargetY)
            {
                citizen.Route.Clear();
                citizen.Plan = plan.StartPerforming();
                return false;
            }
            StepToward(citizen, plan, gridSize);
            return false;
        }

        return Perform(citizen, household, clock, schedule, rules, plan);
    }

    /// <summary>Walk one route tile; a stale or absent route walks the direct line.</summary>
    private static void StepToward(Citizen citizen, Plan plan, int gridSize)
    {
        var route = citizen.Route;
        if (route.HasNext)
        {
            var tile = route.NextTile;
            var nextX = tile % gridSize;
            var nextY = tile / gridSize;
            if (Math.Abs(nextX - citizen.X) + Math.Abs(nextY - citizen.Y) == 1)
            {
                citizen.X = nextX;
                citizen.Y = nextY;
                route.Advance();
                return;
            }
            route.Clear(); // the world changed under the route: walk directly this tick
        }

        StepToward(citizen, plan.TargetX, plan.TargetY);
    }

    private static void StepToward(Citizen citizen, int targetX, int targetY)
    {
        if (citizen.X != targetX)
            citizen.X += Math.Sign(targetX - citizen.X);
        else if (citizen.Y != targetY)
            citizen.Y += Math.Sign(targetY - citizen.Y);
    }

    private static bool Perform(Citizen citizen, Household household, Clock clock, DaySchedule schedule, NeedRules rules, Plan plan)
    {
        var next = plan.Advance();
        switch (plan.Intent)
        {
            case ActivityKind.Sleep:
            {
                if (clock.Tick % rules.SleepTicksPerEnergyPoint == 0)
                    citizen.Needs.Restore(NeedKind.Energy, 1f);
                var wake = clock.MinuteOfDay == schedule.WakeHour * Clock.MinutesPerHour;
                var rested = citizen.Needs[NeedKind.Energy] >= 100
                    && clock.Hour >= schedule.WakeHour
                    && clock.Hour < schedule.NightStartHour;
                if (!wake && !rested)
                {
                    citizen.Plan = next;
                    return false;
                }
                break;
            }

            case ActivityKind.Eat:
            {
                if (next.PhaseTicks < rules.MealTicks)
                {
                    citizen.Plan = next;
                    return false;
                }
                if (household.TryConsumeMeal())
                    citizen.Needs.Restore(NeedKind.Hunger, rules.MealHungerRestore);
                break;
            }

            case ActivityKind.Work:
            {
                if (clock.Hour < schedule.ShiftEndHour)
                {
                    citizen.Plan = next;
                    return false;
                }
                citizen.LastWorkDay = clock.Day;
                break;
            }
        }

        citizen.Route.Clear();
        citizen.Plan = default;
        return true;
    }
}
