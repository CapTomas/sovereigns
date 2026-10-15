using System.Globalization;
using Sovereigns.Simulation.Fields;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Atlas;

/// <summary>Area statistics of a field over a rectangle at its native level (SOV-P03-T09).</summary>
/// <param name="Mean">Area-weighted mean, each native cell weighted by its overlap with the rectangle and the world.</param>
public sealed record AreaStatistics(FieldDefinition Field, double Mean, double Min, double Max, double AreaM2, long Cells);

/// <summary>
/// The queryable, multi-resolution physical-field atlas of one world (SOV-P03, ADR-0009). Every query is answered from
/// authoritative data (base sources, overrides, the clock and identity) through recomputable chunks, so answers never
/// depend on what is resident, cached or viewed. The atlas is part of the world; hosts read it and change it only
/// through commands.
/// </summary>
public sealed class FieldAtlas
{
    /// <summary>Fields whose authoritative overrides this build stores and saves.</summary>
    public static IReadOnlyList<string> StoredGridFields { get; } = [FieldCatalog.Scratch.Key];

    /// <summary>Default cache budget: 64 MiB of chunk data.</summary>
    public const long DefaultBudgetBytes = 64L * 1024 * 1024;

    private readonly World _world;
    private readonly Dictionary<string, FieldProvider> _providers = new(StringComparer.Ordinal);
    private readonly ChunkCache _cache = new(DefaultBudgetBytes);
    private readonly Dictionary<(string, ChunkContent), long> _computations = [];

    internal FieldAtlas(World world)
    {
        _world = world;
        var extent = world.Identity.Extent;
        Register(new AnalyticProvider(FieldCatalog.Latitude, (w, p) => [w.Identity.Geography.LatitudeAt(p.Y)], w => w.Identity.Start,
            "geographic reference: south-edge latitude", "φ = φ₀ + y / 6 371 000 m"));
        Register(new AnalyticProvider(FieldCatalog.LocalSolarTime,
            (w, p) => [w.Identity.Geography.LocalSolarTimeOfDayMs(w.Now, p.X, extent) / 1000.0], w => w.Now,
            "world clock; geographic reference: standard meridian", "standard time + (x − x_std) / (R cos φ_mid) · 86 400 s / 2π"));
        Register(new GridProvider(FieldCatalog.Scratch, new ConstantBase([0.0])));
        Register(new GradientProvider(FieldCatalog.ScratchGradient, FieldCatalog.Scratch.Key));
    }

    public long BudgetBytes
    {
        get => _cache.BudgetBytes;
        set => _cache.BudgetBytes = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    public ChunkCacheStatistics CacheStatistics => _cache.Statistics;

    public bool IsAvailable(string field) => _providers.ContainsKey(field);

    /// <summary>True when the field is held in chunks (grid and grid-derived fields); analytic fields have no chunks.</summary>
    public bool IsChunked(string field) => _providers.TryGetValue(field, out var provider) && provider is not AnalyticProvider;

    /// <summary>Where a field's values come from, for provenance (SOV-P02-T13, SOV-P03-T13).</summary>
    public string SourceOf(string field) => _providers.TryGetValue(field, out var provider)
        ? provider switch
        {
            AnalyticProvider analytic => analytic.Source,
            GradientProvider gradient => $"{gradient.Source} (central differences)",
            _ => $"base data and stored writes of {field}",
        }
        : FieldCatalog.Get(field) is { DependsOn.Count: > 0 } planned ? string.Join(", ", planned.DependsOn) : field;

    /// <summary>Recomputations of one field's chunks of one content kind; shows caching and invalidation at work.</summary>
    public long Computations(string field, ChunkContent content) => _computations.GetValueOrDefault((field, content));

    public IReadOnlyList<ResidentChunk> ResidentChunks() => _cache.Resident();

    /// <summary>
    /// Residency of the chunk that answers queries of <paramref name="field"/> at <paramref name="level"/> near a
    /// point. Finer than the native level, that is the native chunk: refined when its refinement terms are cached.
    /// </summary>
    public Residency ResidencyAt(string field, GridLevel level, WorldPoint point)
    {
        var native = _providers.TryGetValue(field, out var provider) ? provider.Definition.NativeLevel : level;
        return _cache.ResidencyOf(field, Grid.ChunkAt(level > native ? native : level, point));
    }

    /// <summary>Drops every cached chunk. Answers do not change; this exists to prove that (SOV-P03-T17).</summary>
    public void Unload() => _cache.Clear();


    /// <summary>
    /// The value of <paramref name="field"/> at <paramref name="point"/>, as seen at <paramref name="level"/> (the
    /// field's native level by default). Coarser levels see means of native cells; finer levels see the refined
    /// surface (ADR-0009). Outside the world, and for fields no phase has implemented yet, the sample is unavailable.
    /// </summary>
    public FieldSample Sample(string field, WorldPoint point, GridLevel? level = null)
    {
        var definition = _providers.TryGetValue(field, out var provider) ? provider.Definition : FieldCatalog.Get(field);
        var at = level ?? definition.NativeLevel;
        if (provider is null)
        {
            return FieldSample.Unavailable(definition, point, at,
                $"unavailable: {definition.Owner} implements {definition.Name.ToLowerInvariant()} in {definition.ImplementedBy}");
        }

        if (!point.IsFinite || !_world.Extent.Contains(point))
        {
            return FieldSample.Unavailable(definition, point, at, "unavailable: the point is outside the world frame");
        }

        if (provider is AnalyticProvider analytic)
        {
            return new FieldSample(definition, point, at, analytic.Evaluate(_world, point), ReadingBasis.Derived,
                $"analytic: {analytic.Derivation}", [], analytic.ChangedAt(_world));
        }

        var native = definition.NativeLevel;
        if (at <= native)
        {
            return Interpolated(provider, point, at, aggregate: at < native);
        }

        var sample = Interpolated(provider, point, native, aggregate: false);
        if (definition.Refinement == Refinement.Conserve)
        {
            var cell = Grid.CellAt(native, point);
            var values = sample.Values!.ToArray();
            Refine(provider, cell, values);
            return sample with
            {
                Level = at,
                Values = values,
                Classification = $"refined to {Grid.Name(at)}: conservative (cell mean preserved, kept within range)",
                ChangedAt = ChangedAt(provider, sample.Inputs, cell),
            };
        }

        return sample with { Level = at, Classification = $"refined to {Grid.Name(at)}: bilinear" };
    }

    /// <summary>
    /// Samples a regular grid of points for overlays: <paramref name="columns"/> × <paramref name="rows"/> cell
    /// centres from the south-west corner (<paramref name="west"/>, <paramref name="south"/>), spaced
    /// <paramref name="spacingM"/> apart. Returns components interleaved per point, row by row from the south; NaN
    /// where there is no value. Values equal <see cref="Sample"/> exactly.
    /// </summary>
    public double[] SampleArea(string field, GridLevel level, double west, double south, double spacingM, int columns, int rows)
    {
        var definition = _providers.TryGetValue(field, out var known) ? known.Definition : FieldCatalog.Get(field);
        var components = definition.Kind == FieldKind.Vector ? 2 : 1;
        var result = new double[columns * rows * components];
        Array.Fill(result, double.NaN);
        if (!_providers.TryGetValue(field, out var provider))
        {
            return result;
        }

        var values = new double[components];
        var stencil = new (GridCell Cell, double Weight)[4];
        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                var point = new WorldPoint(west + (column + 0.5) * spacingM, south + (row + 0.5) * spacingM);
                if (!_world.Extent.Contains(point))
                {
                    continue;
                }

                LeanValues(provider, point, level, values, stencil);
                Array.Copy(values, 0, result, ((row * columns) + column) * components, components);
            }
        }

        return result;
    }

    /// <summary>
    /// Mean, minimum and maximum over a rectangle at the field's native level (SOV-P03-T09). Each native cell is weighted
    /// by the area of its overlap with the rectangle and the world, so totals (mean × area) are exact for any rectangle.
    /// Native chunks are visited one at a time, and uniform chunks are not materialized, so memory stays bounded.
    /// </summary>
    public AreaStatistics Aggregate(string field, double west, double south, double east, double north)
    {
        if (!_providers.TryGetValue(field, out var provider) || provider is AnalyticProvider || provider.Definition.Kind != FieldKind.Scalar)
        {
            throw new InvalidOperationException($"{field} has no native scalar grid to aggregate");
        }

        var extent = _world.Extent;
        (west, south, east, north) = (Math.Max(0, west), Math.Max(0, south), Math.Min(extent.East, east), Math.Min(extent.North, north));
        var definition = provider.Definition;
        if (east <= west || north <= south)
        {
            return new AreaStatistics(definition, double.NaN, double.NaN, double.NaN, 0, 0);
        }

        var level = definition.NativeLevel;
        var size = Grid.CellSize(level);
        var (i0, j0) = ((long)Math.Floor(west / size), (long)Math.Floor(south / size));
        var (i1, j1) = ((long)Math.Ceiling(east / size) - 1, (long)Math.Ceiling(north / size) - 1);
        double weighted = 0, area = 0, min = double.PositiveInfinity, max = double.NegativeInfinity;
        long cells = 0;
        for (var cy = Grid.FloorDiv(j0, Grid.ChunkCells); cy <= Grid.FloorDiv(j1, Grid.ChunkCells); cy++)
        {
            for (var cx = Grid.FloorDiv(i0, Grid.ChunkCells); cx <= Grid.FloorDiv(i1, Grid.ChunkCells); cx++)
            {
                var chunk = new ChunkKey(level, cx, cy);
                var uniform = UniformValue(provider, chunk);
                var values = uniform is null ? ChunkValues(provider, chunk) : null;
                var (ci1, cj1) = (Math.Min(i1, chunk.FirstI + Grid.ChunkCells - 1), Math.Min(j1, chunk.FirstJ + Grid.ChunkCells - 1));
                for (var j = Math.Max(j0, chunk.FirstJ); j <= cj1; j++)
                {
                    var overlapY = Math.Min(north, (j + 1) * size) - Math.Max(south, j * size);
                    for (var i = Math.Max(i0, chunk.FirstI); i <= ci1; i++)
                    {
                        var overlapX = Math.Min(east, (i + 1) * size) - Math.Max(west, i * size);
                        var value = uniform?[0] ?? values![Offset(new GridCell(level, i, j), chunk, 1)];
                        weighted += value * overlapX * overlapY;
                        area += overlapX * overlapY;
                        min = Math.Min(min, value);
                        max = Math.Max(max, value);
                        cells++;
                    }
                }
            }
        }

        return new AreaStatistics(definition, weighted / area, min, max, area, cells);
    }

    /// <summary>
    /// Writes a stored field's native cells whose centres lie within <paramref name="radiusM"/> of a point and whose
    /// value differs; returns how many changed. Every write is kept, including one back to the base value.
    /// </summary>
    internal int Paint(string field, WorldPoint centre, double radiusM, double value)
    {
        var provider = (GridProvider)_providers[field];
        var level = provider.Definition.NativeLevel;
        var size = Grid.CellSize(level);
        var (maxI, maxJ) = MaxIndex(level);
        var (i0, j0) = (Math.Max(0, (long)Math.Floor((centre.X - radiusM) / size)), Math.Max(0, (long)Math.Floor((centre.Y - radiusM) / size)));
        var (i1, j1) = (Math.Min(maxI, (long)Math.Floor((centre.X + radiusM) / size)), Math.Min(maxJ, (long)Math.Floor((centre.Y + radiusM) / size)));
        var changed = 0;
        for (var j = j0; j <= j1; j++)
        {
            for (var i = i0; i <= i1; i++)
            {
                var cell = new GridCell(level, i, j);
                var dx = cell.Centre.X - centre.X;
                var dy = cell.Centre.Y - centre.Y;
                if (dx * dx + dy * dy > radiusM * radiusM)
                {
                    continue;
                }

                var current = provider.TryGetOverride(i, j, out var existing) ? existing.Values[0] : BaseValues(provider, cell)[0];
                if (current == value)
                {
                    continue;
                }

                provider.Set(cell, [value], _world.Now);
                changed++;
            }
        }

        if (changed > 0)
        {
            Invalidate(field, i0 * size, j0 * size, (i1 + 1) * size, (j1 + 1) * size);
        }

        return changed;
    }

    internal IEnumerable<(string Field, GridCell Cell, CellOverride Override)> AllOverrides() =>
        _providers.Values.OfType<GridProvider>().OrderBy(provider => provider.Definition.Key, StringComparer.Ordinal)
            .SelectMany(provider => provider.Overrides().Select(entry => (provider.Definition.Key, entry.Cell, entry.Override)));

    internal void RestoreOverride(string field, GridCell cell, CellOverride value)
    {
        if (!_providers.TryGetValue(field, out var provider) || provider is not GridProvider grid)
        {
            throw new InvalidOperationException($"field '{field}' has no stored grid in this build");
        }

        grid.Set(cell, value.Values, value.At);
    }

    /// <summary>Number of authoritative writes a stored field holds; diagnostics.</summary>
    public int OverrideCount(string field) => _providers.TryGetValue(field, out var provider) && provider is GridProvider grid ? grid.OverrideCount : 0;

    /// <summary>
    /// Adds a field provider; internal so that tests can exercise contracts no Phase 03 field uses yet. Category fields
    /// are refused until majority-class aggregation exists (ADR-0009), so no coarse level can average class indices.
    /// </summary>
    internal void Register(FieldProvider provider)
    {
        if (provider.Definition.Kind == FieldKind.Category)
        {
            throw new NotSupportedException($"{provider.Definition.Key}: category fields need majority-class aggregation, which arrives with the first category field (ADR-0009)");
        }

        _providers.Add(provider.Definition.Key, provider);
    }

    /// <summary>
    /// Drops cached chunks of a field and of every field that depends on it, transitively, near a changed area. The halo
    /// is one native cell of the coarsest affected field, which covers finite differences and refinement terms.
    /// </summary>
    private void Invalidate(string field, double west, double south, double east, double north)
    {
        var affected = new HashSet<string>(StringComparer.Ordinal) { field };
        var grew = true;
        while (grew)
        {
            grew = false;
            foreach (var provider in _providers.Values)
            {
                if (!affected.Contains(provider.Definition.Key) && provider.Definition.DependsOn.Any(affected.Contains))
                {
                    affected.Add(provider.Definition.Key);
                    grew = true;
                }
            }
        }

        var halo = affected.Max(key => Grid.CellSize(_providers[key].Definition.NativeLevel));
        _cache.Invalidate(affected, west - halo, south - halo, east + halo, north + halo);
    }

    /// <summary>The grid cells and weights that interpolate a point at a level, clamped to the world's cells; returns how many.</summary>
    private int Stencil(Interpolation interpolation, WorldPoint point, GridLevel level, (GridCell Cell, double Weight)[] stencil)
    {
        var (maxI, maxJ) = MaxIndex(level);
        GridCell At(long i, long j) => new(level, Math.Clamp(i, 0, maxI), Math.Clamp(j, 0, maxJ));
        if (interpolation == Interpolation.Nearest)
        {
            var cell = Grid.CellAt(level, point);
            stencil[0] = (At(cell.I, cell.J), 1);
            return 1;
        }

        var size = Grid.CellSize(level);
        var u = point.X / size - 0.5;
        var v = point.Y / size - 0.5;
        var i0 = (long)Math.Floor(u);
        var j0 = (long)Math.Floor(v);
        var fx = u - i0;
        var fy = v - j0;
        stencil[0] = (At(i0, j0), (1 - fx) * (1 - fy));
        stencil[1] = (At(i0 + 1, j0), fx * (1 - fy));
        stencil[2] = (At(i0, j0 + 1), (1 - fx) * fy);
        stencil[3] = (At(i0 + 1, j0 + 1), fx * fy);
        return 4;
    }

    private ArraySegment<double> GridValues(FieldProvider provider, GridCell cell) =>
        cell.Level < provider.Definition.NativeLevel ? AggregateValues(provider, cell) : CellValues(provider, cell);

    private FieldSample Interpolated(FieldProvider provider, WorldPoint point, GridLevel level, bool aggregate)
    {
        var definition = provider.Definition;
        var stencil = new (GridCell Cell, double Weight)[4];
        var count = Stencil(definition.Interpolation, point, level, stencil);
        var components = provider.Components;
        var values = new double[components];
        var inputs = new GridInput[count];
        for (var k = 0; k < count; k++)
        {
            var (cell, weight) = stencil[k];
            var cellValues = GridValues(provider, cell);
            for (var c = 0; c < components; c++)
            {
                values[c] += weight * cellValues[c];
            }

            var stored = !aggregate && provider is GridProvider grid && grid.TryGetOverride(cell.I, cell.J, out _);
            inputs[k] = new GridInput(cell, weight, cellValues, stored);
        }

        var classification = aggregate
            ? $"aggregate: bilinear over {Grid.Name(level)} cell means of {Grid.Name(definition.NativeLevel)} cells"
            : provider is GradientProvider gradient ? $"derived: bilinear over central differences of {gradient.Source}"
            : inputs.Any(input => input.Stored) ? "stored: bilinear over native cells with overrides"
            : "base: bilinear over native cells";
        var basis = provider is GradientProvider || aggregate ? ReadingBasis.Derived : ReadingBasis.Stored;
        return new FieldSample(definition, point, level, values, basis, classification, inputs, ChangedAt(provider, inputs, refined: null));
    }

    /// <summary>The same values as <see cref="Sample"/> without provenance, for bulk sampling.</summary>
    private void LeanValues(FieldProvider provider, WorldPoint point, GridLevel level, double[] values, (GridCell Cell, double Weight)[] stencil)
    {
        if (provider is AnalyticProvider analytic)
        {
            analytic.Evaluate(_world, point).CopyTo(values, 0);
            return;
        }

        var definition = provider.Definition;
        var count = Stencil(definition.Interpolation, point, level < definition.NativeLevel ? level : definition.NativeLevel, stencil);
        Array.Clear(values);
        for (var k = 0; k < count; k++)
        {
            var (cell, weight) = stencil[k];
            var cellValues = GridValues(provider, cell);
            for (var c = 0; c < values.Length; c++)
            {
                values[c] += weight * cellValues[c];
            }
        }

        if (level > definition.NativeLevel && definition.Refinement == Refinement.Conserve)
        {
            Refine(provider, Grid.CellAt(definition.NativeLevel, point), values);
        }
    }

    /// <summary>
    /// Turns native bilinear values inside <paramref name="cell"/> into conservatively refined ones: shifted by the
    /// cell's correction, then blended towards the cell value by its limiter (see <see cref="MaterializeRefined"/>).
    /// </summary>
    private void Refine(FieldProvider provider, GridCell cell, double[] values)
    {
        var chunk = cell.Chunk;
        var components = provider.Components;
        var terms = _cache.GetOrAdd(provider.Definition.Key, chunk, ChunkContent.Refined, () => MaterializeRefined(provider, chunk));
        var offset = Offset(cell, chunk, 2 * components);
        var own = CellValues(provider, cell);
        for (var c = 0; c < components; c++)
        {
            values[c] = own[c] + terms[offset + components + c] * (values[c] + terms[offset + c] - own[c]);
        }
    }

    /// <summary>
    /// When the sample's inputs last changed: the latest kept write among the native cells it depends on, exactly for
    /// native, derived and refined samples (their stencils), and at chunk granularity for coarse cells, never earlier.
    /// Unwritten cells date from the world's start, when base data was generated.
    /// </summary>
    private SimTime? ChangedAt(FieldProvider provider, IReadOnlyList<GridInput> inputs, GridCell? refined)
    {
        var (grid, reach) = provider switch
        {
            GridProvider own => (own, 0),
            GradientProvider gradient => ((GridProvider)_providers[gradient.Source], 1),
            _ => (null, 0),
        };
        if (grid is null)
        {
            return null;
        }

        var native = grid.Definition.NativeLevel;
        var (maxI, maxJ) = MaxIndex(native);
        SimTime? latest = null;
        void Consider(SimTime? at)
        {
            if (at is { } time && (latest is null || time > latest.Value))
            {
                latest = time;
            }
        }

        void Cell(long i, long j)
        {
            if (i >= 0 && j >= 0 && i <= maxI && j <= maxJ && grid.TryGetOverride(i, j, out var written))
            {
                Consider(written.At);
            }
        }

        foreach (var input in inputs)
        {
            var (i, j) = (input.Cell.I, input.Cell.J);
            if (input.Cell.Level == native)
            {
                // A native value, or a central difference over the four neighbours.
                Cell(i, j);
                for (var step = 1; step <= reach; step++)
                {
                    Cell(i - step, j);
                    Cell(i + step, j);
                    Cell(i, j - step);
                    Cell(i, j + step);
                }

                continue;
            }

            var factor = Grid.Factor(input.Cell.Level, native);
            var (ni0, nj0) = (Math.Max(0, i * factor - reach), Math.Max(0, j * factor - reach));
            var (ni1, nj1) = (Math.Min(maxI, (i + 1) * factor - 1 + reach), Math.Min(maxJ, (j + 1) * factor - 1 + reach));
            for (var cy = Grid.FloorDiv(nj0, Grid.ChunkCells); cy <= Grid.FloorDiv(nj1, Grid.ChunkCells); cy++)
            {
                for (var cx = Grid.FloorDiv(ni0, Grid.ChunkCells); cx <= Grid.FloorDiv(ni1, Grid.ChunkCells); cx++)
                {
                    Consider(grid.LatestWriteIn(new ChunkKey(native, cx, cy)));
                }
            }
        }

        if (refined is { } centre)
        {
            // Refinement terms use the 3 × 3 neighbourhood of the containing cell.
            for (var j = centre.J - 1 - reach; j <= centre.J + 1 + reach; j++)
            {
                for (var i = centre.I - 1 - reach; i <= centre.I + 1 + reach; i++)
                {
                    Cell(i, j);
                }
            }
        }

        return latest ?? _world.Identity.Start;
    }

    /// <summary>Values of one native cell of a grid or derived field.</summary>
    private ArraySegment<double> CellValues(FieldProvider provider, GridCell cell)
    {
        var chunk = cell.Chunk;
        return new ArraySegment<double>(ChunkValues(provider, chunk), Offset(cell, chunk, provider.Components), provider.Components);
    }

    /// <summary>All native cells of one chunk, components interleaved; fetched once per chunk by bulk computations.</summary>
    private double[] ChunkValues(FieldProvider provider, ChunkKey chunk) => provider switch
    {
        GridProvider grid => _cache.GetOrAdd(grid.Definition.Key, chunk, ChunkContent.Native, () => MaterializeNative(grid, chunk)),
        GradientProvider gradient => _cache.GetOrAdd(gradient.Definition.Key, chunk, ChunkContent.Derived, () => MaterializeGradient(gradient, chunk)),
        _ => throw new InvalidOperationException($"{provider.Definition.Key} has no grid"),
    };

    private static int Offset(GridCell cell, ChunkKey chunk, int components) =>
        (int)(((cell.J - chunk.FirstJ) * Grid.ChunkCells + (cell.I - chunk.FirstI)) * components);

    /// <summary>
    /// Reads native cells through a per-computation chunk table, so a starved cache cannot thrash. Used only for
    /// stencils within one chunk of halo, so it holds at most nine chunks.
    /// </summary>
    private sealed class ChunkReader(FieldAtlas atlas, FieldProvider provider)
    {
        private readonly Dictionary<ChunkKey, double[]> _chunks = [];

        public double Value(GridCell cell, int component)
        {
            var chunk = cell.Chunk;
            if (!_chunks.TryGetValue(chunk, out var values))
            {
                _chunks[chunk] = values = atlas.ChunkValues(provider, chunk);
            }

            return values[Offset(cell, chunk, provider.Components) + component];
        }
    }

    private static IReadOnlyList<double> BaseValues(GridProvider provider, GridCell cell)
    {
        if (provider.Base.IsUniform(cell.Chunk, out var uniform))
        {
            return uniform;
        }

        var values = new double[Grid.ChunkCells * Grid.ChunkCells * provider.Components];
        provider.Base.Fill(cell.Chunk, values, provider.Components);
        return new ArraySegment<double>(values, Offset(cell, cell.Chunk, provider.Components), provider.Components);
    }

    private double[] MaterializeNative(GridProvider provider, ChunkKey chunk)
    {
        Count(provider.Definition.Key, ChunkContent.Native);
        var components = provider.Components;
        var values = new double[Grid.ChunkCells * Grid.ChunkCells * components];
        provider.Base.Fill(chunk, values, components);
        if (provider.HasOverridesIn(chunk))
        {
            for (var y = 0; y < Grid.ChunkCells; y++)
            {
                for (var x = 0; x < Grid.ChunkCells; x++)
                {
                    if (provider.TryGetOverride(chunk.FirstI + x, chunk.FirstJ + y, out var written))
                    {
                        for (var c = 0; c < components; c++)
                        {
                            values[((y * Grid.ChunkCells) + x) * components + c] = written.Values[c];
                        }
                    }
                }
            }
        }

        return values;
    }

    private double[] MaterializeGradient(GradientProvider provider, ChunkKey chunk)
    {
        Count(provider.Definition.Key, ChunkContent.Derived);
        var source = (GridProvider)_providers[provider.Source];
        var values = new double[Grid.ChunkCells * Grid.ChunkCells * 2];
        if (UniformNative(source, chunk, halo: true) is not null)
        {
            return values;
        }

        var size = Grid.CellSize(chunk.Level);
        var (maxI, maxJ) = MaxIndex(chunk.Level);
        var reader = new ChunkReader(this, source);
        double S(long i, long j) => reader.Value(new GridCell(chunk.Level, i, j), 0);
        for (var y = 0; y < Grid.ChunkCells; y++)
        {
            for (var x = 0; x < Grid.ChunkCells; x++)
            {
                var (i, j) = (Math.Clamp(chunk.FirstI + x, 0, maxI), Math.Clamp(chunk.FirstJ + y, 0, maxJ));
                var (west, east) = (Math.Max(0, i - 1), Math.Min(maxI, i + 1));
                var (south, north) = (Math.Max(0, j - 1), Math.Min(maxJ, j + 1));
                var index = ((y * Grid.ChunkCells) + x) * 2;
                values[index] = east == west ? 0 : (S(east, j) - S(west, j)) / ((east - west) * size);
                values[index + 1] = north == south ? 0 : (S(i, north) - S(i, south)) / ((north - south) * size);
            }
        }

        return values;
    }

    /// <summary>
    /// Refinement terms of every native cell of a chunk: per component, a correction, then a limiter. The correction
    /// shifts the bilinear surface so its mean over the cell equals the cell value; that mean weights the 3 × 3
    /// neighbourhood by (1/8, 3/4, 1/8) per axis. The shifted surface is bilinear between the cell's centre, edge
    /// midpoints and corners, so its extremes are among those nine points. Where they leave the field's range, the
    /// limiter θ ∈ [0, 1] blends the surface towards the cell value, v + θ (s − v), which keeps the mean and the range.
    /// </summary>
    private double[] MaterializeRefined(FieldProvider provider, ChunkKey chunk)
    {
        Count(provider.Definition.Key, ChunkContent.Refined);
        var definition = provider.Definition;
        var components = provider.Components;
        var (maxI, maxJ) = MaxIndex(chunk.Level);
        var reader = new ChunkReader(this, provider);
        var terms = new double[Grid.ChunkCells * Grid.ChunkCells * 2 * components];
        double[] weights = [0.125, 0.75, 0.125];
        var neighbourhood = new double[9];
        for (var y = 0; y < Grid.ChunkCells; y++)
        {
            for (var x = 0; x < Grid.ChunkCells; x++)
            {
                var (i, j) = (Math.Clamp(chunk.FirstI + x, 0, maxI), Math.Clamp(chunk.FirstJ + y, 0, maxJ));
                var index = ((y * Grid.ChunkCells) + x) * 2 * components;
                for (var c = 0; c < components; c++)
                {
                    double N(int a, int b) => neighbourhood[(b + 1) * 3 + a + 1];
                    var mean = 0.0;
                    for (var b = -1; b <= 1; b++)
                    {
                        for (var a = -1; a <= 1; a++)
                        {
                            var value = reader.Value(new GridCell(chunk.Level, Math.Clamp(i + a, 0, maxI), Math.Clamp(j + b, 0, maxJ)), c);
                            neighbourhood[(b + 1) * 3 + a + 1] = value;
                            mean += weights[a + 1] * weights[b + 1] * value;
                        }
                    }

                    var own = N(0, 0);
                    var correction = own - mean;
                    double low = own + correction, high = low;
                    for (var b = -1; b <= 1; b++)
                    {
                        for (var a = -1; a <= 1; a++)
                        {
                            var shifted = ((own + N(a, 0) + N(0, b) + N(a, b)) / 4) + correction;
                            (low, high) = (Math.Min(low, shifted), Math.Max(high, shifted));
                        }
                    }

                    var limit = 1.0;
                    if (high > definition.Max)
                    {
                        limit = Math.Min(limit, (definition.Max - own) / (high - own));
                    }

                    if (low < definition.Min)
                    {
                        limit = Math.Min(limit, (own - definition.Min) / (own - low));
                    }

                    terms[index + c] = correction;
                    terms[index + components + c] = Math.Max(0, limit);
                }
            }
        }

        return terms;
    }

    /// <summary>Values of one coarser-level cell: the in-world-area-weighted mean of the native cells under it.</summary>
    private ArraySegment<double> AggregateValues(FieldProvider provider, GridCell cell)
    {
        var chunk = cell.Chunk;
        var values = _cache.GetOrAdd(provider.Definition.Key, chunk, ChunkContent.Aggregate, () => MaterializeAggregate(provider, chunk));
        return new ArraySegment<double>(values, Offset(cell, chunk, provider.Components), provider.Components);
    }

    /// <summary>
    /// Means of native cells for every cell of a coarse chunk, each native cell weighted by the part of it inside the
    /// world, so coarse values agree with <see cref="Aggregate"/>. Native chunks are visited one at a time and uniform
    /// ones are added without materializing them, so memory stays bounded whatever the world's size.
    /// </summary>
    private double[] MaterializeAggregate(FieldProvider provider, ChunkKey coarse)
    {
        Count(provider.Definition.Key, ChunkContent.Aggregate);
        var native = provider.Definition.NativeLevel;
        var components = provider.Components;
        var factor = Grid.Factor(coarse.Level, native);
        var size = Grid.CellSize(native);
        var extent = _world.Extent;
        var (maxI, maxJ) = MaxIndex(native);
        var sums = new double[Grid.ChunkCells * Grid.ChunkCells * components];
        var weights = new double[Grid.ChunkCells * Grid.ChunkCells];
        var (ni0, nj0) = (Math.Max(0, coarse.FirstI * factor), Math.Max(0, coarse.FirstJ * factor));
        var (ni1, nj1) = (Math.Min(maxI, (coarse.FirstI + Grid.ChunkCells) * factor - 1), Math.Min(maxJ, (coarse.FirstJ + Grid.ChunkCells) * factor - 1));

        // In-world width of native cells first..last along one axis, in cells.
        double InWorld(long first, long last, double limit) => (Math.Min(limit, (last + 1) * size) - first * size) / size;

        for (var cy = Grid.FloorDiv(nj0, Grid.ChunkCells); nj0 <= nj1 && cy <= Grid.FloorDiv(nj1, Grid.ChunkCells); cy++)
        {
            for (var cx = Grid.FloorDiv(ni0, Grid.ChunkCells); ni0 <= ni1 && cx <= Grid.FloorDiv(ni1, Grid.ChunkCells); cx++)
            {
                var chunk = new ChunkKey(native, cx, cy);
                var (ci0, cj0) = (Math.Max(ni0, chunk.FirstI), Math.Max(nj0, chunk.FirstJ));
                var (ci1, cj1) = (Math.Min(ni1, chunk.FirstI + Grid.ChunkCells - 1), Math.Min(nj1, chunk.FirstJ + Grid.ChunkCells - 1));
                if (UniformValue(provider, chunk) is { } uniform)
                {
                    for (var y = Grid.FloorDiv(cj0, factor); y <= Grid.FloorDiv(cj1, factor); y++)
                    {
                        var height = InWorld(Math.Max(cj0, y * factor), Math.Min(cj1, (y + 1) * factor - 1), extent.North);
                        for (var x = Grid.FloorDiv(ci0, factor); x <= Grid.FloorDiv(ci1, factor); x++)
                        {
                            var weight = InWorld(Math.Max(ci0, x * factor), Math.Min(ci1, (x + 1) * factor - 1), extent.East) * height;
                            var target = (int)(((y - coarse.FirstJ) * Grid.ChunkCells) + (x - coarse.FirstI));
                            weights[target] += weight;
                            for (var c = 0; c < components; c++)
                            {
                                sums[target * components + c] += weight * uniform[c];
                            }
                        }
                    }

                    continue;
                }

                var values = ChunkValues(provider, chunk);
                for (var j = cj0; j <= cj1; j++)
                {
                    var height = InWorld(j, j, extent.North);
                    var row = (Grid.FloorDiv(j, factor) - coarse.FirstJ) * Grid.ChunkCells;
                    for (var i = ci0; i <= ci1; i++)
                    {
                        var weight = InWorld(i, i, extent.East) * height;
                        var target = (int)(row + (Grid.FloorDiv(i, factor) - coarse.FirstI));
                        var offset = Offset(new GridCell(native, i, j), chunk, components);
                        weights[target] += weight;
                        for (var c = 0; c < components; c++)
                        {
                            sums[target * components + c] += weight * values[offset + c];
                        }
                    }
                }
            }
        }

        for (var target = 0; target < weights.Length; target++)
        {
            for (var c = 0; c < components; c++)
            {
                sums[target * components + c] = weights[target] > 0 ? sums[target * components + c] / weights[target] : double.NaN;
            }
        }

        return sums;
    }

    /// <summary>The single value of a native chunk when all its cells are equal and known without materializing it.</summary>
    private IReadOnlyList<double>? UniformValue(FieldProvider provider, ChunkKey chunk) => provider switch
    {
        GridProvider grid => UniformNative(grid, chunk, halo: false),
        GradientProvider gradient when UniformNative((GridProvider)_providers[gradient.Source], chunk, halo: true) is not null => [0.0, 0.0],
        _ => null,
    };

    /// <summary>
    /// The common value when a chunk (and, with a halo, its eight neighbours) has a uniform base, all with the same
    /// value, and no overrides; otherwise null.
    /// </summary>
    private static IReadOnlyList<double>? UniformNative(GridProvider provider, ChunkKey chunk, bool halo)
    {
        IReadOnlyList<double>? common = null;
        var reach = halo ? 1 : 0;
        for (var dy = -reach; dy <= reach; dy++)
        {
            for (var dx = -reach; dx <= reach; dx++)
            {
                var neighbour = chunk with { X = chunk.X + dx, Y = chunk.Y + dy };
                if (provider.HasOverridesIn(neighbour) || !provider.Base.IsUniform(neighbour, out var value) ||
                    (common is not null && !common.SequenceEqual(value)))
                {
                    return null;
                }

                common ??= value;
            }
        }

        return common;
    }

    private (long MaxI, long MaxJ) MaxIndex(GridLevel level)
    {
        var (_, _, maxI, maxJ) = Grid.CellRange(level, _world.Extent);
        return (maxI, maxJ);
    }

    private void Count(string field, ChunkContent content) =>
        _computations[(field, content)] = _computations.GetValueOrDefault((field, content)) + 1;

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"atlas: {_providers.Count} fields, {_cache.Statistics.ResidentChunks} chunks resident");
}
