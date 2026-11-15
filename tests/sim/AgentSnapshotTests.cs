using Hamlet.Sim;
using Hamlet.Sim.Agent;
using Hamlet.Sim.Core;
using Xunit;

namespace Hamlet.Sim.Tests;

/// <summary>
/// Presentation snapshots are plain values captured at a tick boundary (§4,
/// rule 3). They must mirror the live citizen and household exactly — the
/// scene tree never receives a live sim reference.
/// </summary>
public class AgentSnapshotTests
{
    private static Simulation FoundedWorld(ulong seed = 3)
    {
        var simulation = new Simulation(seed, 32);
        simulation.Enqueue(new FoundHousehold
        {
            HomeX = 4,
            HomeY = 4,
            WorkX = 20,
            WorkY = 4,
            Members = 1,
        });
        simulation.TickOnce();
        simulation.ClearEvents();
        return simulation;
    }

    [Fact]
    public void CitizenSnapshotMirrorsTheLiveCitizen()
    {
        var simulation = FoundedWorld();
        var citizen = Assert.Single(simulation.Bodies.Citizens);

        var snapshot = citizen.Snapshot();

        Assert.Equal(citizen.Id, snapshot.Id);
        Assert.Equal(citizen.X, snapshot.X);
        Assert.Equal(citizen.Y, snapshot.Y);
        Assert.Equal(citizen.Plan.Intent, snapshot.Activity);
        Assert.Equal(citizen.Needs[NeedKind.Hunger], snapshot.Hunger);
        Assert.Equal(citizen.Needs[NeedKind.Energy], snapshot.Energy);
        Assert.InRange(snapshot.Hunger, 0, 100);
        Assert.InRange(snapshot.Energy, 0, 100);
    }

    [Fact]
    public void HouseholdSnapshotMirrorsThePantry()
    {
        var simulation = FoundedWorld();
        var household = Assert.Single(simulation.Households.Households);

        var full = household.Snapshot();
        Assert.Equal(household.Id, full.Id);
        Assert.Equal(household.HomeX, full.HomeX);
        Assert.Equal(household.WorkX, full.WorkX);
        Assert.Equal(Household.DefaultPantryMeals, full.MealsStocked);

        Assert.True(household.TryConsumeMeal());
        Assert.True(household.TryConsumeMeal());
        Assert.Equal(Household.DefaultPantryMeals - 2, household.Snapshot().MealsStocked);
    }
}
