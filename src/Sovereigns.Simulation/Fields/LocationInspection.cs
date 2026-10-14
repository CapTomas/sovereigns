using System.Text.Json.Serialization;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Markers;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Fields;

/// <summary>
/// One quantity at a location, as a number in its SI <see cref="Unit"/> (ADR-0004 §1); clients format it for display.
/// <see cref="Value"/> is null exactly when the quantity is unavailable, and <see cref="UnavailableReason"/> then
/// says which owner and phase will provide it.
/// </summary>
public sealed record FieldReading(string Quantity, double? Value, string? Unit, string? UnavailableReason = null)
{
    [JsonIgnore]
    public bool IsAvailable => Value is not null;

    public static FieldReading Unavailable(string quantity, string reason) => new(quantity, null, null, reason);
}

public sealed record FamilyInspection(FieldFamily Family, FieldAvailability Availability, IReadOnlyList<FieldReading> Readings);

/// <summary>Everything the simulation can say about one world position at one time.</summary>
public sealed record LocationInspection(
    WorldPoint Point,
    bool InsideWorld,
    SimTime At,
    ulong WorldSeed,
    WorldExtent WorldExtent,
    IReadOnlyList<FamilyInspection> Families,
    IReadOnlyList<DebugMarker> NearbyMarkers);
