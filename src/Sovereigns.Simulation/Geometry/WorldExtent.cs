using System.Text.Json.Serialization;

namespace Sovereigns.Simulation.Geometry;

/// <summary>
/// Size of the bounded world frame in metres. In-world positions satisfy 0 &lt;= x &lt; East and 0 &lt;= y &lt; North.
/// World generation will own the extent; until then a scenario declares it.
/// </summary>
public readonly record struct WorldExtent(double East, double North)
{
    [JsonIgnore]
    public bool IsValid => double.IsFinite(East) && double.IsFinite(North) && East > 0 && North > 0;

    public bool Contains(WorldPoint point) =>
        point.X >= 0 && point.Y >= 0 && point.X < East && point.Y < North;
}
