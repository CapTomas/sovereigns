using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Ids;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Markers;

/// <summary>
/// Developer annotation pinned to a world position. It is authoritative world state so that
/// replays, saves and failure reports reproduce what the developer marked.
/// </summary>
public sealed record DebugMarker(EntityId<Kind.Marker> Id, WorldPoint Position, string Label, SimTime PlacedAt);
