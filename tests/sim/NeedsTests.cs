using Hamlet.Sim.Agent;
using Xunit;

namespace Hamlet.Sim.Tests;

public class NeedsTests
{
    [Fact]
    public void InitialValuesClampToTheHundredScale()
    {
        var needs = new Needs(999);
        Assert.Equal(100, needs[NeedKind.Hunger]);
        Assert.Equal(100, needs[NeedKind.Wealth]);
    }

    [Fact]
    public void DecayQuantisesAndClampsAtZero()
    {
        var needs = new Needs(100);
        needs.Decay(NeedKind.Hunger, 15.25f); // 84.75 → 85
        Assert.Equal(85, needs[NeedKind.Hunger]);
        for (var i = 0; i < 200; i++)
            needs.Decay(NeedKind.Hunger, 10f);
        Assert.Equal(0, needs[NeedKind.Hunger]);
    }

    [Fact]
    public void RestoreQuantisesAndClampsAtOneHundred()
    {
        var needs = new Needs(40);
        needs.Restore(NeedKind.Social, 20.6f); // 60.6 → 61
        Assert.Equal(61, needs[NeedKind.Social]);
        needs.Restore(NeedKind.Social, 500f);
        Assert.Equal(100, needs[NeedKind.Social]);
    }

    [Fact]
    public void CrisisThresholdIsExclusive()
    {
        var needs = new Needs(15);
        Assert.False(needs.InCrisis(NeedKind.Health));
        needs.Decay(NeedKind.Health, 1f); // 14
        Assert.True(needs.InCrisis(NeedKind.Health));
    }
}
