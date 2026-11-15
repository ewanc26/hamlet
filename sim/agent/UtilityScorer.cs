using Hamlet.Sim.Core;

namespace Hamlet.Sim.Agent;

/// <summary>
/// Utility scoring over the need vector (§6.2): urgency × trait weight ×
/// availability − distance cost. Pure integer arithmetic, deterministic, and
/// allocation-free; ties keep the earlier activity in enum order.
/// </summary>
public static class UtilityScorer
{
    /// <summary>A challenger must beat the running plan by this much to pre-empt it (§6.2).</summary>
    public const int SwitchMargin = 12;

    private const int MealThreshold = 60;
    private const int DaySleepThreshold = 40;
    private const int NightSleepDrive = 35;
    private const int WorkDrive = 55;
    private const int DistanceDivisor = 4;

    private static readonly ActivityKind[] Candidates =
    {
        ActivityKind.Sleep,
        ActivityKind.Eat,
        ActivityKind.Work,
    };

    public static ActivityKind Best(Citizen citizen, Household household, Clock clock, DaySchedule schedule)
    {
        var best = ActivityKind.None;
        var bestScore = 0;
        foreach (var activity in Candidates)
        {
            var score = Score(activity, citizen, household, clock, schedule);
            if (score > bestScore)
            {
                best = activity;
                bestScore = score;
            }
        }
        return best;
    }

    public static int Score(ActivityKind activity, Citizen citizen, Household household, Clock clock, DaySchedule schedule)
    {
        var needs = citizen.Needs;
        return activity switch
        {
            ActivityKind.Sleep => SleepScore(citizen, household, clock, schedule),
            ActivityKind.Eat when needs[NeedKind.Hunger] < MealThreshold && household.MealsStocked > 0 =>
                Scale(100 - needs[NeedKind.Hunger], citizen.Traits.Weight(NeedKind.Hunger))
                - Distance(citizen, household.HomeX, household.HomeY),
            ActivityKind.Work when schedule.IsShift(clock.Hour) && citizen.LastWorkDay != clock.Day =>
                Scale(WorkDrive, citizen.Traits.Diligence)
                - Distance(citizen, household.WorkX, household.WorkY),
            _ => 0,
        };
    }

    private static int SleepScore(Citizen citizen, Household household, Clock clock, DaySchedule schedule)
    {
        var energy = citizen.Needs[NeedKind.Energy];
        int urgency;
        if (schedule.IsNight(clock.Hour))
        {
            urgency = Math.Max(100 - energy, NightSleepDrive);
        }
        else
        {
            if (energy >= DaySleepThreshold)
                return 0;
            urgency = 100 - energy;
        }
        return Scale(urgency, citizen.Traits.Weight(NeedKind.Energy))
            - Distance(citizen, household.HomeX, household.HomeY);
    }

    private static int Scale(int urgency, int weight) => urgency * weight / CitizenTraits.Neutral;

    private static int Distance(Citizen citizen, int targetX, int targetY) =>
        (Math.Abs(citizen.X - targetX) + Math.Abs(citizen.Y - targetY)) / DistanceDivisor;
}
