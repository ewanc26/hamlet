using Hamlet.Sim.Agent;
using Xunit;

namespace Hamlet.Sim.Tests;

public class AgentBodyTests
{
    [Fact]
    public void MergeOrdersMembersStablyRegardlessOfInput()
    {
        var bodies = new AgentBodies();
        var ids = new List<long>();
        for (var i = 0; i < 5; i++)
            ids.Add(bodies.SpawnCitizen().Id);

        var reversed = ids.Reverse<long>().ToList();
        var body = bodies.Merge(reversed);

        Assert.Equal(5, body.Mass);
        Assert.Equal(ids, body.Members);
    }

    [Fact]
    public void BodyHistsSumToMass()
    {
        var bodies = new AgentBodies();
        var ids = new List<long>();
        for (var i = 0; i < 5; i++)
            ids.Add(bodies.SpawnCitizen().Id);
        var body = bodies.Merge(ids);

        Assert.Equal(5, body.HistogramTotal);
        // Citizens spawn with needs at 80 (§3.1 defaults).
        Assert.Equal(5, body.HistogramAt(NeedKind.Hunger, 80));
    }

    [Fact]
    public void DeMergeRoundTripsTheSameCitizens()
    {
        var bodies = new AgentBodies();
        var ids = new List<long>();
        for (var i = 0; i < 5; i++)
            ids.Add(bodies.SpawnCitizen().Id);
        var body = bodies.Merge(ids);

        var demerged = bodies.DeMerge(body.BodyId);
        Assert.Equal(ids, demerged);
        Assert.Empty(bodies.Bodies);
        Assert.Equal(5, bodies.Citizens.Count);
    }

    [Fact]
    public void MergingAnUnknownCitizenFailsLoudly()
    {
        var bodies = new AgentBodies();
        Assert.Throws<KeyNotFoundException>(() => bodies.Merge(new long[] { 42 }));
    }
}
