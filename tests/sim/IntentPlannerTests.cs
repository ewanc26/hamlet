using Hamlet.Sim.Agent;
using Xunit;

namespace Hamlet.Sim.Tests;

public class IntentPlannerTests
{
    [Fact]
    public void PlanStartsWithTravelWhenAwayFromTheTarget()
    {
        var citizen = new Citizen { Id = 1, X = 5, Y = 5 };
        var household = new Household(id: 1, homeX: 0, homeY: 0, workX: 30, workY: 10, meals: 10);

        var plan = IntentPlanner.Compile(ActivityKind.Work, citizen, household);

        Assert.Equal(ActivityKind.Work, plan.Intent);
        Assert.Equal(PlanPhase.Travel, plan.Phase);
        Assert.Equal(30, plan.TargetX);
        Assert.Equal(10, plan.TargetY);
    }

    [Fact]
    public void PlanStartsPerformingWhenAlreadyAtTheTarget()
    {
        var citizen = new Citizen { Id = 1, X = 10, Y = 10 };
        var household = new Household(id: 1, homeX: 10, homeY: 10, workX: 30, workY: 10, meals: 10);

        var plan = IntentPlanner.Compile(ActivityKind.Eat, citizen, household);

        Assert.Equal(PlanPhase.Perform, plan.Phase);
        Assert.Equal(10, plan.TargetX);
        Assert.Equal(10, plan.TargetY);
    }

    [Fact]
    public void NoneCompilesToAnEmptyPlan()
    {
        var citizen = new Citizen { Id = 1 };
        var household = new Household(id: 1, homeX: 0, homeY: 0, workX: 1, workY: 1, meals: 0);

        Assert.True(IntentPlanner.Compile(ActivityKind.None, citizen, household).IsNone);
    }
}
