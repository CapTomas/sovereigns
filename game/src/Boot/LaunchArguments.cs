using System.Globalization;
using Sovereigns.Simulation.Geometry;
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
        "--reports <dir>; --bindings <file.json>; --record <journal.jsonl>; --capture <file.png>; --capture-frame <n>; --fail-at-ms <ms>; " +
        "--select <x,y>; --marker <x,y>; --layer <name>";

    private static readonly string[] OptionNames =
        ["fixture", "scenario", "seed", "content", "log", "reports", "bindings", "record", "capture", "capture-frame", "fail-at-ms", "select", "marker", "layer"];

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

    /// <summary>Simulation time at which to inject a failure, to exercise failure reporting.</summary>
    public long? FailAtMs { get; init; }

    /// <summary>World location to select at startup.</summary>
    public WorldPoint? Select { get; init; }

    /// <summary>Location at which to submit a PlaceMarker command at startup.</summary>
    public WorldPoint? Marker { get; init; }

    /// <summary>Layer key or name to activate at startup.</summary>
    public string? Layer { get; init; }

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
            FailAtMs = values.TryGetValue("fail-at-ms", out var failAt) ? ParseFailAt(failAt) : null,
            Select = values.TryGetValue("select", out var select) ? ParsePoint("--select", select) : null,
            Marker = values.TryGetValue("marker", out var marker) ? ParsePoint("--marker", marker) : null,
        };
    }

    private static int ParseCaptureFrame(string text) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : throw new LaunchException($"--capture-frame must be a positive whole number of frames, found '{text}'");

    private static long ParseFailAt(string text) =>
        long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new LaunchException($"--fail-at-ms must be a non-negative whole number of milliseconds, found '{text}'");

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
