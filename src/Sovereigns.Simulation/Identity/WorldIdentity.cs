using Sovereigns.Simulation.Content;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Identity;

/// <summary>Version of the simulation rules. Increase it in the same change as any behavior change that alters outcomes.</summary>
public static class SimulationRules
{
    public const int Version = 1;

    /// <summary>Generator version of worlds set up directly by a scenario, before world generation exists (Phase 04).</summary>
    public const int NoGenerator = 0;

    /// <summary>
    /// Generator versions whose worlds this build can regenerate exactly (ADR-0008). A save from any other version
    /// would regenerate different unsaved base state, so it is refused unless a migration is declared.
    /// </summary>
    public static IReadOnlyList<int> RegenerableGeneratorVersions { get; } = [NoGenerator];
}

/// <summary>
/// Everything that identifies a world and never changes after creation (SOV-P02-T01, ADR-0006): identity, seed,
/// the rules and generator versions that produced it, and the coordinate contract (extent, geographic reference,
/// step and start time). Saves carry it unchanged.
/// </summary>
public sealed record WorldIdentity(
    WorldId Id,
    ulong Seed,
    ContentId Scenario,
    int RulesVersion,
    int GeneratorVersion,
    WorldExtent Extent,
    GeographicReference Geography,
    long StepMs,
    SimTime Start);
