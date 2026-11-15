using Hamlet.Sim.Agent;
using Xunit;

namespace Hamlet.Sim.Tests;

public class NeedsSystemTests
{
    [Fact]
    public void NeedsDecayOnTheirOwnIntegerIntervals()
    {
        var citizen = new Citizen { Id = 0, Needs = new Needs(100) };

        NeedsSystem.Decay(citizen, tick: 1);
        Assert.Equal(100, citizen.Needs[NeedKind.Hunger]);

        NeedsSystem.Decay(citizen, tick: 12);
        Assert.Equal(99, citizen.Needs[NeedKind.Hunger]);
        Assert.Equal(100, citizen.Needs[NeedKind.Energy]);

        NeedsSystem.Decay(citizen, tick: 15);
        Assert.Equal(99, citizen.Needs[NeedKind.Energy]);

        NeedsSystem.Decay(citizen, tick: 120);
        Assert.Equal(99, citizen.Needs[NeedKind.Health]);
    }

    [Fact]
    public void WealthNeverDecaysOnItsOwn()
    {
        var citizen = new Citizen { Id = 0, Needs = new Needs(100) };
        for (long tick = 1; tick <= 1_000; tick++)
            NeedsSystem.Decay(citizen, tick);

        Assert.Equal(100, citizen.Needs[NeedKind.Wealth]);
    }
}
