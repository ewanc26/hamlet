using Hamlet.Sim;
using Hamlet.Sim.Agent;
using Hamlet.Sim.City;
using Hamlet.Sim.Core;
using Xunit;

namespace Hamlet.Sim.Tests;

/// <summary>
/// LOD tier 2 (§6.5): the camera focus square arrives as a SetCameraFocus
/// command; citizens inside it run full LOD (one tile per tick), citizens
/// outside run standard LOD (coarse jumps to the plan anchor) while their
/// needs still decay every tick.
/// </summary>
public class LodTierTests
{
    private const int HomeX = 8;
    private const int WorkX = 24;
    private const int StreetY = 10;

    private static Simulation TownWithFocus(int focusX, int focusY, int radius)
    {
        var simulation = new Simulation(999, 64);
        for (var x = HomeX; x <= WorkX; x++)
            simulation.Enqueue(new PlaceRoad { X = x, Y = StreetY });
        simulation.Enqueue(new FoundHousehold
        {
            HomeX = HomeX,
            HomeY = StreetY,
            WorkX = WorkX,
            WorkY = StreetY,
            Members = 1,
            Meals = 30,
        });
        simulation.Enqueue(new SetCameraFocus { X = focusX, Y = focusY, Radius = radius });
        simulation.TickOnce();
        simulation.ClearEvents();
        return simulation;
    }

    private static void TickUntil(Simulation simulation, Func<bool> condition, int maxTicks)
    {
        for (var i = 0; i < maxTicks && !condition(); i++)
        {
            simulation.TickOnce();
            simulation.ClearEvents();
        }
    }

    private static void TickToShift(Simulation simulation)
    {
        while (simulation.Clock.Hour < 9)
        {
            simulation.TickOnce();
            simulation.ClearEvents();
        }
    }

    [Fact]
    public void CoarseTravelJumpsStraightToThePlanAnchor()
    {
        var citizen = new Citizen { Id = 1, X = 0, Y = 0 };
        var household = new Household(1, 5, 0, 9, 9, meals: 10);
        citizen.Plan = new Plan
        {
            Intent = ActivityKind.Work,
            Phase = PlanPhase.Travel,
            TargetX = 5,
            TargetY = 0,
        };

        ActivityRunner.Tick(citizen, household, new Clock(), DaySchedule.Default, NeedRules.Default, 64, coarse: true);

        Assert.Equal(5, citizen.X);
        Assert.Equal(0, citizen.Y);
        Assert.Equal(PlanPhase.Perform, citizen.Plan.Phase);
    }

    [Fact]
    public void OffCameraCitizenTeleportsInsteadOfStepping()
    {
        var simulation = TownWithFocus(focusX: 60, focusY: 60, radius: 4);
        var citizen = simulation.Bodies.Citizens[0];
        TickToShift(simulation);
        TickUntil(simulation, () => citizen.Plan is { Intent: ActivityKind.Work, Phase: PlanPhase.Travel }, 600);
        Assert.Equal(PlanPhase.Travel, citizen.Plan.Phase);
        Assert.False(simulation.IsFullLod(citizen));

        var before = (citizen.X, citizen.Y);
        simulation.TickOnce();
        simulation.ClearEvents();

        var jumped = Math.Abs(before.X - citizen.X) + Math.Abs(before.Y - citizen.Y);
        Assert.True(jumped > 1, $"coarse traveller should jump, moved {jumped}");
        Assert.Equal(citizen.Plan.TargetX, citizen.X);
        Assert.Equal(citizen.Plan.TargetY, citizen.Y);
    }

    [Fact]
    public void OnCameraCitizenStepsOneTilePerTick()
    {
        var simulation = TownWithFocus(focusX: 16, focusY: StreetY, radius: 8);
        var citizen = simulation.Bodies.Citizens[0];
        TickToShift(simulation);
        TickUntil(simulation, () => citizen.Plan is { Intent: ActivityKind.Work, Phase: PlanPhase.Travel }, 600);
        Assert.Equal(PlanPhase.Travel, citizen.Plan.Phase);
        Assert.True(simulation.IsFullLod(citizen));

        var before = (citizen.X, citizen.Y);
        simulation.TickOnce();
        simulation.ClearEvents();

        var beforeDist = Math.Abs(before.X - WorkX) + Math.Abs(before.Y - StreetY);
        var afterDist = Math.Abs(citizen.X - WorkX) + Math.Abs(citizen.Y - StreetY);
        Assert.Equal(beforeDist - 1, afterDist);
    }

    [Fact]
    public void OffCameraNeedsStillDecayEveryTick()
    {
        var simulation = TownWithFocus(focusX: 60, focusY: 60, radius: 4);
        var citizen = simulation.Bodies.Citizens[0];
        var hungerBefore = citizen.Needs[NeedKind.Hunger];
        for (var i = 0; i < 60; i++)
        {
            simulation.TickOnce();
            simulation.ClearEvents();
        }
        Assert.True(citizen.Needs[NeedKind.Hunger] < hungerBefore, "standard LOD keeps full needs decay");
    }

    [Fact]
    public void NoFocusMeansFullLodEverywhere()
    {
        var simulation = new Simulation(999, 64);
        simulation.Enqueue(new FoundHousehold
        {
            HomeX = HomeX,
            HomeY = StreetY,
            WorkX = WorkX,
            WorkY = StreetY,
            Members = 1,
        });
        simulation.TickOnce();
        simulation.ClearEvents();

        Assert.Null(simulation.Focus);
        Assert.True(simulation.IsFullLod(simulation.Bodies.Citizens[0]));
    }

    [Fact]
    public void FocusIsPartOfTheDeterministicCommandStream()
    {
        Simulation Run(int radius)
        {
            var simulation = TownWithFocus(16, StreetY, radius);
            for (var i = 0; i < 2000; i++)
            {
                simulation.TickOnce();
                simulation.ClearEvents();
            }
            return simulation;
        }

        Assert.Equal(Run(8).ComputeStateHash().Value, Run(8).ComputeStateHash().Value);
        Assert.NotEqual(Run(8).ComputeStateHash().Value, Run(9).ComputeStateHash().Value);
    }
}
