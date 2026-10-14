using System.Globalization;
using System.Text.Json.Serialization;

namespace Sovereigns.Simulation.Geometry;

/// <summary>
/// Horizontal position in the world frame (ADR-0004 §2): metres, +x east, +y north, origin at the
/// world's south-west corner. Clients convert to screen space at their own boundary.
/// </summary>
public readonly record struct WorldPoint(double X, double Y)
{
    [JsonIgnore]
    public bool IsFinite => double.IsFinite(X) && double.IsFinite(Y);

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"({X:0.###} m E, {Y:0.###} m N)");
}
