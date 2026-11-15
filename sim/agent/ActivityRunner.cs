using Hamlet.Sim.Core;

namespace Hamlet.Sim.Agent;

/// <summary>
/// Executes one plan step per tick (§6.2): travel one tile, or perform the
/// activity. M1 movement is a deterministic straight-line walk at grid
/// granularity — road-graph pathfinding and congestion are M2. Returns true
/// when the plan completes so the planner can pick the next one.
/// </summary>
public static class ActivityRunner
{
    public static bool Tick(Citizen citizen, Household household, Clock clock, DaySchedule schedule, NeedRules rules)
    {
        var plan = citizen.Plan;
        if (plan.IsNone)
            return false;

        if (plan.Phase == PlanPhase.Travel)
        {
            if (citizen.X == plan.TargetX && citizen.Y == plan.TargetY)
            {
                citizen.Plan = plan.StartPerforming();
                return false;
            }
            StepToward(citizen, plan.TargetX, plan.TargetY);
            return false;
        }

        return Perform(citizen, household, clock, schedule, rules, plan);
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

        citizen.Plan = default;
        return true;
    }
}
