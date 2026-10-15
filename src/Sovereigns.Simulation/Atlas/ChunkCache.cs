namespace Sovereigns.Simulation.Atlas;

/// <summary>What a cached chunk holds.</summary>
public enum ChunkContent
{
    /// <summary>Native cells: base data with authoritative overrides applied.</summary>
    Native,

    /// <summary>Native cells of a derived field, computed from its sources.</summary>
    Derived,

    /// <summary>Coarser cells holding means of native cells.</summary>
    Aggregate,

    /// <summary>
    /// Refinement terms of a conservatively refined field, one set per native cell, which finer-level samples use
    /// (resident state "refined").
    /// </summary>
    Refined,
}

/// <summary>
/// Residency of an area of a field between calls (ADR-0007, ADR-0009). Loading happens synchronously inside the query
/// that needs it, so no caller ever observes it.
/// </summary>
public enum Residency
{
    Unloaded,
    Loaded,
    Refined,
}

/// <summary>A resident chunk, for diagnostics and the shell's residency overlay.</summary>
public sealed record ResidentChunk(string Field, ChunkKey Key, ChunkContent Content, int Bytes);

/// <summary>Cache counters since the world was created or loaded; diagnostics only.</summary>
public sealed record ChunkCacheStatistics(long Hits, long Loads, long Evictions, long Invalidations, long ResidentBytes, int ResidentChunks, long BudgetBytes);

/// <summary>
/// Least-recently-used cache of materialized chunks under a byte budget (SOV-P03-T07, T10). Everything here can be
/// recomputed from base sources and authoritative overrides, so eviction and invalidation never change any answer;
/// the cache is never saved and never part of a checksum.
/// </summary>
internal sealed class ChunkCache(long budgetBytes)
{
    private readonly Dictionary<(string Field, ChunkKey Key, ChunkContent Content), LinkedListNode<Entry>> _entries = [];
    private readonly LinkedList<Entry> _recency = new();
    private long _hits;
    private long _loads;
    private long _evictions;
    private long _invalidations;
    private long _residentBytes;

    public long BudgetBytes { get; set; } = budgetBytes;

    public ChunkCacheStatistics Statistics =>
        new(_hits, _loads, _evictions, _invalidations, _residentBytes, _entries.Count, BudgetBytes);

    public double[] GetOrAdd(string field, ChunkKey key, ChunkContent content, Func<double[]> materialize)
    {
        if (_entries.TryGetValue((field, key, content), out var node))
        {
            _hits++;
            _recency.Remove(node);
            _recency.AddFirst(node);
            return node.Value.Values;
        }

        _loads++;
        var values = materialize();
        var entry = new Entry(field, key, content, values);
        _entries[(field, key, content)] = _recency.AddFirst(entry);
        _residentBytes += entry.Bytes;
        while (_residentBytes > BudgetBytes && _recency.Count > 1)
        {
            Remove(_recency.Last!);
            _evictions++;
        }

        return values;
    }

    /// <summary>Drops cached chunks of the given fields whose world bounds intersect the rectangle.</summary>
    public void Invalidate(IReadOnlyCollection<string> fields, double west, double south, double east, double north)
    {
        foreach (var node in _entries.Values.ToList())
        {
            var (w, s, e, n) = node.Value.Key.Bounds;
            if (fields.Contains(node.Value.Field) && w < east && e > west && s < north && n > south)
            {
                Remove(node);
                _invalidations++;
            }
        }
    }

    public void Clear()
    {
        _evictions += _entries.Count;
        _entries.Clear();
        _recency.Clear();
        _residentBytes = 0;
    }

    public Residency ResidencyOf(string field, ChunkKey key) =>
        _entries.ContainsKey((field, key, ChunkContent.Refined)) ? Residency.Refined
        : _entries.ContainsKey((field, key, ChunkContent.Native)) || _entries.ContainsKey((field, key, ChunkContent.Derived)) ||
          _entries.ContainsKey((field, key, ChunkContent.Aggregate)) ? Residency.Loaded
        : Residency.Unloaded;

    public IReadOnlyList<ResidentChunk> Resident() =>
        [.. _recency.Select(entry => new ResidentChunk(entry.Field, entry.Key, entry.Content, entry.Bytes))];

    private void Remove(LinkedListNode<Entry> node)
    {
        _entries.Remove((node.Value.Field, node.Value.Key, node.Value.Content));
        _recency.Remove(node);
        _residentBytes -= node.Value.Bytes;
    }

    private sealed record Entry(string Field, ChunkKey Key, ChunkContent Content, double[] Values)
    {
        public int Bytes => Values.Length * sizeof(double);
    }
}
