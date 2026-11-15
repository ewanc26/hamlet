namespace Hamlet.Sim.Agent;

/// <summary>
/// A stage-dependent merged body of citizens (§6.6). At town scale the members
/// of one household merge into a single "body" for shared trips so the player
/// (and the planner) reasons about one agent with a mass; a divergent member
/// event splits it again. Merge/de-merge must be lossless: the same citizens in
/// the same id order round-trip exactly, with the need histogram intact.
/// </summary>
public sealed class AgentBody
{
    private readonly List<long> _members;
    private readonly List<Citizen> _agents;
    private readonly int[] _needHistogram;

    public long BodyId { get; init; }

    public int Mass => _members.Count;

    /// <summary>Member ids in stable (ascending) id order — never input order (§6.6).</summary>
    public IReadOnlyList<long> Members => _members;

    /// <summary>The member citizens, same order as <see cref="Members"/>.</summary>
    public IReadOnlyList<Citizen> Agents => _agents;

    /// <summary>The lowest-id member: the body's planner and movement stand-in.</summary>
    public long RepresentativeId => _members[0];

    internal AgentBody(long bodyId, IReadOnlyList<Citizen> members)
    {
        BodyId = bodyId;
        _members = new List<long>(members.Count);
        _agents = new List<Citizen>(members.Count);
        _needHistogram = new int[Needs.Count * 101];
        foreach (var member in members)
        {
            _members.Add(member.Id);
            _agents.Add(member);
            for (var kind = 0; kind < Needs.Count; kind++)
                _needHistogram[kind * 101 + member.Needs[(NeedKind)kind]]++;
        }
    }

    /// <summary>How many members hold this need at this value on the 0-100 scale.</summary>
    public int HistogramAt(NeedKind kind, int needValue) => _needHistogram[(int)kind * 101 + needValue];

    public int HistogramTotal => _members.Count;
}

/// <summary>
/// Owns all citizens and bodies. Spawn order fixes identity; merges sort by id
/// so a body's member list is stable no matter the input order (§6.6).
/// </summary>
public sealed class AgentBodies
{
    /// <summary>First merge tier is family trips: households larger than this never merge (cohort bodies are the city stage, §6.6).</summary>
    public const int MaxBodyMembers = 8;

    private readonly List<Citizen> _citizens = new();
    private readonly Dictionary<long, Citizen> _byId = new();
    private readonly Dictionary<long, AgentBody> _bodies = new();
    private readonly List<AgentBody> _bodyOrder = new(); // insertion order: deterministic iteration
    private readonly Dictionary<long, long> _bodyOfCitizen = new();
    private long _nextCitizenId = 1;
    private long _nextBodyId = 1;

    /// <summary>Spawn order is id order: the deterministic iteration order (§7.3).</summary>
    public IReadOnlyList<Citizen> Citizens => _citizens;
    public IReadOnlyList<AgentBody> Bodies => _bodyOrder;

    public Citizen SpawnCitizen() => SpawnCitizen(CitizenTraits.Baseline);

    public Citizen SpawnCitizen(CitizenTraits traits)
    {
        var citizen = new Citizen { Id = _nextCitizenId++, Traits = traits };
        _citizens.Add(citizen);
        _byId[citizen.Id] = citizen;
        return citizen;
    }

    /// <summary>The body this citizen currently rides, or null when individual.</summary>
    public long? BodyOf(long citizenId) =>
        _bodyOfCitizen.TryGetValue(citizenId, out var bodyId) ? bodyId : null;

    public AgentBody? GetBody(long bodyId) =>
        _bodies.TryGetValue(bodyId, out var body) ? body : null;

    public Citizen? FindCitizen(long id) => _byId.TryGetValue(id, out var citizen) ? citizen : null;

    /// <summary>Merge citizens into one body; returns the new body. Input order never matters.</summary>
    public AgentBody Merge(IReadOnlyList<long> citizenIds)
    {
        var sorted = new List<long>(citizenIds);
        sorted.Sort();
        var members = new List<Citizen>(sorted.Count);
        foreach (var id in sorted)
            members.Add(_byId[id]); // unknown id: loud failure, never silent drops
        var body = new AgentBody(_nextBodyId++, members);
        _bodies[body.BodyId] = body;
        _bodyOrder.Add(body);
        foreach (var id in sorted)
            _bodyOfCitizen[id] = body.BodyId;
        return body;
    }

    /// <summary>Dissolve a body; the members return as individual citizens, id-stable and lossless.</summary>
    public IReadOnlyList<long> DeMerge(long bodyId)
    {
        var body = _bodies.Remove(bodyId, out var removed) && removed is not null
            ? removed
            : throw new KeyNotFoundException($"no body {bodyId}");
        _bodyOrder.Remove(body);
        foreach (var id in body.Members)
            _bodyOfCitizen.Remove(id);
        return body.Members;
    }
}
