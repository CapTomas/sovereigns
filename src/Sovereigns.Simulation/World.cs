using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Content;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Events;
using Sovereigns.Simulation.Fields;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Identity;
using Sovereigns.Simulation.Ids;
using Sovereigns.Simulation.Kernel;
using Sovereigns.Simulation.Markers;
using Sovereigns.Simulation.Scenarios;
using Sovereigns.Simulation.State;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation;

/// <summary>What a world was doing when an exception left its state partly updated.</summary>
public enum FaultStage
{
    None,
    Command,
    Step,
}

/// <summary>
/// One authoritative world (ADR-0006, ADR-0007). Its clock advances only through <see cref="AdvanceTo"/>, which
/// runs scheduled systems in time and priority order and stops at pending decisions; its state changes only
/// through those systems and through journaled commands. Rendering clocks and queries never change it.
/// </summary>
public sealed class World : ILogContext
{
    public const string KernelOwner = "kernel";
    public const string DeveloperOwner = "developer";

    private const int ChecksumFormat = 3;
    private const double MarkerCellM = 1_000;

    private readonly Scheduler _scheduler = new();
    private readonly List<DebugMarker> _markers = [];
    private readonly SortedSet<Breakpoint> _breakpoints = new(Comparer<Breakpoint>.Create((a, b) =>
        a.At != b.At ? a.At.CompareTo(b.At) : a.Id.Value.CompareTo(b.Id.Value)));
    private readonly List<PendingDecision> _decisions = [];
    private readonly StateVersion _markerVersion = new("markers");
    private readonly DerivedCache<ILookup<(long, long), DebugMarker>> _markerIndex;
    private readonly SubsystemLog _log;
    private SimCommand? _inFlight;

    private World(WorldIdentity identity, Logger logger)
    {
        Identity = identity;
        Now = identity.Start;
        _log = logger.For("world", this);
        foreach (var component in new[] { "clock", "ids", "ledger", "breakpoints", "decisions" })
        {
            Ownership.Register(component, KernelOwner);
        }

        Ownership.Register("markers", DeveloperOwner);
        _markerIndex = new DerivedCache<ILookup<(long, long), DebugMarker>>(
            () => _markers.ToLookup(marker => Cell(marker.Position)), _markerVersion);
        _scheduler.Register(new CalendarSystem(), identity.StepMs, Ownership);
    }

    public WorldIdentity Identity { get; }

    public ulong Seed => Identity.Seed;

    public ContentId ScenarioId => Identity.Scenario;

    public WorldExtent Extent => Identity.Extent;

    public long StepMs => Identity.StepMs;

    public SimTime Now { get; private set; }

    /// <summary>Whole steps since the world's start; derived from <see cref="Now"/>.</summary>
    public long StepCount => (Now.Milliseconds - Identity.Start.Milliseconds) / StepMs;

    public IdAllocator Ids { get; } = new();

    public EventLedger Ledger { get; } = new();

    public CommandJournal Journal { get; } = new();

    public OwnershipRegistry Ownership { get; } = new();

    public TimeControl TimeControl { get; } = new();

    public IReadOnlyList<ISimulationSystem> Systems => _scheduler.Systems;

    public IReadOnlyList<DebugMarker> Markers => _markers;

    public IReadOnlyCollection<Breakpoint> Breakpoints => _breakpoints;

    public IReadOnlyList<PendingDecision> PendingDecisions => _decisions;

    /// <summary>Where an exception left the state partly updated; the world then refuses further changes.</summary>
    public FaultStage Fault { get; private set; }

    /// <summary>The command that threw, when <see cref="Fault"/> is <see cref="FaultStage.Command"/>.</summary>
    public SimCommand? FaultingCommand => Fault == FaultStage.Command ? _inFlight : null;

    /// <summary>SHA-256 of the save this world was loaded from, or null for a world created from a scenario.</summary>
    public string? BaseSaveSha256 { get; private set; }

    /// <summary>Recomputations of the marker spatial index; shows that queries reuse it until markers change.</summary>
    public int MarkerIndexComputations => _markerIndex.Computations;

    ulong ILogContext.WorldSeed => Seed;

    long ILogContext.SimTimeMs => Now.Milliseconds;

    /// <summary>Creates a world at the scenario's start time. Hosts pass a fresh <paramref name="id"/> for each new world.</summary>
    public static World Create(ScenarioDefinition scenario, ulong seed, Logger? logger = null, WorldId id = default)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        var issues = scenario.Validate().ToList();
        if (issues.Count > 0)
        {
            throw new ArgumentException($"Scenario {scenario.Id} is invalid: {string.Join("; ", issues.Select(issue => $"{issue.JsonPath}: {issue.Message}"))}", nameof(scenario));
        }

        var identity = new WorldIdentity(id, seed, scenario.Id, SimulationRules.Version, SimulationRules.NoGenerator,
            scenario.ExtentM, scenario.GeographicReference, scenario.StepMs, scenario.StartTime);
        var world = new World(identity, logger ?? Logger.None);
        world.Record(WorldEventType.WorldCreated, EventCause.Origin, [], new Dictionary<string, string>
        {
            ["scenario"] = scenario.Id.ToString(),
            ["rules_version"] = SimulationRules.Version.ToString(CultureInfo.InvariantCulture),
        });
        world._log.Info("world created", data: new Dictionary<string, string>
        {
            ["scenario"] = scenario.Id.ToString(),
            ["extent_m"] = string.Create(CultureInfo.InvariantCulture, $"{scenario.ExtentM.East}x{scenario.ExtentM.North}"),
            ["step_ms"] = scenario.StepMs.ToString(CultureInfo.InvariantCulture),
            ["start"] = WorldCalendar.ToCalendar(scenario.StartTime).ToString(),
            ["world_id"] = id.ToString(),
        });
        return world;
    }

    /// <summary>Rebuilds a world from saved parts; <see cref="Persistence.SaveFile"/> validates them first.</summary>
    internal static World Restore(WorldIdentity identity, SimTime now, long commandsApplied, IReadOnlyDictionary<string, ulong> counters,
        IEnumerable<DebugMarker> markers, IEnumerable<Breakpoint> breakpoints, IEnumerable<PendingDecision> decisions,
        IEnumerable<WorldEvent> ledger, string saveSha256, Logger? logger)
    {
        var world = new World(identity, logger ?? Logger.None) { Now = now, BaseSaveSha256 = saveSha256 };
        world.Journal.PriorCount = commandsApplied;
        world.Ids.Restore(counters);
        world._markers.AddRange(markers);
        world._markerVersion.Bump();
        foreach (var breakpoint in breakpoints)
        {
            world._breakpoints.Add(breakpoint);
        }

        world._decisions.AddRange(decisions);
        world.Ledger.Restore(ledger);
        world._log.Info("world loaded", data: new Dictionary<string, string> { ["world_id"] = identity.Id.ToString() });
        return world;
    }

    /// <summary>
    /// Applies a command now and records it in the journal with the outcome. Returns the journal entry; its
    /// <see cref="JournalEntry.Rejection"/> says why a refused command changed nothing.
    /// </summary>
    public JournalEntry Submit(SimCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command is ControlCommand)
        {
            throw new ArgumentException("control commands change host pacing, not world state; apply them with Control", nameof(command));
        }

        EnsureNotFaulted();
        var sequence = Journal.NextSequence;
        Fault = FaultStage.Command;
        _inFlight = command;
        var rejection = Apply(command, sequence);
        Fault = FaultStage.None;
        _inFlight = null;
        var entry = Journal.Append(Now, command, rejection);
        if (rejection is not null)
        {
            _log.Warning($"command rejected: {rejection}", data: new Dictionary<string, string>
            {
                ["sequence"] = entry.Sequence.ToString(CultureInfo.InvariantCulture),
                ["command"] = command.GetType().Name,
            });
        }

        return entry;
    }

    /// <summary>
    /// Applies a pacing command to <see cref="TimeControl"/> (SOV-P02-T11) and returns why it was refused, or null.
    /// Pacing is host state: it is not journaled and never changes world state, so pausing, stepping or changing
    /// speed cannot alter outcomes (SOV-P02-T16).
    /// </summary>
    public string? Control(ControlCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        switch (command)
        {
            case Pause:
                TimeControl.Paused = true;
                break;
            case Resume:
                TimeControl.Paused = false;
                break;
            case SetSpeed { SimMillisecondsPerRealSecond: <= 0 or > TimeControl.MaxSimMillisecondsPerRealSecond } speed:
                return $"speed must be 1 to {TimeControl.MaxSimMillisecondsPerRealSecond} simulated ms per real second, found {speed.SimMillisecondsPerRealSecond}";
            case SetSpeed speed:
                TimeControl.SimMillisecondsPerRealSecond = speed.SimMillisecondsPerRealSecond;
                break;
            case StepOnce when _decisions.Count > 0:
                return "a decision is pending; acknowledge it before stepping";
            case StepOnce:
                TimeControl.RequestedSteps++;
                break;
            default:
                throw new NotSupportedException($"No handler for control command {command.GetType().Name}");
        }

        _log.Debug($"time control {command.GetType().Name}", data: new Dictionary<string, string>
        {
            ["paused"] = TimeControl.Paused ? "true" : "false",
            ["speed_ms_per_s"] = TimeControl.SimMillisecondsPerRealSecond.ToString(CultureInfo.InvariantCulture),
        });
        return null;
    }

    /// <summary>
    /// Advances toward <paramref name="target"/>, a whole number of steps ahead, and returns the time reached. It
    /// stops early at a pending decision and never passes one. Updates due at a time run when the clock leaves it,
    /// so commands submitted while the world sits at a time apply before that time's updates.
    /// </summary>
    public SimTime AdvanceTo(SimTime target)
    {
        EnsureNotFaulted();
        var distance = target.Milliseconds - Now.Milliseconds;
        if (distance < 0 || distance % StepMs != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(target), target,
                $"target must be {Now} or later by a whole number of {StepMs} ms steps");
        }

        while (Now < target && _decisions.Count == 0)
        {
            Fault = FaultStage.Step;
            foreach (var system in _scheduler.DueAt(Now))
            {
                system.Update(new SystemContext(this, system, (type, subjects, data) =>
                    Record(type, EventCause.FromSystem(system.Id), subjects, data)));
            }

            var next = target;
            if (_scheduler.NextDueAfter(Now) is { } due && due < next)
            {
                next = due;
            }

            if (_breakpoints.Count > 0 && _breakpoints.Min!.At < next)
            {
                next = _breakpoints.Min.At;
            }

            Now = next;
            RaiseDecisionsDueNow();
            Fault = FaultStage.None;
        }

        return Now;
    }

    /// <summary>Advances one fixed step unless a decision is pending.</summary>
    public SimTime Step() => AdvanceTo(Now.Plus(StepMs));

    /// <summary>Uses one step requested with <see cref="StepOnce"/>; false when none was requested.</summary>
    public bool TryTakeRequestedStep()
    {
        if (TimeControl.RequestedSteps == 0)
        {
            return false;
        }

        TimeControl.RequestedSteps--;
        return true;
    }

    /// <summary>
    /// Reports what the simulation knows at a position now, with provenance for every reading. It reads state and
    /// never changes it, whatever the host's view or residency (ADR-0007).
    /// </summary>
    public LocationInspection Inspect(WorldPoint point, double markerRadiusM = 0)
    {
        var inside = point.IsFinite && Extent.Contains(point);
        var families = FieldFamilies.All.Select(family => family == FieldFamilies.GeographicReference
                ? new FamilyInspection(family, FieldAvailability.Partial, GeographicReadings(point, inside))
                : new FamilyInspection(family, FieldAvailability.Unavailable,
                    [FieldReading.Unavailable("all quantities", FieldFamilies.UnavailableReason(family))]))
            .ToList();
        return new LocationInspection(point, inside, Now, Seed, Extent, families, MarkersNear(point, markerRadiusM));
    }

    /// <summary>
    /// SHA-256 over a canonical little-endian encoding of all authoritative state: identity except the world ID,
    /// clock, number of world commands applied, ID counters, markers, breakpoints, pending decisions and the event
    /// ledger. Equal checksums mean equal
    /// state; the encoding changes only with <c>ChecksumFormat</c>.
    /// </summary>
    public string ComputeChecksum()
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[8];
        void Int64(long value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
            hash.AppendData(buffer);
        }

        void Double(double value) => Int64(BitConverter.DoubleToInt64Bits(value));

        void Text(string? value)
        {
            if (value is null)
            {
                Int64(-1);
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(value);
            Int64(bytes.Length);
            hash.AppendData(bytes);
        }

        Int64(ChecksumFormat);
        Int64(unchecked((long)Seed));
        Text(ScenarioId.ToString());
        Int64(Identity.RulesVersion);
        Int64(Identity.GeneratorVersion);
        Double(Extent.East);
        Double(Extent.North);
        Double(Identity.Geography.SouthEdgeLatitudeRad);
        Double(Identity.Geography.StandardMeridianX);
        Int64(StepMs);
        Int64(Identity.Start.Milliseconds);
        Int64(Now.Milliseconds);
        Int64(Journal.NextSequence - 1);
        Int64(Ids.Counters.Count);
        foreach (var (kind, last) in Ids.Counters)
        {
            Text(kind);
            Int64(unchecked((long)last));
        }

        Int64(_markers.Count);
        foreach (var marker in _markers)
        {
            Int64(unchecked((long)marker.Id.Value));
            Double(marker.Position.X);
            Double(marker.Position.Y);
            Int64(marker.PlacedAt.Milliseconds);
            Text(marker.Label);
        }

        Int64(_breakpoints.Count);
        foreach (var breakpoint in _breakpoints)
        {
            Int64(unchecked((long)breakpoint.Id.Value));
            Int64(breakpoint.At.Milliseconds);
            Text(breakpoint.Label);
            Int64(unchecked((long)breakpoint.SetBy.Value));
        }

        Int64(_decisions.Count);
        foreach (var decision in _decisions)
        {
            Int64(unchecked((long)decision.Event.Value));
            Int64(decision.At.Milliseconds);
            Text(decision.Source);
            Text(decision.Label);
        }

        Int64(Ledger.Count);
        foreach (var worldEvent in Ledger.Events)
        {
            Int64(unchecked((long)worldEvent.Id.Value));
            Int64(worldEvent.At.Milliseconds);
            Text(worldEvent.Type.ToString());
            Text(worldEvent.Cause.Kind.ToString());
            Int64(worldEvent.Cause.CommandSequence ?? -1);
            Text(worldEvent.Cause.System);
            Int64(unchecked((long)(worldEvent.Cause.ParentEvent?.Value ?? 0)));
            Int64(worldEvent.Subjects.Count);
            foreach (var subject in worldEvent.Subjects)
            {
                Text(subject);
            }

            var data = worldEvent.Data?.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToList() ?? [];
            Int64(data.Count);
            foreach (var (key, value) in data)
            {
                Text(key);
                Text(value);
            }
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private void EnsureNotFaulted()
    {
        if (Fault != FaultStage.None)
        {
            throw new InvalidOperationException($"the world faulted during a {Fault.ToString().ToLowerInvariant()} at {Now}; its state is partial and it cannot change further");
        }
    }

    private WorldEvent Record(WorldEventType type, EventCause cause, IReadOnlyList<string> subjects, IReadOnlyDictionary<string, string>? data = null)
    {
        var worldEvent = new WorldEvent(Ids.Next<Kind.Event>(), Now, type, cause, subjects, data);
        Ledger.Append(worldEvent);
        var severity = type is WorldEventType.MarkerPlaced or WorldEventType.MarkerRemoved ? LogSeverity.Debug : LogSeverity.Info;
        _log.Write(severity, $"event {type}", [worldEvent.Id.ToString(), .. subjects], data);
        return worldEvent;
    }

    private string? Apply(SimCommand command, long sequence)
    {
        var cause = EventCause.FromCommand(sequence);
        switch (command)
        {
            case PlaceMarker place:
                if (!place.Position.IsFinite || !Extent.Contains(place.Position))
                {
                    return $"marker position {place.Position} is outside the world";
                }

                if (place.Label is null || place.Label.Length > PlaceMarker.MaxLabelLength || place.Label.Any(char.IsControl))
                {
                    return $"marker label must be at most {PlaceMarker.MaxLabelLength} characters without control characters";
                }

                var marker = new DebugMarker(Ids.Next<Kind.Marker>(), place.Position, place.Label, Now);
                _markers.Add(marker);
                _markerVersion.Bump();
                Record(WorldEventType.MarkerPlaced, cause, [marker.Id.ToString()], new Dictionary<string, string>
                {
                    ["label"] = marker.Label,
                    ["position"] = marker.Position.ToString(),
                });
                return null;

            case RemoveMarker remove:
                var index = _markers.FindIndex(existing => existing.Id == remove.Marker);
                if (index < 0)
                {
                    return Ids.WasIssued(remove.Marker) ? $"{remove.Marker} was already removed" : $"{remove.Marker} does not exist";
                }

                _markers.RemoveAt(index);
                _markerVersion.Bump();
                Record(WorldEventType.MarkerRemoved, cause, [remove.Marker.ToString()]);
                return null;

            case SetBreakpoint set:
                if (set.At <= Now || (set.At.Milliseconds - Identity.Start.Milliseconds) % StepMs != 0)
                {
                    return $"breakpoint time {set.At} must be after {Now} and on the {StepMs} ms step grid";
                }

                if (set.Label is null || set.Label.Length > PlaceMarker.MaxLabelLength || set.Label.Any(char.IsControl))
                {
                    return $"breakpoint label must be at most {PlaceMarker.MaxLabelLength} characters without control characters";
                }

                var id = Ids.Next<Kind.Breakpoint>();
                var setEvent = Record(WorldEventType.BreakpointSet, cause, [id.ToString()], new Dictionary<string, string>
                {
                    ["at"] = WorldCalendar.ToCalendar(set.At).ToString(),
                    ["label"] = set.Label,
                });
                _breakpoints.Add(new Breakpoint(id, set.At, set.Label, setEvent.Id));
                return null;

            case AcknowledgeDecision acknowledge:
                var pending = _decisions.FindIndex(decision => decision.Event == acknowledge.Decision);
                if (pending < 0)
                {
                    return $"{acknowledge.Decision} is not a pending decision";
                }

                _decisions.RemoveAt(pending);
                Record(WorldEventType.DecisionAcknowledged, cause, [acknowledge.Decision.ToString()]);
                return null;

            default:
                throw new NotSupportedException($"No handler for command {command.GetType().Name}");
        }
    }

    private void RaiseDecisionsDueNow()
    {
        while (_breakpoints.Count > 0 && _breakpoints.Min!.At == Now)
        {
            var breakpoint = _breakpoints.Min;
            _breakpoints.Remove(breakpoint);
            var raised = Record(WorldEventType.DecisionRaised, EventCause.FromEvent(breakpoint.SetBy), [breakpoint.Id.ToString()],
                new Dictionary<string, string> { ["label"] = breakpoint.Label });
            _decisions.Add(new PendingDecision(raised.Id, Now, breakpoint.Id.ToString(), breakpoint.Label));
        }

        if (_decisions.Count > 0)
        {
            // A step requested before the stop must not run unasked after the acknowledgement.
            TimeControl.RequestedSteps = 0;
        }
    }

    private IReadOnlyList<FieldReading> GeographicReadings(WorldPoint point, bool inside)
    {
        const string outside = "unavailable: the point is outside the world frame";
        var geography = Identity.Geography;
        var latitude = inside
            ? new FieldReading("latitude", geography.LatitudeAt(point.Y), "rad", ReadingBasis.Derived, "World foundation",
                "geographic reference: south-edge latitude", "φ = φ₀ + y / 6 371 000 m", Identity.Start)
            : FieldReading.Unavailable("latitude", outside);
        var solar = inside
            ? new FieldReading("local mean solar time", geography.LocalSolarTimeOfDayMs(Now, point.X, Extent) / 1000.0, "s",
                ReadingBasis.Derived, "Simulation scheduler", "world clock; geographic reference: standard meridian",
                "standard time + (x − x_std) / (R cos φ_mid) · 86 400 s / 2π", Now)
            : FieldReading.Unavailable("local mean solar time", outside);
        return
        [
            new("easting x", point.X, "m", ReadingBasis.Query, Source: "inspected position"),
            new("northing y", point.Y, "m", ReadingBasis.Query, Source: "inspected position"),
            latitude,
            solar,
            FieldReading.Unavailable("elevation above datum", "unavailable: World foundation implements elevation in Phase 04"),
        ];
    }

    private List<DebugMarker> MarkersNear(WorldPoint point, double radiusM)
    {
        var radius = Math.Max(0, radiusM);
        if (!point.IsFinite || !double.IsFinite(radius))
        {
            return [];
        }

        bool Within(DebugMarker marker)
        {
            var dx = marker.Position.X - point.X;
            var dy = marker.Position.Y - point.Y;
            return dx * dx + dy * dy <= radius * radius;
        }

        var cellsPerSide = 2 * radius / MarkerCellM + 2;
        List<DebugMarker> found;
        if (cellsPerSide * cellsPerSide > 4 * (_markers.Count + 1) || !Extent.Contains(point))
        {
            // A radius covering more cells than there are markers is cheaper to answer by scanning.
            found = [.. _markers.Where(Within)];
        }
        else
        {
            var (minX, minY) = Cell(new WorldPoint(point.X - radius, point.Y - radius));
            var (maxX, maxY) = Cell(new WorldPoint(point.X + radius, point.Y + radius));
            var index = _markerIndex.Value;
            found = [];
            for (var cx = minX; cx <= maxX; cx++)
            {
                for (var cy = minY; cy <= maxY; cy++)
                {
                    found.AddRange(index[(cx, cy)].Where(Within));
                }
            }
        }

        found.Sort((a, b) => a.Id.Value.CompareTo(b.Id.Value));
        return found;
    }

    private static (long, long) Cell(WorldPoint point) =>
        ((long)Math.Floor(point.X / MarkerCellM), (long)Math.Floor(point.Y / MarkerCellM));
}
