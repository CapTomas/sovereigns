using System.Text.Json.Serialization;
using Sovereigns.Simulation.Atlas;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Markers;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Fields;

/// <summary>How a reading was obtained (spec §3.3, §3.10).</summary>
public enum ReadingBasis
{
    /// <summary>The query itself, such as the inspected coordinate.</summary>
    Query,

    /// <summary>Authoritative stored state.</summary>
    Stored,

    /// <summary>Computed on demand from authoritative inputs; never saved.</summary>
    Derived,

    /// <summary>An approximation with stated uncertainty.</summary>
    Estimated,

    /// <summary>Not available: its owner is not implemented yet, or the point is outside the world.</summary>
    Unavailable,
}

/// <summary>
/// One quantity at a location (SOV-P02-T13), as a number in its SI <see cref="Unit"/> (ADR-0004 §1), with its
/// provenance: the <see cref="Owner"/> that writes its inputs, the <see cref="Source"/> fields, the
/// <see cref="Derivation"/> formula, and when its inputs last changed. Clients format it for display.
/// <see cref="Value"/> is null exactly when <see cref="Basis"/> is <see cref="ReadingBasis.Unavailable"/>.
/// </summary>
public sealed record FieldReading(
    string Quantity,
    double? Value,
    string? Unit,
    ReadingBasis Basis,
    string? Owner = null,
    string? Source = null,
    string? Derivation = null,
    SimTime? UpdatedAt = null,
    string? UnavailableReason = null)
{
    [JsonIgnore]
    public bool IsAvailable => Value is not null;

    /// <summary>Grid level the value was sampled at, for atlas fields.</summary>
    public GridLevel? Level { get; init; }

    /// <summary>East and north components of a vector field; <see cref="Value"/> is then the magnitude.</summary>
    public IReadOnlyList<double>? Components { get; init; }

    /// <summary>The grid samples and weights behind the value (SOV-P03-T13).</summary>
    public IReadOnlyList<GridInput> Inputs { get; init; } = [];

    public static FieldReading Unavailable(string quantity, string reason) =>
        new(quantity, null, null, ReadingBasis.Unavailable, UnavailableReason: reason);
}

public sealed record FamilyInspection(FieldFamily Family, FieldAvailability Availability, IReadOnlyList<FieldReading> Readings);

/// <summary>
/// Everything the simulation can say about one world position at one time. It is an omniscient developer view;
/// faction observations are separate and never write back (ADR-0007).
/// </summary>
public sealed record LocationInspection(
    WorldPoint Point,
    bool InsideWorld,
    SimTime At,
    ulong WorldSeed,
    WorldExtent WorldExtent,
    IReadOnlyList<FamilyInspection> Families,
    IReadOnlyList<DebugMarker> NearbyMarkers,
    IReadOnlyList<FeatureHit> NearbyFeatures);
