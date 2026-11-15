using Hamlet.Sim.Agent;
using Hamlet.Sim.Core;
using Xunit;

namespace Hamlet.Sim.Tests;

public class CitizenTraitsTests
{
    [Fact]
    public void BaselineMatchesTheDefaultNeedRules()
    {
        var rules = NeedRules.Default;
        for (var kind = 0; kind < Needs.Count; kind++)
        {
            Assert.Equal(CitizenTraits.Neutral, CitizenTraits.Baseline.Weight((NeedKind)kind));
            Assert.Equal(rules.DecayEveryTicks((NeedKind)kind), CitizenTraits.Baseline.DecayEveryTicks((NeedKind)kind));
        }
        Assert.Equal(CitizenTraits.Neutral, CitizenTraits.Baseline.Diligence);
    }

    [Fact]
    public void RolledTraitsStayOnTheHundredScaleAndKeepEveryIntervalPositive()
    {
        var traits = CitizenTraits.Roll(new DeterministicRandom(42), NeedRules.Default);
        for (var kind = 0; kind < Needs.Count; kind++)
        {
            var need = (NeedKind)kind;
            Assert.InRange(traits.Weight(need), 25, 75);
            if (NeedRules.Default.DecayEveryTicks(need) == 0)
                Assert.Equal(0, traits.DecayEveryTicks(need));
            else
                Assert.True(traits.DecayEveryTicks(need) > 0);
        }
        Assert.InRange(traits.Diligence, 25, 75);
    }

    [Fact]
    public void RollIsDeterministicPerSeed()
    {
        static int[] Snapshot(CitizenTraits traits)
        {
            var values = new int[Needs.Count * 2 + 1];
            for (var kind = 0; kind < Needs.Count; kind++)
            {
                values[kind] = traits.Weight((NeedKind)kind);
                values[Needs.Count + kind] = traits.DecayEveryTicks((NeedKind)kind);
            }
            values[^1] = traits.Diligence;
            return values;
        }

        var first = CitizenTraits.Roll(new DeterministicRandom(4242), NeedRules.Default);
        var second = CitizenTraits.Roll(new DeterministicRandom(4242), NeedRules.Default);
        var other = CitizenTraits.Roll(new DeterministicRandom(4243), NeedRules.Default);

        Assert.Equal(Snapshot(first), Snapshot(second));
        Assert.NotEqual(Snapshot(first), Snapshot(other));
    }
}
