using Hamlet.Sim.Core;
using Xunit;

namespace Hamlet.Sim.Tests;

public class ClockTests
{
    [Fact]
    public void TickAdvancesOneSimMinute()
    {
        var clock = new Clock();
        clock.Advance();
        Assert.Equal(1, clock.MinuteOfDay);
    }

    [Fact]
    public void DayRollsOverAfterTicksPerDay()
    {
        var clock = new Clock();
        for (long tick = 0; tick < Clock.TicksPerDay; tick++)
            clock.Advance();
        Assert.Equal(0, clock.MinuteOfDay);
        Assert.Equal(2, clock.DayOfMonth);
    }

    [Fact]
    public void YearRollsOverAfterTwelveMonthsOfThirtyDays()
    {
        var clock = new Clock();
        var ticksPerYear = (long)Clock.TicksPerDay * Clock.DaysPerMonth * Clock.MonthsPerYear;
        for (long tick = 0; tick < ticksPerYear; tick++)
            clock.Advance();
        Assert.Equal(1, clock.DayOfMonth);
        Assert.Equal(1, clock.Month);
        Assert.Equal(2, clock.Year);
    }
}
