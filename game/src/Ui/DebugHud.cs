using System.Globalization;
using Sovereigns.Simulation.Atlas;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Kernel;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Client.Ui;

/// <summary>Everything the debug HUD shows (SOV-P01-T11), gathered by the shell once per refresh.</summary>
public sealed record DebugHudModel(
    ulong Seed,
    string? FixtureReference,
    string ScenarioId,
    SimTime Now,
    Hemisphere Hemisphere,
    long StepCount,
    long StepMs,
    bool Paused,
    long SimMillisecondsPerRealSecond,
    int RequestedSteps,
    IReadOnlyList<PendingDecision> Decisions,
    string AcknowledgeKey,
    int LedgerEvents,
    WorldPoint CameraCentre,
    double MetresPerPixel,
    ChunkKey ActiveChunk,
    Residency ActiveResidency,
    string ActiveChunkField,
    ChunkCacheStatistics Cache,
    string Overlay,
    double Fps,
    double FrameMs,
    double LastAdvanceCostMs,
    long LastAdvanceSteps,
    long ManagedHeapBytes,
    long GodotStaticBytes,
    int OrphanNodes,
    int WarningCount,
    int ErrorCount,
    string? LastWarning,
    int JournalEntries,
    string Checksum,
    string BuildConfiguration,
    bool Failed);

/// <summary>A translucent overlay of seed, date, time control, camera, active atlas chunk and cache, frame, memory, log and journal state.</summary>
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
            F($"date: {ClockText.Date(model.Now)}  (world standard time)"),
            F($"season: {ClockText.Season(model.Now, model.Hemisphere)}"),
            F($"time: {TimeState(model)}"),
            F($"steps: {model.StepCount}   step size: {model.StepMs} ms   ledger events: {model.LedgerEvents}"),
        };
        if (model.Decisions.Count == 0)
        {
            lines.Add("pending decisions: none");
        }
        else
        {
            lines.AddRange(model.Decisions.Take(3).Select(decision =>
                F($"DECISION PENDING: {decision.Label} (raised {ClockText.Date(decision.At)}) — {model.AcknowledgeKey} to acknowledge")));
            if (model.Decisions.Count > 3)
            {
                lines.Add(F($"  and {model.Decisions.Count - 3} more"));
            }
        }

        lines.AddRange(
        [
            F($"active chunk: {model.ActiveChunk} ({model.ActiveResidency}) of {model.ActiveChunkField}"),
            F($"overlay: {model.Overlay}"),
            F($"cache: {model.Cache.ResidentChunks} chunks, {Megabytes(model.Cache.ResidentBytes)} of {Megabytes(model.Cache.BudgetBytes)}, loads {model.Cache.Loads}, evictions {model.Cache.Evictions}"),
            F($"camera centre: {model.CameraCentre}   {model.MetresPerPixel:0.###} m/px"),
            F($"fps: {model.Fps:0}   frame: {model.FrameMs:0.0} ms   advance: {model.LastAdvanceCostMs:0.000} ms/{model.LastAdvanceSteps} step(s)"),
            F($"managed heap: {Megabytes(model.ManagedHeapBytes)}   godot static: {Megabytes(model.GodotStaticBytes)}"),
            F($"orphan nodes: {model.OrphanNodes}"),
            F($"log warnings: {model.WarningCount}   errors: {model.ErrorCount}"),
            F($"last warning/error: {model.LastWarning ?? "none"}"),
            F($"journal entries: {model.JournalEntries}"),
            F($"state checksum: {model.Checksum}"),
            F($"build: {model.BuildConfiguration}"),
        ]);
        return string.Join('\n', lines);
    }

    private static string TimeState(DebugHudModel model)
    {
        var speed = ClockText.Speed(model.SimMillisecondsPerRealSecond);
        var state = model.Failed ? "STOPPED by a failure"
            : model.Decisions.Count > 0 ? $"STOPPED by a pending decision (speed {speed})"
            : model.Paused ? $"PAUSED (speed {speed})"
            : $"running at {speed}";
        return model.RequestedSteps > 0 ? string.Create(CultureInfo.InvariantCulture, $"{state}; {model.RequestedSteps} step(s) requested") : state;
    }

    private static string Megabytes(long bytes) => string.Create(CultureInfo.InvariantCulture, $"{bytes / 1048576.0:0.0} MiB");
}
