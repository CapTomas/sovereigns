using System.Text.Json.Serialization;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Ids;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Commands;

/// <summary>
/// An explicit request from a host. <see cref="World.Submit"/> applies a world command immediately, at
/// <see cref="World.Now"/> and before the updates due at that time, and records it in the journal.
/// <see cref="ControlCommand"/>s go through <see cref="World.Control"/>: they only change how hosts pace time and
/// are not journaled. A fixture script that hosts play back may contain both; a journal replayed with
/// <see cref="Replay"/> contains world commands only.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(PlaceMarker), "place_marker")]
[JsonDerivedType(typeof(RemoveMarker), "remove_marker")]
[JsonDerivedType(typeof(SetBreakpoint), "set_breakpoint")]
[JsonDerivedType(typeof(AcknowledgeDecision), "acknowledge_decision")]
[JsonDerivedType(typeof(PaintScratch), "paint_scratch")]
[JsonDerivedType(typeof(DrawSurveyLine), "draw_survey_line")]
[JsonDerivedType(typeof(RemoveSurveyLine), "remove_survey_line")]
[JsonDerivedType(typeof(Pause), "pause")]
[JsonDerivedType(typeof(Resume), "resume")]
[JsonDerivedType(typeof(SetSpeed), "set_speed")]
[JsonDerivedType(typeof(StepOnce), "step_once")]
public abstract record SimCommand;

/// <summary>Pin a labelled developer marker at a position inside the world.</summary>
public sealed record PlaceMarker(WorldPoint Position, string Label) : SimCommand
{
    public const int MaxLabelLength = 64;
}

/// <summary>Remove an existing developer marker.</summary>
public sealed record RemoveMarker(EntityId<Kind.Marker> Marker) : SimCommand;

/// <summary>Schedule a decision interrupt at a future step boundary; time stops there until it is acknowledged.</summary>
public sealed record SetBreakpoint(SimTime At, string Label) : SimCommand;

/// <summary>Resolve a pending decision so that time may advance again.</summary>
public sealed record AcknowledgeDecision(EntityId<Kind.Event> Decision) : SimCommand;

/// <summary>Set the developer scratch field (0–1) in every local cell whose centre lies within the radius.</summary>
public sealed record PaintScratch(WorldPoint Centre, double RadiusM, double Value) : SimCommand
{
    public const double MaxRadiusM = 5_000;
}

/// <summary>Draw a developer survey line: 2 to 256 points inside the world.</summary>
public sealed record DrawSurveyLine(IReadOnlyList<WorldPoint> Points, string Label) : SimCommand
{
    public const int MaxPoints = 256;

    public bool Equals(DrawSurveyLine? other) =>
        other is not null && Label == other.Label && Points.SequenceEqual(other.Points);

    public override int GetHashCode() => HashCode.Combine(Label, Points.Count);
}

public sealed record RemoveSurveyLine(EntityId<Kind.SurveyLine> Line) : SimCommand;

/// <summary>Commands that change only host pacing (<see cref="Kernel.TimeControl"/>), never world state; see <see cref="World.Control"/>.</summary>
public abstract record ControlCommand : SimCommand;

public sealed record Pause : ControlCommand;

public sealed record Resume : ControlCommand;

/// <summary>Set the pace in simulated milliseconds per real second (1000 is real time).</summary>
public sealed record SetSpeed(long SimMillisecondsPerRealSecond) : ControlCommand;

/// <summary>Request one fixed step, also while paused.</summary>
public sealed record StepOnce : ControlCommand;
