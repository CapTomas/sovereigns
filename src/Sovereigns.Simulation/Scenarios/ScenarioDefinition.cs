using System.Text.Json.Serialization;
using Sovereigns.Simulation.Content;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Identity;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Scenarios;

/// <summary>Where a scenario world lies on the globe; degrees only in authored data (ADR-0004 §1).</summary>
/// <param name="SouthEdgeLatitudeDeg">Latitude at y = 0, positive north.</param>
/// <param name="StandardMeridianEastM">x of the meridian whose mean solar time is world standard time.</param>
public sealed record GeographyDefinition(double SouthEdgeLatitudeDeg, double StandardMeridianEastM);

/// <summary>A calendar position in authored data (ADR-0006).</summary>
public sealed record CalendarStart(long Year, int Month, int Day, int Hour = 0, int Minute = 0);

/// <summary>
/// Setup for a world run, selected by name together with a seed (SOV-P01-T08). Until world generation
/// exists, the scenario declares the world extent and geographic placement; it creates no terrain or other
/// physical fields.
/// </summary>
/// <param name="ExtentM">World size in metres, east by north.</param>
/// <param name="StepMs">Fixed simulation step in whole milliseconds (ADR-0004 §4).</param>
/// <param name="DurationMs">Default headless run length; a whole number of steps.</param>
public sealed record ScenarioDefinition(
    ContentId Id,
    string Description,
    WorldExtent ExtentM,
    GeographyDefinition Geography,
    CalendarStart Start,
    long StepMs,
    long DurationMs)
    : IContentDefinition
{
    [JsonIgnore]
    public GeographicReference GeographicReference =>
        GeographicReference.FromDegrees(Geography.SouthEdgeLatitudeDeg, Geography.StandardMeridianEastM);

    [JsonIgnore]
    public SimTime StartTime => WorldCalendar.FromCalendar(Start.Year, Start.Month, Start.Day, Start.Hour, Start.Minute);

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
        else
        {
            foreach (var problem in GeographicReference.Validate(ExtentM))
            {
                yield return new("$.geography", problem);
            }
        }

        string? startProblem = null;
        try
        {
            _ = StartTime;
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or OverflowException)
        {
            startProblem = ex.Message;
        }

        if (startProblem is not null)
        {
            yield return new("$.start", $"not a calendar position: {startProblem}");
        }

        if (StepMs <= 0)
        {
            yield return new("$.step_ms", $"step must be at least 1 ms, found {StepMs}");
        }
        else
        {
            if (DurationMs < 0 || DurationMs % StepMs != 0)
            {
                yield return new("$.duration_ms", $"duration must be a non-negative whole number of {StepMs} ms steps, found {DurationMs}");
            }

            if (startProblem is null && StartTime.Milliseconds % StepMs != 0)
            {
                yield return new("$.start", $"start must fall on the {StepMs} ms step grid");
            }

            if (WorldCalendar.MillisecondsPerDay % StepMs != 0)
            {
                yield return new("$.step_ms", "step must divide a day evenly, so that daily systems fall on the step grid");
            }
        }
    }

    public IEnumerable<ContentReference> References() => [];
}
