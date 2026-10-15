using System.Text.Json.Serialization;
using Sovereigns.Simulation.Ids;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Events;

/// <summary>Kinds of world event. Persisted by name; append new kinds, never rename or reuse one.</summary>
public enum WorldEventType
{
    WorldCreated,
    MarkerPlaced,
    MarkerRemoved,
    BreakpointSet,
    DecisionRaised,
    DecisionAcknowledged,
    SeasonBegan,
    YearBegan,
    FieldPainted,
    FeatureDrawn,
    FeatureRemoved,
}

public enum CauseKind
{
    /// <summary>World creation or loading; no prior cause inside the world.</summary>
    Origin,

    /// <summary>A command from a host, identified by its journal sequence.</summary>
    Command,

    /// <summary>A scheduled system update.</summary>
    System,

    /// <summary>An earlier world event, such as a scheduled breakpoint coming due.</summary>
    Event,
}

/// <summary>Why an event happened: the command, system or earlier event that caused it.</summary>
public sealed record EventCause(
    CauseKind Kind,
    long? CommandSequence = null,
    string? System = null,
    EntityId<Kind.Event>? ParentEvent = null)
{
    public static EventCause Origin { get; } = new(CauseKind.Origin);

    public static EventCause FromCommand(long sequence) => new(CauseKind.Command, CommandSequence: sequence);

    public static EventCause FromSystem(string system) => new(CauseKind.System, System: system);

    public static EventCause FromEvent(EntityId<Kind.Event> parent) => new(CauseKind.Event, ParentEvent: parent);

    public override string ToString() => Kind switch
    {
        CauseKind.Command => $"command #{CommandSequence}",
        CauseKind.System => $"system {System}",
        CauseKind.Event => $"{ParentEvent}",
        _ => "origin",
    };
}

/// <summary>
/// One entry of the ordered world-event ledger (SOV-P02-T07): identity, simulation time, type, cause and the
/// typed IDs of the state it affected (<see cref="Subjects"/>, for example <c>marker:3</c>).
/// </summary>
public sealed record WorldEvent(
    EntityId<Kind.Event> Id,
    SimTime At,
    WorldEventType Type,
    EventCause Cause,
    IReadOnlyList<string> Subjects,
    IReadOnlyDictionary<string, string>? Data = null)
{
    [JsonIgnore]
    public string Summary => Data is { Count: > 0 } data
        ? $"{Type} [{string.Join(", ", Subjects)}] {{{string.Join(", ", data.Select(pair => $"{pair.Key}={pair.Value}"))}}}"
        : $"{Type} [{string.Join(", ", Subjects)}]";
}
