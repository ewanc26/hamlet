using Hamlet.Sim.Agent;
using Hamlet.Sim.Core;
using Xunit;

namespace Hamlet.Sim.Tests;

public class UtilityScorerTests
{
    private static Clock At(int hour)
    {
        var clock = new Clock();
        for (var minute = 0; minute < hour * Clock.MinutesPerHour; minute++)
            clock.Advance();
        return clock;
    }

    private static Household HomeWith(int meals = 10, int workX = 2) =>
        new(id: 1, homeX: 0, homeY: 0, workX: workX, workY: 0, meals: meals);

    private static Citizen CitizenWith(int hunger = 80, int energy = 90)
    {
        var citizen = new Citizen { Id = 1, Needs = new Needs(100) };
        citizen.Needs.Decay(NeedKind.Hunger, 100 - hunger);
        citizen.Needs.Decay(NeedKind.Energy, 100 - energy);
        return citizen;
    }

    [Fact]
    public void NightChoosesSleepWhenRested()
    {
        var clock = new Clock(); // midnight
        Assert.Equal(ActivityKind.Sleep, UtilityScorer.Best(CitizenWith(), HomeWith(), clock, DaySchedule.Default));
    }

    [Fact]
    public void WorkIsChosenDuringTheShiftWhenNeedsAreComfortable()
    {
        var clock = At(10);
        Assert.Equal(ActivityKind.Work, UtilityScorer.Best(CitizenWith(), HomeWith(), clock, DaySchedule.Default));
    }

    [Fact]
    public void HungerOutranksWorkAtLunch()
    {
        var clock = At(13);
        Assert.Equal(ActivityKind.Eat, UtilityScorer.Best(CitizenWith(hunger: 30), HomeWith(), clock, DaySchedule.Default));
    }

    [Fact]
    public void RestedCitizenDoesNotNapDuringTheDay()
    {
        var clock = At(13);
        Assert.Equal(0, UtilityScorer.Score(ActivityKind.Sleep, CitizenWith(energy: 90), HomeWith(), clock, DaySchedule.Default));
    }

    [Fact]
    public void EmptyPantryRemovesEating()
    {
        var clock = At(13);
        Assert.Equal(0, UtilityScorer.Score(
            ActivityKind.Eat, CitizenWith(hunger: 10), HomeWith(meals: 0), clock, DaySchedule.Default));
    }

    [Fact]
    public void DistanceCostsUtility()
    {
        var clock = At(10);
        var citizen = CitizenWith();
        var near = UtilityScorer.Score(ActivityKind.Work, citizen, HomeWith(workX: 2), clock, DaySchedule.Default);
        var far = UtilityScorer.Score(ActivityKind.Work, citizen, HomeWith(workX: 40), clock, DaySchedule.Default);
        Assert.True(near > far);
    }
}
