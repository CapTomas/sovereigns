namespace Sovereigns.Simulation.Ids;

/// <summary>
/// Per-world, per-kind monotonic ID counters (SOV-P02-T08). Allocation in one kind never shifts IDs in another,
/// and the counters are saved, so an ID is never issued twice in a world's lifetime.
/// </summary>
public sealed class IdAllocator
{
    private readonly SortedDictionary<string, ulong> _last = new(StringComparer.Ordinal);

    /// <summary>The last ID issued per kind name, in ordinal order of the name.</summary>
    public IReadOnlyDictionary<string, ulong> Counters => _last;

    /// <summary>Allocates the next ID. Internal: hosts change state only through commands (ADR-0007).</summary>
    internal EntityId<TKind> Next<TKind>()
        where TKind : IEntityKind
    {
        var next = checked(LastIssued<TKind>() + 1);
        _last[TKind.Name] = next;
        return new EntityId<TKind>(next);
    }

    public ulong LastIssued<TKind>()
        where TKind : IEntityKind => _last.GetValueOrDefault(TKind.Name);

    /// <summary>True when <paramref name="id"/> is not none and was issued by this world.</summary>
    public bool WasIssued<TKind>(EntityId<TKind> id)
        where TKind : IEntityKind => !id.IsNone && id.Value <= LastIssued<TKind>();

    internal void Restore(IReadOnlyDictionary<string, ulong> counters)
    {
        _last.Clear();
        foreach (var (kind, last) in counters)
        {
            _last[kind] = last;
        }
    }
}
