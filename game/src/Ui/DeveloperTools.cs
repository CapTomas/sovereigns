using System.Globalization;
using Sovereigns.Client.View;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Geometry;

namespace Sovereigns.Client.Ui;

/// <summary>
/// The state of the developer tools that exercise the atlas through the shell (SOV-P03-T13, T14): the scratch-paint
/// brush and the survey line being drawn. It has no Godot types and no world access; it only builds the commands that
/// the shell submits, so the world journals every change. The brush and the pending points are host state, not world
/// state, and are never saved.
/// </summary>
public sealed class DeveloperTools
{
    /// <summary>Paint radii in metres, smallest first; the largest is the world's limit.</summary>
    public static IReadOnlyList<double> Radii { get; } = [10, 20, 50, 100, 200, 500, 1000, 2000, PaintScratch.MaxRadiusM];

    /// <summary>The paint value changes in steps of this size, between 0 and 1.</summary>
    public const double ValueStep = 0.1;

    private const int DefaultRadiusIndex = 5;

    private readonly List<WorldPoint> _pending = [];
    private int _radiusIndex = DefaultRadiusIndex;

    public double RadiusM => Radii[_radiusIndex];

    public double Value { get; private set; } = 1.0;

    /// <summary>Points of the survey line being drawn, in the order added.</summary>
    public IReadOnlyList<WorldPoint> Pending => _pending;

    /// <summary>Survey lines the world has accepted from this tool; labels run S1, S2, …</summary>
    public int LinesDrawn { get; private set; }

    public string NextLabel => string.Create(CultureInfo.InvariantCulture, $"S{LinesDrawn + 1}");

    /// <summary>Changes the radius to the next larger (+1) or smaller (-1) preset; false at either end.</summary>
    public bool StepRadius(int direction)
    {
        var next = _radiusIndex + direction;
        if (next < 0 || next >= Radii.Count)
        {
            return false;
        }

        _radiusIndex = next;
        return true;
    }

    /// <summary>Raises (+1) or lowers (-1) the paint value by one step; false at 0 or 1.</summary>
    public bool StepValue(int direction)
    {
        var next = Math.Round(Math.Clamp(Value + (direction * ValueStep), 0, 1), 1);
        if (next == Value)
        {
            return false;
        }

        Value = next;
        return true;
    }

    public PaintScratch Paint(WorldPoint centre) => new(centre, RadiusM, Value);

    /// <summary>Adds a point to the pending line unless it repeats the previous one, which the world would refuse.</summary>
    public bool AddPoint(WorldPoint point)
    {
        if (_pending.Count > 0 && _pending[^1] == point)
        {
            return false;
        }

        _pending.Add(point);
        return true;
    }

    /// <summary>The command that draws the pending line, or null while it has fewer than two points. The line stays pending until <see cref="Accepted"/>.</summary>
    public DrawSurveyLine? Finish() => _pending.Count < 2 ? null : new DrawSurveyLine([.. _pending], NextLabel);

    /// <summary>Records that the world accepted the finished line, and starts a new one.</summary>
    public void Accepted()
    {
        LinesDrawn++;
        _pending.Clear();
    }

    public void Cancel() => _pending.Clear();

    /// <summary>The tool state for the status bar: brush radius and value, and the pending survey line.</summary>
    public string Describe() => string.Create(CultureInfo.InvariantCulture,
        $"paint: radius {MapScale.FormatDistance(RadiusM)}, value {Value:0.00}   |   survey line: {(_pending.Count == 0 ? "none pending" : $"{_pending.Count} pending point(s), next label {NextLabel}")}");
}
