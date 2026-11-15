using Hamlet.Sim.Core;

namespace Hamlet.Sim.Agent;

/// <summary>
/// Executes one plan step per tick (§6.2): travel one tile, or perform the
/// activity. Travel follows the road route compiled by <see cref="IntentPlanner"/>
/// when one exists; with no connecting road it falls back to the direct
/// straight-line walk, so road-less worlds still run (§7.2). Off-camera
/// (standard LOD, §6.5) travellers jump straight to the target instead. When
/// the actor represents a household body (§6.6), perform effects apply to every
/// member and shopping scales by mass.
/// </summary>
public static class ActivityRunner
{
    public static bool Tick(
        Citizen citizen,
        Household household,
        Clock clock,
        DaySchedule schedule,
        NeedRules rules,
        int gridSize,
        bool coarse = false,
        IReadOnlyList<Citizen>? bodyMembers = null)
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
            if (coarse)
            {
                // Standard LOD (§6.5): off-camera citizens skip per-tick nav and
                // jump straight to the plan's anchor point.
                citizen.X = plan.TargetX;
                citizen.Y = plan.TargetY;
                citizen.Route.Clear();
                citizen.Plan = plan.StartPerforming();
                return false;
            }
            StepToward(citizen, plan, gridSize);
            return false;
        }

        return Perform(citizen, household, clock, schedule, rules, plan, bodyMembers);
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

    /// <summary>
    /// Perform the plan. <paramref name="bodyMembers"/> is the full member list
    /// of the household body the actor represents (§6.6) — every member eats,
    /// is paid, and is rested, and shopping scales by mass; null means the
    /// actor is an individual and no list is touched.
    /// </summary>
    private static bool Perform(
        Citizen citizen,
        Household household,
        Clock clock,
        DaySchedule schedule,
        NeedRules rules,
        Plan plan,
        IReadOnlyList<Citizen>? bodyMembers)
    {
        var next = plan.Advance();
        switch (plan.Intent)
        {
            case ActivityKind.Sleep:
            {
                if (clock.Tick % rules.SleepTicksPerEnergyPoint == 0)
                    RestoreEach(citizen, bodyMembers, NeedKind.Energy, 1f);
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
                if (bodyMembers is null)
                {
                    if (household.TryConsumeMeal())
                        citizen.Needs.Restore(NeedKind.Hunger, rules.MealHungerRestore);
                }
                else
                {
                    for (var i = 0; i < bodyMembers.Count; i++)
                        if (household.TryConsumeMeal())
                            bodyMembers[i].Needs.Restore(NeedKind.Hunger, rules.MealHungerRestore);
                }
                break;
            }

            case ActivityKind.Work:
            {
                if (clock.Hour < schedule.ShiftEndHour)
                {
                    citizen.Plan = next;
                    return false;
                }
                if (bodyMembers is null)
                {
                    citizen.LastWorkDay = clock.Day;
                    household.CollectWage();
                }
                else
                {
                    for (var i = 0; i < bodyMembers.Count; i++)
                    {
                        bodyMembers[i].LastWorkDay = clock.Day;
                        household.CollectWage();
                    }
                }
                break;
            }

            case ActivityKind.Shop:
            {
                if (next.PhaseTicks < rules.ShopTicks)
                {
                    citizen.Plan = next;
                    return false;
                }
                household.TryBuyMeals(rules.ShopBundleMeals * (bodyMembers?.Count ?? 1));
                break;
            }
        }

        citizen.Route.Clear();
        citizen.Plan = default;
        return true;
    }

    private static void RestoreEach(Citizen actor, IReadOnlyList<Citizen>? members, NeedKind kind, float amount)
    {
        if (members is null)
        {
            actor.Needs.Restore(kind, amount);
            return;
        }
        for (var i = 0; i < members.Count; i++)
            members[i].Needs.Restore(kind, amount);
    }
}
