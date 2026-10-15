using System.Globalization;
using Sovereigns.Simulation.Geometry;

namespace Sovereigns.Simulation.Atlas;

/// <summary>
/// The four nested resolutions of the world (SOV-P03-T01, ADR-0009). Each is 16 times finer than the one before,
/// and every grid is anchored at the world origin, so cells nest exactly.
/// </summary>
public enum GridLevel
{
    /// <summary>4096 m cells: continental climate, macro statistics.</summary>
    Macro,

    /// <summary>256 m cells: landscape elevation, hydrology, regional land state.</summary>
    Regional,

    /// <summary>16 m cells: local terrain, land use and ground near settlements and routes.</summary>
    Local,

    /// <summary>1 m cells: refined battlefield terrain, materialized only where needed.</summary>
    Tactical,
}

/// <summary>A cell of one level: covers [i·Δ, (i+1)·Δ) × [j·Δ, (j+1)·Δ), with its sample at the centre (ADR-0004 §2).</summary>
public readonly record struct GridCell(GridLevel Level, long I, long J)
{
    public WorldPoint Centre => new((I + 0.5) * Grid.CellSize(Level), (J + 0.5) * Grid.CellSize(Level));

    public ChunkKey Chunk => new(Level, Grid.FloorDiv(I, Grid.ChunkCells), Grid.FloorDiv(J, Grid.ChunkCells));

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Grid.Name(Level)}[{I},{J}]");
}

/// <summary>A square block of <see cref="Grid.ChunkCells"/>² cells of one level: the unit of loading, caching and invalidation.</summary>
public readonly record struct ChunkKey(GridLevel Level, long X, long Y)
{
    public long FirstI => X * Grid.ChunkCells;

    public long FirstJ => Y * Grid.ChunkCells;

    /// <summary>World-frame bounds: west, south, east, north in metres.</summary>
    public (double West, double South, double East, double North) Bounds
    {
        get
        {
            var size = Grid.ChunkSize(Level);
            return (X * size, Y * size, (X + 1) * size, (Y + 1) * size);
        }
    }

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Grid.Name(Level)} chunk ({X}, {Y})");
}

/// <summary>Cell and chunk arithmetic for the world grids. Integer cell indices make lookups exact and platform-independent.</summary>
public static class Grid
{
    /// <summary>Cells per chunk side.</summary>
    public const int ChunkCells = 64;

    /// <summary>Linear refinement factor between consecutive levels.</summary>
    public const int Ratio = 16;

    private static readonly double[] CellSizes = [4096, 256, 16, 1];

    public static double CellSize(GridLevel level) => CellSizes[(int)level];

    public static double ChunkSize(GridLevel level) => CellSize(level) * ChunkCells;

    public static string Name(GridLevel level) => level.ToString().ToLowerInvariant();

    /// <summary>The cell containing <paramref name="point"/>.</summary>
    public static GridCell CellAt(GridLevel level, WorldPoint point)
    {
        var size = CellSize(level);
        return new GridCell(level, (long)Math.Floor(point.X / size), (long)Math.Floor(point.Y / size));
    }

    public static ChunkKey ChunkAt(GridLevel level, WorldPoint point) => CellAt(level, point).Chunk;

    /// <summary>The coarser-level cell that contains <paramref name="cell"/>.</summary>
    public static GridCell Parent(GridCell cell, GridLevel coarser)
    {
        var factor = Factor(coarser, cell.Level);
        return new GridCell(coarser, FloorDiv(cell.I, factor), FloorDiv(cell.J, factor));
    }

    /// <summary>How many cells of <paramref name="finer"/> span one cell of <paramref name="coarser"/> along each axis.</summary>
    public static long Factor(GridLevel coarser, GridLevel finer)
    {
        if (finer < coarser)
        {
            throw new ArgumentException($"{finer} is not finer than {coarser}");
        }

        var factor = 1L;
        for (var level = coarser; level < finer; level++)
        {
            factor *= Ratio;
        }

        return factor;
    }

    public static long FloorDiv(long value, long divisor) =>
        value >= 0 ? value / divisor : -((-(value + 1) / divisor) + 1);

    /// <summary>Cells of a level that overlap the world, as inclusive index ranges; the last row and column may be partial.</summary>
    public static (long MinI, long MinJ, long MaxI, long MaxJ) CellRange(GridLevel level, WorldExtent extent)
    {
        var size = CellSize(level);
        return (0, 0, (long)Math.Ceiling(extent.East / size) - 1, (long)Math.Ceiling(extent.North / size) - 1);
    }
}
