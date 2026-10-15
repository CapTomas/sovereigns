using System.Globalization;
using Sovereigns.Client.View;
using Sovereigns.Simulation.Atlas;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Kernel;
using Sovereigns.Simulation.Scenarios;

namespace Sovereigns.Client.Boot;

/// <summary>
/// Options after <c>--</c> on the Godot command line, for example
/// <c>godot --path game -- --fixture F-EMPTY --capture shell.png</c>.
/// </summary>
public sealed record LaunchArguments
{
    public const string Usage =
        "Options: --fixture <F-ID|path> | --scenario <content-id> --seed <n>; --content <dir>; --log <file.jsonl>; " +
        "--reports <dir>; --bindings <file.json>; --record <journal.jsonl>; --capture <file.png>; --capture-frame <n>; --fail-after-ms <ms>; " +
        "--select <x,y>; --marker <x,y>; --layer <key|name>; --paused; --speed <1-5>; " +
        "--paint <x,y,radius,value[;...]>; --line <x,y;x,y[;...][|...]>; --center <x,y>; --zoom <m/px>; --level <macro|regional|local|tactical>; --residency; --no-hud";

    private static readonly string[] OptionNames =
    [
        "fixture", "scenario", "seed", "content", "log", "reports", "bindings", "record", "capture", "capture-frame", "fail-after-ms",
        "select", "marker", "layer", "speed", "paint", "line", "center", "zoom", "level",
    ];

    /// <summary>Options that stand alone, without a value.</summary>
    private static readonly string[] FlagNames = ["paused", "residency", "no-hud"];

    private static string[]? s_injected;

    /// <summary>F-ID or manifest path that pins seed and scenario.</summary>
    public string? Fixture { get; init; }

    public string? Scenario { get; init; }

    public string? Seed { get; init; }

    /// <summary>Content directory; defaults to the repository's content/ (or next to an exported executable).</summary>
    public string? ContentDirectory { get; init; }

    public string? LogPath { get; init; }

    public string? ReportsDirectory { get; init; }

    /// <summary>Input binding overrides file; defaults to <c>user://input_bindings.json</c>.</summary>
    public string? BindingsFile { get; init; }

    /// <summary>Where to write the command journal at exit.</summary>
    public string? RecordPath { get; init; }

    /// <summary>Where to save a PNG of the viewport after a few rendered frames.</summary>
    public string? CapturePath { get; init; }

    /// <summary>Rendered frame after which <see cref="CapturePath"/> is saved; defaults to 10.</summary>
    public int CaptureFrame { get; init; } = 10;

    /// <summary>Milliseconds after the world's start at which to inject a failure, to exercise failure reporting.</summary>
    public long? FailAfterMs { get; init; }

    /// <summary>Start with time paused: the client submits a Pause command at startup.</summary>
    public bool Paused { get; init; }

    /// <summary>Speed preset number, 1 to 5, to select at startup with a SetSpeed command.</summary>
    public int? Speed { get; init; }

    /// <summary>World location to select at startup.</summary>
    public WorldPoint? Select { get; init; }

    /// <summary>Location at which to submit a PlaceMarker command at startup.</summary>
    public WorldPoint? Marker { get; init; }

    /// <summary>Layer key or name to activate at startup.</summary>
    public string? Layer { get; init; }

    /// <summary>Scratch strokes to submit as <c>PaintScratch</c> commands at startup, in order.</summary>
    public IReadOnlyList<PaintScratch> Paint { get; init; } = [];

    /// <summary>Survey lines to submit as <c>DrawSurveyLine</c> commands at startup, labelled S1, S2, …</summary>
    public IReadOnlyList<IReadOnlyList<WorldPoint>> Lines { get; init; } = [];

    /// <summary>World position of the map centre at startup.</summary>
    public WorldPoint? Center { get; init; }

    /// <summary>Map scale at startup, in metres per screen pixel.</summary>
    public double? MetresPerPixel { get; init; }

    /// <summary>Atlas level to sample and inspect at, instead of the one the zoom chooses.</summary>
    public GridLevel? Level { get; init; }

    /// <summary>Start with the chunk-residency overlay on.</summary>
    public bool Residency { get; init; }

    /// <summary>Start with the debug HUD hidden.</summary>
    public bool HideHud { get; init; }

    /// <summary>
    /// Supplies arguments to the next <see cref="FromEnvironment"/> call instead of the command line.
    /// Tests use this to boot the real scene with chosen options; it is consumed once.
    /// </summary>
    public static void InjectForNextBoot(IEnumerable<string> args) => s_injected = [.. args];

    public static LaunchArguments FromEnvironment()
    {
        var injected = s_injected;
        s_injected = null;
        return Parse(injected ?? OS.GetCmdlineUserArgs());
    }

    /// <exception cref="LaunchException">An option is unknown, repeated, lacks a value or has a malformed value.</exception>
    public static LaunchArguments Parse(IEnumerable<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        using var enumerator = args.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var option = enumerator.Current;
            var name = option.StartsWith("--", StringComparison.Ordinal) ? option[2..] : "";
            if (FlagNames.Contains(name))
            {
                if (!values.TryAdd(name, "true"))
                {
                    throw new LaunchException($"{option} given twice");
                }

                continue;
            }

            if (!OptionNames.Contains(name))
            {
                throw new LaunchException($"unknown launch argument '{option}'. {Usage}");
            }

            if (!enumerator.MoveNext())
            {
                throw new LaunchException($"{option} needs a value. {Usage}");
            }

            if (!values.TryAdd(name, enumerator.Current))
            {
                throw new LaunchException($"{option} given twice");
            }
        }

        return new LaunchArguments
        {
            Fixture = values.GetValueOrDefault("fixture"),
            Scenario = values.GetValueOrDefault("scenario"),
            Seed = values.GetValueOrDefault("seed"),
            ContentDirectory = values.GetValueOrDefault("content"),
            LogPath = values.GetValueOrDefault("log"),
            ReportsDirectory = values.GetValueOrDefault("reports"),
            BindingsFile = values.GetValueOrDefault("bindings"),
            RecordPath = values.GetValueOrDefault("record"),
            CapturePath = values.GetValueOrDefault("capture"),
            Layer = values.GetValueOrDefault("layer"),
            CaptureFrame = values.TryGetValue("capture-frame", out var frame) ? ParseCaptureFrame(frame) : 10,
            FailAfterMs = values.TryGetValue("fail-after-ms", out var failAfter) ? ParseFailAfter(failAfter) : null,
            Paused = values.ContainsKey("paused"),
            Speed = values.TryGetValue("speed", out var speed) ? ParseSpeed(speed) : null,
            Select = values.TryGetValue("select", out var select) ? ParsePoint("--select", select) : null,
            Marker = values.TryGetValue("marker", out var marker) ? ParsePoint("--marker", marker) : null,
            Paint = values.TryGetValue("paint", out var paint) ? ParsePaint(paint) : [],
            Lines = values.TryGetValue("line", out var line) ? ParseLines(line) : [],
            Center = values.TryGetValue("center", out var center) ? ParsePoint("--center", center) : null,
            MetresPerPixel = values.TryGetValue("zoom", out var zoom) ? ParseZoom(zoom) : null,
            Level = values.TryGetValue("level", out var level) ? ParseLevel(level) : null,
            Residency = values.ContainsKey("residency"),
            HideHud = values.ContainsKey("no-hud"),
        };
    }

    private static List<PaintScratch> ParsePaint(string text)
    {
        var strokes = new List<PaintScratch>();
        foreach (var stroke in text.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = stroke.Split(',');
            if (parts.Length == 4 &&
                double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) &&
                double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var radius) &&
                double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) &&
                double.IsFinite(x) && double.IsFinite(y) && radius > 0 && radius <= PaintScratch.MaxRadiusM && value is >= 0 and <= 1)
            {
                strokes.Add(new PaintScratch(new WorldPoint(x, y), radius, value));
                continue;
            }

            throw new LaunchException($"--paint must be east,north,radius,value strokes separated by ';' with 0 < radius <= {PaintScratch.MaxRadiusM:0} m and a value from 0 to 1, such as 50000,50000,500,1; found '{stroke}'");
        }

        return strokes.Count > 0 ? strokes : throw new LaunchException("--paint needs at least one stroke east,north,radius,value");
    }

    private static List<IReadOnlyList<WorldPoint>> ParseLines(string text)
    {
        var lines = new List<IReadOnlyList<WorldPoint>>();
        foreach (var line in text.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var points = line.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(point => ParsePoint("--line", point)).ToList();
            lines.Add(points.Count is >= 2 and <= DrawSurveyLine.MaxPoints
                ? points
                : throw new LaunchException($"--line needs 2 to {DrawSurveyLine.MaxPoints} points east,north separated by ';' (lines separated by '|'), found {points.Count} in '{line}'"));
        }

        return lines.Count > 0 ? lines : throw new LaunchException("--line needs at least one line of points east,north;east,north");
    }

    private static double ParseZoom(string text) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value) && value > 0
            ? value
            : throw new LaunchException($"--zoom must be a positive number of metres per screen pixel such as 25, found '{text}'");

    private static GridLevel ParseLevel(string text) =>
        ViewLevels.TryParse(text, out var level)
            ? level
            : throw new LaunchException($"--level must be macro, regional, local or tactical, found '{text}'");

    private static int ParseCaptureFrame(string text) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : throw new LaunchException($"--capture-frame must be a positive whole number of frames, found '{text}'");

    private static long ParseFailAfter(string text) =>
        long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new LaunchException($"--fail-after-ms must be a non-negative whole number of milliseconds, found '{text}'");

    private static int ParseSpeed(string text) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value >= 1 && value <= TimeControl.Presets.Count
            ? value
            : throw new LaunchException($"--speed must be a preset number from 1 to {TimeControl.Presets.Count} (1 s, 1 min, 1 h, 1 day, 1 week per real second), found '{text}'");

    private static WorldPoint ParsePoint(string option, string text)
    {
        var parts = text.Split(',');
        if (parts.Length == 2 &&
            double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
            double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) &&
            double.IsFinite(x) && double.IsFinite(y))
        {
            return new WorldPoint(x, y);
        }

        throw new LaunchException($"{option} must be two finite metre coordinates east,north such as 50000,50000, found '{text}'");
    }
}
