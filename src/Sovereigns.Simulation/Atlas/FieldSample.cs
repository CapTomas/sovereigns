using Sovereigns.Simulation.Fields;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Atlas;

/// <summary>One grid value that contributed to a sample, with its interpolation weight.</summary>
/// <param name="Stored">True when the value is an authoritative override rather than base or derived data.</param>
public sealed record GridInput(GridCell Cell, double Weight, IReadOnlyList<double> Values, bool Stored);

/// <summary>
/// A field value at a point and level (SOV-P03-T04, T13): the value (one component for scalars and categories,
/// east and north for vectors), how it was obtained, the grid inputs and weights behind it, and when its inputs last
/// changed. <see cref="Values"/> is null exactly when the field has no value there.
/// </summary>
public sealed record FieldSample(
    FieldDefinition Field,
    WorldPoint Point,
    GridLevel Level,
    IReadOnlyList<double>? Values,
    ReadingBasis Basis,
    string Classification,
    IReadOnlyList<GridInput> Inputs,
    SimTime? ChangedAt,
    string? UnavailableReason = null)
{
    /// <summary>The scalar value, or a vector's magnitude.</summary>
    public double? Value => Values switch
    {
        null => null,
        [var single] => single,
        [var east, var north] => Math.Sqrt(east * east + north * north),
        _ => null,
    };

    public static FieldSample Unavailable(FieldDefinition field, WorldPoint point, GridLevel level, string reason) =>
        new(field, point, level, null, ReadingBasis.Unavailable, "unavailable", [], null, reason);
}
