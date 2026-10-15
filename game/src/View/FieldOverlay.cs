using System.Diagnostics;
using Sovereigns.Simulation.Atlas;
using Sovereigns.Simulation.Geometry;

namespace Sovereigns.Client.View;

/// <summary>One sampled vector: where it was sampled and its east and north components in the field's unit.</summary>
public readonly record struct OverlayArrow(WorldPoint Origin, double East, double North)
{
    public double Magnitude => Math.Sqrt(East * East + North * North);
}

/// <summary>
/// One field sampled over a rectangle of the world for display (SOV-P03-T14). Every value comes from
/// <see cref="FieldAtlas.SampleArea"/>, so the picture can only show what the atlas answers at the chosen level and
/// never invents or smooths values of its own. The sample grid is aligned to multiples of the spacing, so panning
/// at a fixed zoom does not shimmer. NaN means no data.
/// </summary>
public sealed class FieldOverlay
{
    /// <summary>Upper bound on samples per overlay; a larger area gets a coarser spacing instead.</summary>
    public const int MaxSamples = 80_000;

    private readonly double[] _values;

    private FieldOverlay(FieldDefinition field, GridLevel level, double west, double south, double spacingM, int columns, int rows, double[] values, double sampleMs)
    {
        SampleMilliseconds = sampleMs;
        Field = field;
        Level = level;
        West = west;
        South = south;
        SpacingM = spacingM;
        Columns = columns;
        Rows = rows;
        _values = values;

        var components = IsVector ? 2 : 1;
        double min = double.PositiveInfinity, max = double.NegativeInfinity;
        for (var i = 0; i < columns * rows; i++)
        {
            var scalar = IsVector ? Math.Sqrt((values[i * 2] * values[i * 2]) + (values[(i * 2) + 1] * values[(i * 2) + 1])) : values[i * components];
            if (double.IsNaN(scalar))
            {
                NoDataCount++;
                continue;
            }

            min = Math.Min(min, scalar);
            max = Math.Max(max, scalar);
        }

        HasData = max >= min;
        DataMin = HasData ? min : double.NaN;
        DataMax = HasData ? max : double.NaN;
        if (IsVector)
        {
            (RangeMin, RangeMax, UsesDataRange) = (0.0, HasData ? NiceCeiling(max) : 0.0, false);
            ArrowReference = RangeMax;
        }
        else
        {
            (RangeMin, RangeMax, UsesDataRange) = HasData ? FieldRamp.ChooseRange(field, min, max) : (field.Min, field.Max, false);
        }
    }

    public FieldDefinition Field { get; }

    public GridLevel Level { get; }

    /// <summary>West and south edges of the sampled rectangle, in metres.</summary>
    public double West { get; }

    public double South { get; }

    public double SpacingM { get; }

    public int Columns { get; }

    public int Rows { get; }

    public double East => West + (Columns * SpacingM);

    public double North => South + (Rows * SpacingM);

    public bool IsVector => Field.Kind == FieldKind.Vector;

    /// <summary>Samples with no value (outside the world, or the field has none there).</summary>
    public int NoDataCount { get; }

    public bool HasData { get; }

    /// <summary>Smallest and largest sampled value; for vectors, of the magnitude. NaN without data.</summary>
    public double DataMin { get; }

    public double DataMax { get; }

    /// <summary>The values the grey ramp spans: the field's declared range, or the data range when the data is far narrower.</summary>
    public double RangeMin { get; }

    public double RangeMax { get; }

    public bool UsesDataRange { get; }

    /// <summary>For vectors, the magnitude one full-length arrow stands for: the data maximum rounded up to 1, 2 or 5 times a power of ten; 0 when flat.</summary>
    public double ArrowReference { get; }

    public int SampleCount => Columns * Rows;

    /// <summary>Wall-clock cost of the atlas query that produced the values, for the HUD.</summary>
    public double SampleMilliseconds { get; }

    /// <summary>The sampled values, components interleaved, rows from the south, exactly as the atlas returned them.</summary>
    public IReadOnlyList<double> Values => _values;

    /// <summary>
    /// Samples <paramref name="area"/> (west, south, east, north in metres) at <paramref name="level"/> every
    /// <paramref name="spacingM"/> metres, rounding the grid outward so the whole area is covered.
    /// </summary>
    public static FieldOverlay Sample(FieldAtlas atlas, FieldDefinition field, GridLevel level, (double MinX, double MinY, double MaxX, double MaxY) area, double spacingM)
    {
        ArgumentNullException.ThrowIfNull(atlas);
        ArgumentNullException.ThrowIfNull(field);
        if (!(spacingM > 0) || !double.IsFinite(spacingM))
        {
            throw new ArgumentOutOfRangeException(nameof(spacingM), "sample spacing must be positive");
        }

        var (columns, rows) = Dimensions(area, spacingM);
        if ((long)columns * rows > MaxSamples)
        {
            spacingM *= Math.Sqrt((double)columns * rows / MaxSamples);
            (columns, rows) = Dimensions(area, spacingM);
        }

        var west = Math.Floor(area.MinX / spacingM) * spacingM;
        var south = Math.Floor(area.MinY / spacingM) * spacingM;
        var started = Stopwatch.GetTimestamp();
        var values = atlas.SampleArea(field.Key, level, west, south, spacingM, columns, rows);
        return new FieldOverlay(field, level, west, south, spacingM, columns, rows, values, Stopwatch.GetElapsedTime(started).TotalMilliseconds);

        (int Columns, int Rows) Dimensions((double MinX, double MinY, double MaxX, double MaxY) rectangle, double spacing)
        {
            var w = Math.Floor(rectangle.MinX / spacing) * spacing;
            var s = Math.Floor(rectangle.MinY / spacing) * spacing;
            return (Math.Max(1, (int)Math.Ceiling((rectangle.MaxX - w) / spacing)), Math.Max(1, (int)Math.Ceiling((rectangle.MaxY - s) / spacing)));
        }
    }

    /// <summary>The scalar value of the sample at a column and row (row 0 is the southernmost), NaN for no data. Vectors report their magnitude.</summary>
    public double ScalarAt(int column, int row)
    {
        var index = (row * Columns) + column;
        return IsVector ? Math.Sqrt((_values[index * 2] * _values[index * 2]) + (_values[(index * 2) + 1] * _values[(index * 2) + 1])) : _values[index];
    }

    /// <summary>A greyscale RGBA image, one pixel per sample, north at the top. No data is transparent.</summary>
    public Image ToImage()
    {
        if (IsVector)
        {
            throw new InvalidOperationException($"{Field.Key} is a vector field; draw its arrows instead");
        }

        var bytes = new byte[Columns * Rows * 4];
        for (var row = 0; row < Rows; row++)
        {
            var imageRow = Rows - 1 - row;
            for (var column = 0; column < Columns; column++)
            {
                var (r, g, b, a) = FieldRamp.Map(_values[(row * Columns) + column], RangeMin, RangeMax);
                var offset = ((imageRow * Columns) + column) * 4;
                bytes[offset] = r;
                bytes[offset + 1] = g;
                bytes[offset + 2] = b;
                bytes[offset + 3] = a;
            }
        }

        return Image.CreateFromData(Columns, Rows, false, Image.Format.Rgba8, bytes);
    }

    /// <summary>The vectors with a nonzero magnitude, each at the centre of its sample.</summary>
    public IReadOnlyList<OverlayArrow> Arrows()
    {
        var arrows = new List<OverlayArrow>();
        if (!IsVector)
        {
            return arrows;
        }

        for (var row = 0; row < Rows; row++)
        {
            for (var column = 0; column < Columns; column++)
            {
                var index = ((row * Columns) + column) * 2;
                var (east, north) = (_values[index], _values[index + 1]);
                if (double.IsNaN(east) || double.IsNaN(north) || (east == 0 && north == 0))
                {
                    continue;
                }

                arrows.Add(new OverlayArrow(new WorldPoint(West + ((column + 0.5) * SpacingM), South + ((row + 0.5) * SpacingM)), east, north));
            }
        }

        return arrows;
    }

    /// <summary>The smallest 1, 2 or 5 times a power of ten that is at least <paramref name="value"/>.</summary>
    public static double NiceCeiling(double value)
    {
        if (!(value > 0) || !double.IsFinite(value))
        {
            return 0;
        }

        var exponent = Math.Floor(Math.Log10(value));
        foreach (var mantissa in new[] { 1.0, 2.0, 5.0, 10.0 })
        {
            var candidate = mantissa * Math.Pow(10, exponent);
            if (candidate >= value * (1 - 1e-12))
            {
                return candidate;
            }
        }

        return 10 * Math.Pow(10, exponent);
    }
}
