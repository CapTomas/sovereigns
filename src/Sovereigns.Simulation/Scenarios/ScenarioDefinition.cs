using Sovereigns.Simulation.Content;
using Sovereigns.Simulation.Geometry;

namespace Sovereigns.Simulation.Scenarios;

/// <summary>
/// Setup for a world run, selected by name together with a seed (SOV-P01-T08). Until world generation
/// exists, the scenario declares the world extent; it creates no terrain or other physical fields.
/// </summary>
/// <param name="ExtentM">World size in metres, east by north.</param>
/// <param name="StepMs">Fixed simulation step in whole milliseconds (ADR-0004 §4).</param>
/// <param name="DurationMs">Default headless run length; a whole number of steps.</param>
public sealed record ScenarioDefinition(ContentId Id, string Description, WorldExtent ExtentM, long StepMs, long DurationMs)
    : IContentDefinition
{
    public IEnumerable<ContentIssue> Validate()
    {
        if (string.IsNullOrWhiteSpace(Description))
        {
            yield return new("$.description", "description must say what the scenario is for");
        }

        if (!ExtentM.IsValid)
        {
            yield return new("$.extent_m", "east and north must be finite and greater than 0 metres");
        }

        if (StepMs <= 0)
        {
            yield return new("$.step_ms", $"step must be at least 1 ms, found {StepMs}");
        }
        else if (DurationMs < 0 || DurationMs % StepMs != 0)
        {
            yield return new("$.duration_ms", $"duration must be a non-negative whole number of {StepMs} ms steps, found {DurationMs}");
        }
    }

    public IEnumerable<ContentReference> References() => [];
}
