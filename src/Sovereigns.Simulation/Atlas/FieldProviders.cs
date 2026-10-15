using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Atlas;

/// <summary>An authoritative override of one native cell, with the time it was written.</summary>
public sealed record CellOverride(IReadOnlyList<double> Values, SimTime At);

/// <summary>
/// Deterministic base data of a grid field: generated from seed and generator version, regenerable at any time, so
/// never saved (ADR-0008). World generation (Phase 04) supplies real sources; a constant source serves fields whose
/// base is uniform, such as an unpainted scratch field.
/// </summary>
internal interface IBaseSource
{
    /// <summary>Fills a chunk's native cells, components interleaved per cell, row by row from the south-west.</summary>
    void Fill(ChunkKey key, double[] values, int components);

    /// <summary>True when every cell of the chunk has the same base value.</summary>
    bool IsUniform(ChunkKey key, out IReadOnlyList<double> value);
}

internal sealed class ConstantBase(IReadOnlyList<double> value) : IBaseSource
{
    public void Fill(ChunkKey key, double[] values, int components)
    {
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = value[index % components];
        }
    }

    public bool IsUniform(ChunkKey key, out IReadOnlyList<double> uniform)
    {
        uniform = value;
        return true;
    }
}

internal abstract class FieldProvider(FieldDefinition definition)
{
    public FieldDefinition Definition { get; } = definition;

    public int Components => Definition.Kind == FieldKind.Vector ? 2 : 1;
}

/// <summary>A field computed directly from position and time, such as latitude; it has no grid.</summary>
internal sealed class AnalyticProvider(FieldDefinition definition, Func<World, WorldPoint, double[]> evaluate,
    Func<World, SimTime> changedAt, string source, string derivation) : FieldProvider(definition)
{
    public double[] Evaluate(World world, WorldPoint point) => evaluate(world, point);

    public SimTime ChangedAt(World world) => changedAt(world);

    public string Source { get; } = source;

    public string Derivation { get; } = derivation;
}

/// <summary>
/// A base or stored grid field: base data plus sparse authoritative writes at the native level. Every write is kept,
/// including one that restores the base value, so the time each cell last changed is part of saved state.
/// </summary>
internal sealed class GridProvider(FieldDefinition definition, IBaseSource baseSource) : FieldProvider(definition)
{
    private readonly Dictionary<(long I, long J), CellOverride> _overrides = [];
    private readonly Dictionary<ChunkKey, int> _overridesPerChunk = [];
    private readonly Dictionary<ChunkKey, SimTime> _latestWritePerChunk = [];

    public IBaseSource Base { get; } = baseSource;

    public int OverrideCount => _overrides.Count;

    public bool TryGetOverride(long i, long j, out CellOverride value) => _overrides.TryGetValue((i, j), out value!);

    public bool HasOverridesIn(ChunkKey key) => _overridesPerChunk.ContainsKey(key);

    /// <summary>Latest write in a native chunk, derived from the kept writes; null if the chunk was never written.</summary>
    public SimTime? LatestWriteIn(ChunkKey key) => _latestWritePerChunk.TryGetValue(key, out var at) ? at : null;

    /// <summary>Overrides in ascending (j, i) order: the canonical order for checksums and saves.</summary>
    public IEnumerable<(GridCell Cell, CellOverride Override)> Overrides() =>
        _overrides.OrderBy(pair => pair.Key.J).ThenBy(pair => pair.Key.I)
            .Select(pair => (new GridCell(Definition.NativeLevel, pair.Key.I, pair.Key.J), pair.Value));

    /// <summary>Writes one native cell.</summary>
    public void Set(GridCell cell, IReadOnlyList<double> values, SimTime at)
    {
        var chunk = cell.Chunk;
        if (!_latestWritePerChunk.TryGetValue(chunk, out var latest) || at > latest)
        {
            _latestWritePerChunk[chunk] = at;
        }

        if (_overrides.TryAdd((cell.I, cell.J), new CellOverride(values, at)))
        {
            _overridesPerChunk[chunk] = _overridesPerChunk.GetValueOrDefault(chunk) + 1;
        }
        else
        {
            _overrides[(cell.I, cell.J)] = new CellOverride(values, at);
        }
    }
}

/// <summary>The gradient (∂/∂x east, ∂/∂y north) of a scalar grid field at the same native level.</summary>
internal sealed class GradientProvider(FieldDefinition definition, string source) : FieldProvider(definition)
{
    public string Source { get; } = source;
}
