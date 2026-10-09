using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Content;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Fields;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Markers;
using Sovereigns.Simulation.Scenarios;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation;

/// <summary>
/// One authoritative seeded world. It advances only through <see cref="Step"/> in fixed whole-millisecond
/// steps, never from a rendering clock, and changes only through submitted commands (ADR-0001, ADR-0004).
/// The world has no physical fields yet; <see cref="Inspect"/> reports each family as unavailable.
/// </summary>
public sealed class World : ILogContext
{
    private const int ChecksumFormat = 1;

    private readonly Queue<SimCommand> _pending = new();
    private readonly List<DebugMarker> _markers = [];
    private readonly SubsystemLog _log;
    private ulong _lastMarkerId;
    private SimCommand? _inFlight;

    private World(ScenarioDefinition scenario, ulong seed, Logger logger)
    {
        Seed = seed;
        ScenarioId = scenario.Id;
        Extent = scenario.ExtentM;
        StepMs = scenario.StepMs;
        _log = logger.For("world", this);
    }

    public ulong Seed { get; }

    public ContentId ScenarioId { get; }

    public WorldExtent Extent { get; }

    public long StepMs { get; }

    public SimTime Now { get; private set; }

    public long StepCount { get; private set; }

    public IReadOnlyList<DebugMarker> Markers => _markers;

    public CommandJournal Journal { get; } = new();

    public int PendingCommandCount => _pending.Count;

    /// <summary>
    /// True from the start of <see cref="Step"/> until it completes. It stays true when a step threw: the state
    /// is then partly updated, and the world refuses to step again.
    /// </summary>
    public bool StepInProgress { get; private set; }

    /// <summary>Commands submitted but not applied: the one being applied when a step threw, then the queue.</summary>
    public IReadOnlyList<SimCommand> UnappliedCommands => _inFlight is null ? [.. _pending] : [_inFlight, .. _pending];

    ulong ILogContext.WorldSeed => Seed;

    long ILogContext.SimTimeMs => Now.Milliseconds;

    /// <summary>Creates the world at <see cref="SimTime.Zero"/> from a validated scenario and a seed.</summary>
    public static World Create(ScenarioDefinition scenario, ulong seed, Logger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        var issues = scenario.Validate().ToList();
        if (issues.Count > 0)
        {
            throw new ArgumentException($"Scenario {scenario.Id} is invalid: {string.Join("; ", issues.Select(issue => $"{issue.JsonPath}: {issue.Message}"))}", nameof(scenario));
        }

        var world = new World(scenario, seed, logger ?? Logger.None);
        world._log.Info("world created", data: new Dictionary<string, string>
        {
            ["scenario"] = scenario.Id.ToString(),
            ["extent_m"] = string.Create(CultureInfo.InvariantCulture, $"{scenario.ExtentM.East}x{scenario.ExtentM.North}"),
            ["step_ms"] = scenario.StepMs.ToString(CultureInfo.InvariantCulture),
        });
        return world;
    }

    /// <summary>Queues a command; it takes effect at the start of the next <see cref="Step"/>.</summary>
    public void Submit(SimCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        _pending.Enqueue(command);
    }

    /// <summary>Applies queued commands in submission order at <see cref="Now"/>, then advances one fixed step.</summary>
    public void Step()
    {
        if (StepInProgress)
        {
            throw new InvalidOperationException($"the step at {Now} failed earlier; the world state is partial and cannot advance");
        }

        StepInProgress = true;
        while (_pending.TryDequeue(out var command))
        {
            _inFlight = command;
            var rejection = Apply(command);
            _inFlight = null;
            var entry = Journal.Append(Now.Plus(StepMs), command, rejection);
            if (rejection is not null)
            {
                _log.Warning($"command rejected: {rejection}", data: new Dictionary<string, string>
                {
                    ["sequence"] = entry.Sequence.ToString(CultureInfo.InvariantCulture),
                    ["command"] = command.GetType().Name,
                });
            }
        }

        Now = Now.Plus(StepMs);
        StepCount++;
        StepInProgress = false;
    }

    /// <summary>Steps until <see cref="Now"/> equals <paramref name="target"/>, which must lie a whole number of steps ahead.</summary>
    public void AdvanceTo(SimTime target)
    {
        var distance = target.Milliseconds - Now.Milliseconds;
        if (distance < 0 || distance % StepMs != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(target), target,
                $"target must be {Now} or later by a whole number of {StepMs} ms steps");
        }

        while (Now < target)
        {
            Step();
        }
    }

    /// <summary>Reports what the simulation knows at a position now. Markers within <paramref name="markerRadiusM"/> are listed.</summary>
    public LocationInspection Inspect(WorldPoint point, double markerRadiusM = 0)
    {
        var inside = point.IsFinite && Extent.Contains(point);
        var families = FieldFamilies.All.Select(family => family == FieldFamilies.GeographicReference
                ? new FamilyInspection(family, FieldAvailability.Partial, GeographicReadings(point))
                : new FamilyInspection(family, FieldAvailability.Unavailable,
                    [FieldReading.Unavailable("all quantities", FieldFamilies.UnavailableReason(family))]))
            .ToList();
        var radius = Math.Max(0, markerRadiusM);
        var radiusSquared = radius * radius;
        var nearby = _markers.Where(marker =>
        {
            var dx = marker.Position.X - point.X;
            var dy = marker.Position.Y - point.Y;
            return dx * dx + dy * dy <= radiusSquared;
        }).ToList();
        return new LocationInspection(point, inside, Now, Seed, Extent, families, nearby);
    }

    /// <summary>
    /// SHA-256 over a canonical little-endian encoding of all authoritative state. Equal checksums mean
    /// equal state; the encoding changes only with <c>ChecksumFormat</c>.
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

        void Text(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            Int64(bytes.Length);
            hash.AppendData(bytes);
        }

        Int64(ChecksumFormat);
        Int64(unchecked((long)Seed));
        Text(ScenarioId.ToString());
        Int64(BitConverter.DoubleToInt64Bits(Extent.East));
        Int64(BitConverter.DoubleToInt64Bits(Extent.North));
        Int64(StepMs);
        Int64(Now.Milliseconds);
        Int64(StepCount);
        Int64(unchecked((long)_lastMarkerId));
        Int64(_markers.Count);
        foreach (var marker in _markers)
        {
            Int64(unchecked((long)marker.Id.Value));
            Int64(BitConverter.DoubleToInt64Bits(marker.Position.X));
            Int64(BitConverter.DoubleToInt64Bits(marker.Position.Y));
            Int64(marker.PlacedAt.Milliseconds);
            Text(marker.Label);
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private string? Apply(SimCommand command)
    {
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

                var marker = new DebugMarker(new MarkerId(++_lastMarkerId), place.Position, place.Label, Now);
                _markers.Add(marker);
                _log.Debug("marker placed", [marker.Id.ToString()], new Dictionary<string, string>
                {
                    ["label"] = marker.Label,
                    ["position"] = marker.Position.ToString(),
                });
                return null;

            case RemoveMarker remove:
                var index = _markers.FindIndex(marker => marker.Id == remove.Marker);
                if (index < 0)
                {
                    return $"{remove.Marker} does not exist";
                }

                _markers.RemoveAt(index);
                _log.Debug("marker removed", [remove.Marker.ToString()]);
                return null;

            default:
                throw new NotSupportedException($"No handler for command {command.GetType().Name}");
        }
    }

    private static IReadOnlyList<FieldReading> GeographicReadings(WorldPoint point) =>
    [
        new("easting x", point.X, "m"),
        new("northing y", point.Y, "m"),
        FieldReading.Unavailable("latitude", "unavailable: geographic reference arrives with SOV-P02-T01"),
        FieldReading.Unavailable("local solar time", "unavailable: calendar and solar time arrive with SOV-P02-T02"),
        FieldReading.Unavailable("elevation above datum", "unavailable: World foundation implements elevation in Phase 04"),
    ];
}
