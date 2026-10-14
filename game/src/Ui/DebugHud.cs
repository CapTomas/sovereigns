using System.Globalization;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Client.Ui;

/// <summary>Everything the debug HUD shows (SOV-P01-T11), gathered by the shell once per refresh.</summary>
public sealed record DebugHudModel(
    ulong Seed,
    string? FixtureReference,
    string ScenarioId,
    SimTime Now,
    long StepCount,
    long StepMs,
    WorldPoint CameraCentre,
    double MetresPerPixel,
    double Fps,
    double FrameMs,
    double LastStepCostMs,
    long ManagedHeapBytes,
    long GodotStaticBytes,
    int OrphanNodes,
    int WarningCount,
    int ErrorCount,
    string? LastWarning,
    int JournalEntries,
    int PendingCommands,
    string Checksum,
    string BuildConfiguration,
    bool Failed);

/// <summary>A translucent overlay of seed, clock, camera, frame, memory, log and journal state.</summary>
public partial class DebugHud : PanelContainer
{
    private Label _label = null!;

    public string Text => _label.Text;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Position = new Vector2(8, 8);
        CustomMinimumSize = new Vector2(380, 0);
        AddThemeStyleboxOverride("panel", UiStyle.Box(new Color(0.05f, 0.06f, 0.08f, 0.82f), 6));
        _label = UiStyle.MakeLabel(12, wrap: true);
        AddChild(_label);
    }

    public void Update(DebugHudModel model) => _label.Text = Format(model);

    public static string Format(DebugHudModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        string F(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
        var lines = new List<string>
        {
            "DEBUG HUD (F3 to hide)",
            F($"seed: {model.Seed}   fixture: {model.FixtureReference ?? "none"}"),
            F($"scenario: {model.ScenarioId}"),
            F($"sim time: {model.Now.ToElapsedString()} ({model.Now.Milliseconds} ms){(model.Failed ? "  STOPPED" : "")}"),
            F($"steps: {model.StepCount}   step size: {model.StepMs} ms   real-time pace 1x"),
            "time controls: SOV-P02-T11",
            "active world chunk: unavailable — chunking arrives with SOV-P03-T07",
            F($"camera centre: {model.CameraCentre}   {model.MetresPerPixel:0.###} m/px"),
            F($"fps: {model.Fps:0}   frame: {model.FrameMs:0.0} ms   last sim step: {model.LastStepCostMs:0.000} ms"),
            F($"managed heap: {Megabytes(model.ManagedHeapBytes)}   godot static: {Megabytes(model.GodotStaticBytes)}"),
            F($"orphan nodes: {model.OrphanNodes}"),
            F($"log warnings: {model.WarningCount}   errors: {model.ErrorCount}"),
            F($"last warning/error: {model.LastWarning ?? "none"}"),
            F($"journal entries: {model.JournalEntries}   pending commands: {model.PendingCommands}"),
            F($"state checksum: {model.Checksum}"),
            F($"build: {model.BuildConfiguration}"),
        };
        return string.Join('\n', lines);
    }

    private static string Megabytes(long bytes) => string.Create(CultureInfo.InvariantCulture, $"{bytes / 1048576.0:0.0} MiB");
}
