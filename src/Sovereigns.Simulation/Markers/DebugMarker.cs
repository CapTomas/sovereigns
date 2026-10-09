using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Markers;

/// <summary>
/// Developer annotation pinned to a world position. It is authoritative world state so that
/// replays and failure reports reproduce what the developer marked.
/// </summary>
public sealed record DebugMarker(MarkerId Id, WorldPoint Position, string Label, SimTime PlacedAt);
