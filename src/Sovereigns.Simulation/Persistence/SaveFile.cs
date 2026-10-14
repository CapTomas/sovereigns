using System.Security.Cryptography;
using System.Text.Json;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Events;
using Sovereigns.Simulation.Identity;
using Sovereigns.Simulation.Ids;
using Sovereigns.Simulation.Kernel;
using Sovereigns.Simulation.Markers;
using Sovereigns.Simulation.Serialization;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Persistence;

/// <summary>Mutable authoritative state at the save time. Derived values, caches and host pacing are never saved.</summary>
public sealed record SaveState(
    SimTime Now,
    long CommandsApplied,
    IReadOnlyDictionary<string, ulong> IdCounters,
    IReadOnlyList<DebugMarker> Markers,
    IReadOnlyList<Breakpoint> Breakpoints,
    IReadOnlyList<PendingDecision> PendingDecisions);

/// <summary>Everything a save restores: immutable identity, mutable state and the event ledger (delta history).</summary>
public sealed record SavePayload(WorldIdentity Identity, SaveState State, IReadOnlyList<WorldEvent> Ledger);

/// <summary>Raised when a save cannot be loaded; <see cref="Problems"/> lists every reason found.</summary>
public sealed class SaveLoadException(IReadOnlyList<string> problems)
    : Exception("the save cannot be loaded: " + string.Join("; ", problems))
{
    public IReadOnlyList<string> Problems { get; } = problems;
}

/// <summary>
/// Versioned save file (SOV-P02-T12, T17; ADR-0008). The header names the format and its version and the rules
/// version that wrote it, and carries the SHA-256 of the canonical payload. Loading refuses a different format,
/// version or rules version unless a migration is declared, refuses a corrupted payload, and validates every
/// reference instead of inventing replacements. The client never writes save data directly.
/// </summary>
public sealed record SaveFile(string Format, int FormatVersion, int RulesVersion, string SavedAt, string PayloadSha256, SavePayload Payload)
{
    public const string FormatName = "sovereigns-save";
    public const int CurrentFormatVersion = 1;

    /// <summary>Captures a world between operations. A faulted world has partial state and cannot be saved.</summary>
    public static SaveFile Capture(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (world.Fault != FaultStage.None)
        {
            throw new InvalidOperationException("a faulted world has partial state and cannot be saved; write a failure report instead");
        }

        var state = new SaveState(world.Now, world.Journal.NextSequence - 1, new SortedDictionary<string, ulong>(
            world.Ids.Counters.ToDictionary(), StringComparer.Ordinal), [.. world.Markers], [.. world.Breakpoints], [.. world.PendingDecisions]);
        var payload = new SavePayload(world.Identity, state, [.. world.Ledger.Events]);
        return new SaveFile(FormatName, CurrentFormatVersion, world.Identity.RulesVersion, WorldCalendar.ToCalendar(world.Now).ToString(),
            Hash(payload), payload);
    }

    public void WriteTo(Stream stream) => JsonSerializer.Serialize(stream, this, SimJson.Indented);

    /// <summary>Reads, verifies and restores a world. Throws <see cref="SaveLoadException"/> listing every problem.</summary>
    public static World Load(Stream stream, Logger? logger = null)
    {
        SaveFile? file;
        try
        {
            file = JsonSerializer.Deserialize<SaveFile>(stream, SimJson.Options);
        }
        catch (JsonException ex)
        {
            throw new SaveLoadException([$"not a readable save file: {ex.Message}"]);
        }

        if (file is null)
        {
            throw new SaveLoadException(["the save file is empty"]);
        }

        var problems = file.Validate().ToList();
        if (problems.Count > 0)
        {
            throw new SaveLoadException(problems);
        }

        var state = file.Payload.State;
        try
        {
            return World.Restore(file.Payload.Identity, state.Now, state.CommandsApplied, state.IdCounters, state.Markers,
                state.Breakpoints, state.PendingDecisions, file.Payload.Ledger, file.PayloadSha256, logger);
        }
        catch (InvalidOperationException ex)
        {
            throw new SaveLoadException([ex.Message]);
        }
    }

    /// <summary>Every reason this save cannot be loaded by this build; empty when it can.</summary>
    public IEnumerable<string> Validate()
    {
        if (Format != FormatName)
        {
            yield return $"format '{Format}' is not '{FormatName}'";
            yield break;
        }

        if (FormatVersion != CurrentFormatVersion)
        {
            yield return FormatVersion > CurrentFormatVersion
                ? $"save format {FormatVersion} is newer than this build (format {CurrentFormatVersion}); use a newer build"
                : $"save format {FormatVersion} has no declared migration to format {CurrentFormatVersion}";
            yield break;
        }

        var identity = Payload.Identity;
        if (RulesVersion != SimulationRules.Version || identity.RulesVersion != RulesVersion)
        {
            yield return $"saved with rules version {RulesVersion} (identity {identity.RulesVersion}); this build runs version {SimulationRules.Version} and declares no migration";
            yield break;
        }

        if (Hash(Payload) != PayloadSha256)
        {
            yield return "the payload does not match its SHA-256; the save is corrupted or was edited";
            yield break;
        }

        if (!SimulationRules.RegenerableGeneratorVersions.Contains(identity.GeneratorVersion))
        {
            yield return $"made by world generator version {identity.GeneratorVersion}; this build regenerates only versions " +
                $"{string.Join(", ", SimulationRules.RegenerableGeneratorVersions)} and declares no migration, so unsaved base state would change";
            yield break;
        }

        foreach (var problem in ValidateReferences())
        {
            yield return problem;
        }
    }

    private IEnumerable<string> ValidateReferences()
    {
        var (identity, state, ledger) = (Payload.Identity, Payload.State, Payload.Ledger);
        if (!identity.Extent.IsValid)
        {
            yield return "identity: world extent is invalid";
        }

        foreach (var problem in identity.Geography.Validate(identity.Extent))
        {
            yield return "identity: " + problem;
        }

        if (identity.StepMs <= 0 || WorldCalendar.MillisecondsPerDay % identity.StepMs != 0 || identity.Start.Milliseconds % identity.StepMs != 0)
        {
            yield return "identity: step must be positive, divide a day, and the start must lie on its grid";
            yield break;
        }

        if (state.Now < identity.Start || (state.Now.Milliseconds - identity.Start.Milliseconds) % identity.StepMs != 0)
        {
            yield return $"state: time {state.Now} is before the start or off the step grid";
        }

        if (state.CommandsApplied < 0)
        {
            yield return "state: commands applied cannot be negative";
        }

        foreach (var (kind, last) in state.IdCounters)
        {
            if (!Kind.Names.Contains(kind) || last == 0)
            {
                yield return $"state: ID counter '{kind}' = {last} is not a known kind with issued IDs";
            }
        }

        ulong Issued(string kind) => state.IdCounters.GetValueOrDefault(kind);
        var events = new Dictionary<ulong, WorldEvent>();
        WorldEvent? previous = null;
        foreach (var worldEvent in ledger)
        {
            if (previous is not null && (worldEvent.At < previous.At || worldEvent.Id.Value <= previous.Id.Value))
            {
                yield return $"ledger: {worldEvent.Id} at {worldEvent.At} does not follow {previous.Id} at {previous.At}";
            }

            previous = worldEvent;
            if (worldEvent.Cause.CommandSequence is { } sequence && (sequence < 1 || sequence > state.CommandsApplied))
            {
                yield return $"ledger: {worldEvent.Id} names command #{sequence}, but {state.CommandsApplied} commands were applied";
            }

            if (worldEvent.Id.IsNone || worldEvent.Id.Value > Issued(Kind.Event.Name))
            {
                yield return $"ledger: {worldEvent.Id} was never issued";
            }

            if (!events.TryAdd(worldEvent.Id.Value, worldEvent))
            {
                yield return $"ledger: {worldEvent.Id} appears twice";
            }

            if (worldEvent.At > state.Now)
            {
                yield return $"ledger: {worldEvent.Id} at {worldEvent.At} is after the save time {state.Now}";
            }
        }

        var markerIds = new HashSet<ulong>();
        foreach (var marker in state.Markers)
        {
            if (marker.Id.IsNone || marker.Id.Value > Issued(Kind.Marker.Name) || !markerIds.Add(marker.Id.Value))
            {
                yield return $"state: {marker.Id} was never issued or appears twice";
            }

            if (!marker.Position.IsFinite || !identity.Extent.Contains(marker.Position))
            {
                yield return $"state: {marker.Id} lies outside the world";
            }
        }

        var breakpointIds = new HashSet<ulong>();
        foreach (var breakpoint in state.Breakpoints)
        {
            if (breakpoint.Id.IsNone || breakpoint.Id.Value > Issued(Kind.Breakpoint.Name) || !breakpointIds.Add(breakpoint.Id.Value))
            {
                yield return $"state: {breakpoint.Id} was never issued or appears twice";
            }

            if (breakpoint.At <= state.Now || (breakpoint.At.Milliseconds - identity.Start.Milliseconds) % identity.StepMs != 0)
            {
                yield return $"state: {breakpoint.Id} at {breakpoint.At} is not a future step boundary";
            }

            if (!events.TryGetValue(breakpoint.SetBy.Value, out var set) || set.Type != WorldEventType.BreakpointSet)
            {
                yield return $"state: {breakpoint.Id} refers to missing set event {breakpoint.SetBy}";
            }
        }

        var decisionEvents = new HashSet<ulong>();
        foreach (var decision in state.PendingDecisions)
        {
            if (!events.TryGetValue(decision.Event.Value, out var raised) || raised.Type != WorldEventType.DecisionRaised)
            {
                yield return $"state: pending decision refers to missing event {decision.Event}";
            }

            if (decision.At != state.Now || !decisionEvents.Add(decision.Event.Value))
            {
                yield return $"state: pending decision {decision.Event} must be raised at the save time and appear once";
            }
        }
    }

    private static string Hash(SavePayload payload) =>
        Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(payload, SimJson.Options)));
}
