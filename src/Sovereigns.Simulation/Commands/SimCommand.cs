using System.Text.Json.Serialization;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Markers;

namespace Sovereigns.Simulation.Commands;

/// <summary>
/// An explicit request to change authoritative state. Hosts submit commands; the world applies
/// them in submission order at the next step boundary and records them in its journal.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(PlaceMarker), "place_marker")]
[JsonDerivedType(typeof(RemoveMarker), "remove_marker")]
public abstract record SimCommand;

/// <summary>Pin a labelled developer marker at a position inside the world.</summary>
public sealed record PlaceMarker(WorldPoint Position, string Label) : SimCommand
{
    public const int MaxLabelLength = 64;
}

/// <summary>Remove an existing developer marker.</summary>
public sealed record RemoveMarker(MarkerId Marker) : SimCommand;
