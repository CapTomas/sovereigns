using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Sovereigns.Simulation.Atlas;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Events;
using Sovereigns.Simulation.Identity;
using Sovereigns.Simulation.Ids;
using Sovereigns.Simulation.Kernel;
using Sovereigns.Simulation.Markers;
using Sovereigns.Simulation.Serialization;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Persistence;

/// <summary>One authoritative override of a native cell.</summary>
public sealed record SavedOverride(long I, long J, IReadOnlyList<double> Values, SimTime At);

/// <summary>A stored field's overrides at its native level, with the field's schema version (ADR-0009).</summary>
public sealed record SavedField(string Field, int Schema, GridLevel Level, IReadOnlyList<SavedOverride> Overrides);

/// <summary>Mutable authoritative state at the save time. Derived values, caches and host pacing are never saved.</summary>
public sealed record SaveState(
    SimTime Now,
    long CommandsApplied,
    IReadOnlyDictionary<string, ulong> IdCounters,
    IReadOnlyList<DebugMarker> Markers,
    IReadOnlyList<Breakpoint> Breakpoints,
    IReadOnlyList<PendingDecision> PendingDecisions,
    IReadOnlyList<SavedField> Fields,
    IReadOnlyList<MapFeature> Features);

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
    public const int CurrentFormatVersion = 2;

    /// <summary>
    /// Declared migrations: the payload of format N becomes format N + 1. Each runs on the JSON after its integrity
    /// was verified, and the hash is recomputed afterwards.
    /// </summary>
    private static readonly Dictionary<int, Action<JsonObject>> Migrations = new()
    {
        // Format 2 (Phase 03) adds stored field overrides and linear features; a format 1 world had neither.
        [1] = payload =>
        {
            var state = payload["state"]!.AsObject();
            state["fields"] = new JsonArray();
            state["features"] = new JsonArray();
        },
    };

    /// <summary>Captures a world between operations. A faulted world has partial state and cannot be saved.</summary>
    public static SaveFile Capture(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (world.Fault != FaultStage.None)
        {
            throw new InvalidOperationException("a faulted world has partial state and cannot be saved; write a failure report instead");
        }

        var fields = world.Atlas.AllOverrides().GroupBy(entry => entry.Field).Select(group =>
        {
            var definition = FieldCatalog.Get(group.Key);
            return new SavedField(group.Key, definition.Schema, definition.NativeLevel,
                [.. group.Select(entry => new SavedOverride(entry.Cell.I, entry.Cell.J, entry.Override.Values, entry.Override.At))]);
        }).ToList();
        var state = new SaveState(world.Now, world.Journal.NextSequence - 1, new SortedDictionary<string, ulong>(
            world.Ids.Counters.ToDictionary(), StringComparer.Ordinal), [.. world.Markers], [.. world.Breakpoints], [.. world.PendingDecisions],
            fields, [.. world.Features]);
        var payload = new SavePayload(world.Identity, state, [.. world.Ledger.Events]);
        return new SaveFile(FormatName, CurrentFormatVersion, world.Identity.RulesVersion, WorldCalendar.ToCalendar(world.Now).ToString(),
            Hash(payload), payload);
    }

    public void WriteTo(Stream stream) => JsonSerializer.Serialize(stream, this, SimJson.Indented);

    /// <summary>
    /// Reads, verifies, migrates and restores a world. Integrity is checked on the payload as written, before any
    /// migration, so an older save is proven intact before it is converted. Throws <see cref="SaveLoadException"/>
    /// listing every problem.
    /// </summary>
    public static World Load(Stream stream, Logger? logger = null)
    {
        JsonObject root;
        try
        {
            root = JsonNode.Parse(stream)?.AsObject() ?? throw new JsonException("empty document");
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            throw new SaveLoadException([$"not a readable save file: {ex.Message}"]);
        }

        var header = Header(root).ToList();
        if (header.Count > 0)
        {
            throw new SaveLoadException(header);
        }

        for (var version = root["format_version"]!.GetValue<int>(); version < CurrentFormatVersion; version++)
        {
            var payload = root["payload"]!.AsObject();
            Migrations[version](payload);
            root["format_version"] = version + 1;
            root["payload_sha256"] = Hash(payload);
        }

        SaveFile? file;
        try
        {
            file = root.Deserialize<SaveFile>(SimJson.Options);
        }
        catch (JsonException ex)
        {
            throw new SaveLoadException([$"not a readable save file: {ex.Message}"]);
        }

        if (file is null)
        {
            throw new SaveLoadException(["the save file is empty"]);
        }

        var problems = file.Validate(checkHash: false).ToList();
        if (problems.Count > 0)
        {
            throw new SaveLoadException(problems);
        }

        var state = file.Payload.State;
        var overrides = state.Fields.SelectMany(field => field.Overrides.Select(cell =>
            (field.Field, new GridCell(field.Level, cell.I, cell.J), new CellOverride(cell.Values, cell.At))));
        try
        {
            return World.Restore(file.Payload.Identity, state.Now, state.CommandsApplied, state.IdCounters, state.Markers,
                state.Breakpoints, state.PendingDecisions, overrides, state.Features, file.Payload.Ledger, file.PayloadSha256, logger);
        }
        catch (InvalidOperationException ex)
        {
            throw new SaveLoadException([ex.Message]);
        }
    }

    /// <summary>Format, version and integrity of a save as written; empty when it may be migrated and read.</summary>
    private static IEnumerable<string> Header(JsonObject root)
    {
        if (root["format"]?.GetValueKind() != JsonValueKind.String || root["format"]!.GetValue<string>() != FormatName)
        {
            yield return $"format is not '{FormatName}'";
            yield break;
        }

        if (root["format_version"]?.GetValueKind() != JsonValueKind.Number || !root["format_version"]!.AsValue().TryGetValue<int>(out var version))
        {
            yield return "format_version is missing";
            yield break;
        }

        if (version > CurrentFormatVersion)
        {
            yield return $"save format {version} is newer than this build (format {CurrentFormatVersion}); use a newer build";
            yield break;
        }

        for (var step = version; step < CurrentFormatVersion; step++)
        {
            if (!Migrations.ContainsKey(step))
            {
                yield return $"save format {version} has no declared migration to format {CurrentFormatVersion}";
                yield break;
            }
        }

        if (root["payload"] is not JsonObject payload || root["payload_sha256"]?.GetValueKind() != JsonValueKind.String ||
            Hash(payload) != root["payload_sha256"]!.GetValue<string>())
        {
            yield return "the payload does not match its SHA-256; the save is corrupted or was edited";
        }
    }

    /// <summary>Every reason this save cannot be loaded by this build; empty when it can.</summary>
    public IEnumerable<string> Validate(bool checkHash = true)
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

        if (checkHash && Hash(Payload) != PayloadSha256)
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

        var fieldKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in state.Fields)
        {
            if (!FieldAtlas.StoredGridFields.Contains(field.Field) || !fieldKeys.Add(field.Field))
            {
                yield return $"state: field '{field.Field}' is not stored by this build, or appears twice";
                continue;
            }

            var definition = FieldCatalog.Get(field.Field);
            if (field.Schema != definition.Schema || field.Level != definition.NativeLevel)
            {
                yield return $"state: field '{field.Field}' was saved with schema {field.Schema} at {field.Level}; this build needs schema {definition.Schema} at {definition.NativeLevel} and declares no migration";
                continue;
            }

            var (_, _, maxI, maxJ) = Grid.CellRange(field.Level, identity.Extent);
            var components = definition.Kind == FieldKind.Vector ? 2 : 1;
            var cells = new HashSet<(long, long)>();
            foreach (var cell in field.Overrides)
            {
                if (cell.I < 0 || cell.J < 0 || cell.I > maxI || cell.J > maxJ || !cells.Add((cell.I, cell.J)))
                {
                    yield return $"state: field '{field.Field}' override at [{cell.I},{cell.J}] lies outside the world or appears twice";
                }

                if (cell.Values.Count != components || cell.Values.Any(value => !double.IsFinite(value) || !definition.InRange(value)) ||
                    cell.At > state.Now || cell.At < identity.Start)
                {
                    yield return $"state: field '{field.Field}' override at [{cell.I},{cell.J}] has values outside {definition.Min}–{definition.Max} {definition.Unit} or a time outside the world's history";
                }
            }
        }

        var featureIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var feature in state.Features)
        {
            var parts = feature.Id.Split(':');
            if (parts.Length != 2 || parts[0] != Kind.SurveyLine.Name ||
                !ulong.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var number) ||
                feature.Id != new EntityId<Kind.SurveyLine>(number).ToString() || number == 0 ||
                number > Issued(Kind.SurveyLine.Name) || !featureIds.Add(feature.Id) || feature.Kind != FeatureKind.SurveyLine)
            {
                yield return $"state: feature '{feature.Id}' was never issued, appears twice, or is not a survey line";
            }

            if (World.SurveyLineProblem(feature.Points, feature.Label, identity.Extent) is { } problem)
            {
                yield return $"state: feature '{feature.Id}': {problem}";
            }

            if (feature.CreatedAt > state.Now || feature.CreatedAt < identity.Start)
            {
                yield return $"state: feature '{feature.Id}' was created at {feature.CreatedAt}, outside the world's history";
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

    /// <summary>SHA-256 of the canonical (compact) payload JSON.</summary>
    private static string Hash(SavePayload payload) =>
        Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(payload, SimJson.Options)));

    /// <summary>
    /// The same hash computed from a parsed payload, so that a save can be verified before it is migrated. Compact JSON
    /// of a parsed document reproduces the serializer's bytes: member order, escaping and number text are preserved.
    /// </summary>
    private static string Hash(JsonObject payload) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payload.ToJsonString(SimJson.Options))));
}
