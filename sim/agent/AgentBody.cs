namespace Hamlet.Sim.Agent;

/// <summary>
/// A stage-dependent merged body of citizens (§6.6). At hamlet scale citizens
/// can merge into a single "body" so the player (and the AI) reasons about
/// dozens of individuals; as the settlement scales up, bodies de-merge back
/// into individual citizens. Merge/de-merge must be lossless: the same citizens
/// in the same order must round-trip exactly.
/// </summary>
public sealed class AgentBody
{
    private readonly List<long> _members;
    private readonly int[] _needHistogram;

    public long BodyId { get; init; }

    public int Mass => _members.Count;

    public IReadOnlyList<long> Members => _members;

    internal AgentBody(long bodyId, IReadOnlyList<Citizen> members)
    {
        BodyId = bodyId;
        _members = new List<long>(members.Count);
        _needHistogram = new int[Needs.Count * 101];
        foreach (var member in members)
        {
            _members.Add(member.Id);
            _needHistogram[(int)NeedKind.Hunger * 101 + member.Needs[NeedKind.Hunger]]++;
        }
    }

    /// <summary>How many members hold this need at this value on the 0-100 scale.</summary>
    public int HistogramAt(NeedKind kind, int needValue) => _needHistogram[(int)kind * 101 + needValue];

    public int HistogramTotal => _members.Count;
}

/// <summary>
/// Owns all citizens and bodies. Spawn order fixes identity; merges sort by id
/// so a body's member list is stable no matter the input order (§7.3).
/// </summary>
public sealed class AgentBodies
{
    private readonly Dictionary<long, Citizen> _citizens = new();
    private readonly Dictionary<long, AgentBody> _bodies = new();
    private long _nextCitizenId = 1;
    private long _nextBodyId = 1;

    public IReadOnlyCollection<Citizen> Citizens => _citizens.Values;
    public IReadOnlyCollection<AgentBody> Bodies => _bodies.Values;

    public Citizen SpawnCitizen()
    {
        var citizen = new Citizen { Id = _nextCitizenId++ };
        _citizens[citizen.Id] = citizen;
        return citizen;
    }

    /// <summary>Merge citizens into one body; returns the new body. Input order never matters.</summary>
    public AgentBody Merge(IReadOnlyList<long> citizenIds)
    {
        var members = new List<Citizen>(citizenIds.Count);
        var sorted = citizenIds.OrderBy(id => id).ToList();
        foreach (var id in sorted)
            members.Add(_citizens[id]); // unknown id: loud failure, never silent drops
        var body = new AgentBody(_nextBodyId++, members);
        _bodies[body.BodyId] = body;
        return body;
    }

    /// <summary>Dissolve a body; the members return as individual citizens, id-stable.</summary>
    public IReadOnlyList<long> DeMerge(long bodyId)
    {
        var body = _bodies.Remove(bodyId, out var removed) && removed is not null
            ? removed
            : throw new KeyNotFoundException($"no body {bodyId}");
        return body.Members;
    }
}
