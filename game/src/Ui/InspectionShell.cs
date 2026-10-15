using System.Globalization;
using Sovereigns.Client.Boot;
using Sovereigns.Client.Controls;
using Sovereigns.Client.Diagnostics;
using Sovereigns.Client.Simulation;
using Sovereigns.Client.View;
using Sovereigns.Simulation;
using Sovereigns.Simulation.Atlas;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Ids;
using Sovereigns.Simulation.Kernel;
using Sovereigns.Simulation.Scenarios;
using Sovereigns.Simulation.Time;
using InputEvent = Godot.InputEvent;

namespace Sovereigns.Client.Ui;

/// <summary>What the inspection shell needs from the rest of the client.</summary>
public sealed record ShellContext(
    SimulationHost Host,
    LaunchSelection Selection,
    LaunchArguments? Arguments,
    InputBindings Bindings,
    InputHistory Inputs,
    RecentLogBuffer RecentLog,
    SubsystemLog Log,
    BuildInfo Build);

/// <summary>
/// The reusable 2D inspection shell (SOV-P01-T10, T11; SOV-P02-T11, T13; SOV-P03-T13, T14): pan/zoom map with atlas
/// field overlays, layer list, recent events, location inspector, debug HUD and the dispatch of input actions.
/// Every view reads the one <see cref="World"/> held by the host; field overlays and the inspector are sampled from
/// its <see cref="FieldAtlas"/> only. World-changing actions (markers, breakpoints, scratch paint, survey lines) only
/// submit commands, which the world journals with sequence and time; time controls (pause, step, speed) go to the
/// world's time control and change only how fast the host paces it.
/// </summary>
public partial class InspectionShell : Control
{
    private const double PickRadiusPixels = 14;
    private const double ZoomStep = 1.25;
    private const double RefreshSeconds = 0.25;
    private const double ChecksumSeconds = 1.0;

    private readonly ShellContext _context;
    private readonly WorldView _map = new();
    private readonly LayerPanel _layers = new();
    private readonly InspectorPanel _inspector = new();
    private readonly EventsPanel _events = new();
    private readonly DecisionBanner _decisions = new();
    private readonly DebugHud _hud = new();
    private readonly LayerLegend _legend = new();
    private readonly StatusBar _status = new();
    private readonly ErrorBanner _banner = new();
    private readonly DeveloperTools _tools = new();
    private readonly IReadOnlyList<MapLayer> _layerList;
    private int _layerIndex;
    private bool _viewPending;
    private int _markerSerial;
    private int _breakpointSerial;
    private bool _dragging;
    private bool _failureShown;
    private double _sinceRefresh = double.MaxValue;
    private double _sinceChecksum = double.MaxValue;
    private double _frameMs = 16.7;
    private string _checksum = "";

    public InspectionShell(ShellContext context)
    {
        _context = context;
        _layerList = MapLayers.For(context.Host.World.Atlas);
    }

    public WorldView Map => _map;

    public InspectorPanel Inspector => _inspector;

    public EventsPanel Events => _events;

    public DecisionBanner Decisions => _decisions;

    public DebugHud Hud => _hud;

    public LayerLegend Legend => _legend;

    public StatusBar Status => _status;

    public ErrorBanner Banner => _banner;

    public WorldPoint? SelectedPoint { get; private set; }

    public LayerPanel LayerList => _layers;

    public IReadOnlyList<MapLayer> Layers => _layerList;

    public MapLayer ActiveLayer => _layerList[_layerIndex];

    public DeveloperTools Tools => _tools;

    public bool SurveyLinesVisible => _map.ShowSurveyLines;

    public bool ResidencyVisible => _map.ShowResidency;

    /// <summary>The atlas level overlays are sampled at: the one chosen with the level keys, else the one the zoom picks.</summary>
    public GridLevel ViewLevel => _map.EffectiveLevel;

    /// <summary>The level the inspector samples at: the chosen one, else the overlay's level while a field layer is selected, else each field's native level (null).</summary>
    public GridLevel? InspectionLevel => _map.LevelOverride ?? (ActiveLayer.IsFieldOverlay ? _map.EffectiveLevel : null);

    private World World => _context.Host.World;

    public override void _Ready()
    {
        // Only the panels take mouse input; everything else must reach _UnhandledInput for map interaction.
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var root = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(root);

        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
        root.AddChild(body);
        var left = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        _layers.Layers = _layerList;
        left.AddChild(_layers);
        left.AddChild(_events);
        body.AddChild(left);

        _map.World = World;
        _map.PendingLine = _tools.Pending;
        _map.OverlayRebuilt += UpdateLegend;
        _map.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _map.SizeFlagsVertical = SizeFlags.ExpandFill;
        _map.AddChild(_hud);
        _map.AddChild(_decisions);
        _map.AddChild(_legend);
        body.AddChild(_map);
        body.AddChild(_inspector);
        root.AddChild(_status);
        AddChild(_banner);

        _layers.LayerChosen += SetLayer;
        _layers.SurveyLinesToggled += SetSurveyLines;
        _layers.ResidencyToggled += SetResidency;
        _status.SetHelp(HelpText.Build(_context.Bindings));
        ApplyLaunchArguments();
        _layers.ShowOverlays(_map.ShowSurveyLines, _map.ShowResidency);
        SetLayer(_layerIndex);
    }

    public override void _Process(double delta)
    {
        _frameMs = (_frameMs * 0.9) + (delta * 1000 * 0.1);
        var pan = Input.GetVector(InputActions.CameraPanLeft, InputActions.CameraPanRight, InputActions.CameraPanUp, InputActions.CameraPanDown);
        if (pan != Vector2.Zero)
        {
            _map.PanByKeys(pan, delta);
        }

        if (_viewPending && _map.Size.X > 1 && _map.Size.Y > 1)
        {
            ApplyLaunchView();
        }

        _sinceRefresh += delta;
        _sinceChecksum += delta;
        if (_sinceRefresh >= RefreshSeconds)
        {
            RefreshPanels();
        }
        else
        {
            UpdateDecisionBanner();
        }

        if (!_failureShown && _context.Host.Failure is { } failure)
        {
            _failureShown = true;
            _banner.ShowMessage(
            [
                $"SIMULATION STOPPED: {failure.Reason}",
                $"Failure report: {failure.ReportPath ?? "could not be written, see the log"}",
                $"Reproduce: {failure.ReplayCommand}",
            ]);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        if (_dragging && @event is InputEventMouseMotion motion)
        {
            _map.PanByDrag(motion.Relative);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (_dragging && @event.IsActionReleased(InputActions.CameraDrag, exactMatch: true))
        {
            _dragging = false;
            return;
        }

        foreach (var action in InputActions.All)
        {
            if (action.Name != InputActions.Quit && @event.IsActionPressed(action.Name, action.Repeats, exactMatch: true))
            {
                Dispatch(action.Name, @event);
                GetViewport().SetInputAsHandled();
                return;
            }
        }
    }

    /// <summary>Runs one input action as the player would trigger it. <paramref name="source"/> supplies the pointer position, if any.</summary>
    public void Dispatch(string action, InputEvent? source = null)
    {
        var detail = action switch
        {
            InputActions.CameraPanLeft or InputActions.CameraPanRight or InputActions.CameraPanUp or InputActions.CameraPanDown => "pan",
            InputActions.CameraZoomIn => Zoom(source, ZoomStep),
            InputActions.CameraZoomOut => Zoom(source, 1 / ZoomStep),
            InputActions.CameraDrag => StartDrag(),
            InputActions.CameraRecenter => FitWorld(),
            InputActions.SelectLocation => SelectAtPointer(source),
            InputActions.PlaceMarker => PlaceMarkerAtSelection(),
            InputActions.RemoveMarker => RemoveMarkerNearSelection(),
            InputActions.LayerNext => StepLayer(1),
            InputActions.LayerPrevious => StepLayer(-1),
            InputActions.ToggleHud => ToggleHud(),
            InputActions.FailureReport => WriteManualReport(),
            InputActions.TimeTogglePause => TogglePause(),
            InputActions.TimeStep => StepTime(),
            InputActions.SetBreakpoint => SetBreakpointAtNextMidnight(),
            InputActions.AcknowledgeDecision => AcknowledgeOldestDecision(),
            InputActions.LevelCoarser => StepLevel(-1),
            InputActions.LevelFiner => StepLevel(1),
            InputActions.ToggleSurveyLines => ToggleSurveyLines(),
            InputActions.ToggleResidency => ToggleResidency(),
            InputActions.PaintScratch => PaintAtCursor(source),
            InputActions.PaintRadiusUp => StepRadius(1),
            InputActions.PaintRadiusDown => StepRadius(-1),
            InputActions.PaintValueUp => StepValue(1),
            InputActions.PaintValueDown => StepValue(-1),
            InputActions.SurveyAddPoint => AddSurveyPoint(source),
            InputActions.SurveyFinish => FinishSurveyLine(),
            InputActions.SurveyCancel => CancelSurveyLine(),
            InputActions.SurveyRemove => RemoveSurveyLineNearSelection(),
            _ when InputActions.SpeedPresetOf(action) is >= 0 and var preset => SetSpeed(preset),
            _ => null,
        };
        if (detail is not null)
        {
            _context.Inputs.Record((long)Engine.GetProcessFrames(), Time.GetTicksMsec() / 1000.0, action, detail);
        }
    }

    public void SelectLocation(WorldPoint point)
    {
        SelectedPoint = point;
        _map.Selection = point;
        _sinceRefresh = double.MaxValue;
        RefreshPanels();
    }

    public void SetLayer(int index)
    {
        var count = _layerList.Count;
        _layerIndex = ((index % count) + count) % count;
        _map.ActiveLayer = ActiveLayer;
        _map.InvalidateOverlay();
        _layers.Highlight(_layerIndex);
        UpdateLegend();
        UpdateToolsLine();
        _sinceRefresh = double.MaxValue;
    }

    public void SetSurveyLines(bool visible)
    {
        _map.ShowSurveyLines = visible;
        _layers.ShowOverlays(_map.ShowSurveyLines, _map.ShowResidency);
    }

    public void SetResidency(bool visible)
    {
        _map.ShowResidency = visible;
        _layers.ShowOverlays(_map.ShowSurveyLines, _map.ShowResidency);
    }

    /// <summary>Re-queries the world and redraws the inspector, legend, tool state and HUD.</summary>
    public void RefreshPanels()
    {
        _sinceRefresh = 0;
        if (SelectedPoint is { } point)
        {
            var radius = PickRadiusPixels / _map.Transform.PixelsPerMetre;
            var inspection = World.Inspect(point, radius, InspectionLevel);
            _inspector.Show(inspection, radius, LevelNote(), ActiveLayer.Family);
            _map.HighlightedFeature = inspection.NearbyFeatures is [var nearest, ..] ? nearest.Feature.Id : null;
        }
        else
        {
            _inspector.ShowNothing();
            _map.HighlightedFeature = null;
        }

        UpdateLegend();
        UpdateToolsLine();
        _events.Show(World.Ledger);
        UpdateDecisionBanner();
        _hud.Update(BuildHudModel());
    }

    private void UpdateToolsLine() => _status.SetTools($"{_tools.Describe()}   |   view level: {LevelText()}");

    private void UpdateLegend() =>
        _legend.Show(ActiveLayer, World.Markers.Count, _map.Overlay, _map.EffectiveLevel, _map.LevelOverride is not null);

    private string LevelText() => _map.LevelOverride is { } fixedLevel
        ? $"{ViewLevels.Describe(fixedLevel)}, fixed"
        : ActiveLayer.IsFieldOverlay
            ? $"{ViewLevels.Describe(_map.EffectiveLevel)}, automatic from zoom"
            : "native per field (no field layer selected)";

    private string LevelNote()
    {
        var keys = $"{KeyNames(InputActions.LevelCoarser)} / {KeyNames(InputActions.LevelFiner)}";
        return InspectionLevel is { } level
            ? $"Inspection level: {ViewLevels.Describe(level)} ({(_map.LevelOverride is null ? "from the layer's zoom" : "fixed")}; {keys} change it)"
            : $"Inspection level: each field's native level (select a field layer or press {keys} to choose one)";
    }

    private void ApplyLaunchArguments()
    {
        var arguments = _context.Arguments;
        if (arguments?.Layer is { } layerName)
        {
            if (MapLayers.IndexOf(_layerList, layerName) is >= 0 and var index)
            {
                _layerIndex = index;
            }
            else
            {
                _context.Log.Warning($"--layer '{layerName}' matches no layer; use a name or key from the layer list");
            }
        }

        if (arguments?.Select is { } select)
        {
            SelectLocation(select);
        }

        if (arguments?.Speed is { } speed)
        {
            SetSpeed(speed - 1);
        }

        if (arguments?.Paused == true)
        {
            Control(new Pause());
        }

        if (arguments?.Marker is { } marker)
        {
            _markerSerial++;
            Submit(new PlaceMarker(marker, $"M{_markerSerial}"));
        }

        foreach (var stroke in arguments?.Paint ?? [])
        {
            Submit(stroke);
        }

        foreach (var points in arguments?.Lines ?? [])
        {
            if (Submit(new DrawSurveyLine(points, _tools.NextLabel)) is { Rejection: null })
            {
                _tools.Accepted();
            }
        }

        _map.LevelOverride = arguments?.Level;
        _map.ShowResidency = arguments?.Residency == true;
        _hud.Visible = arguments?.HideHud != true;
        _viewPending = arguments?.Center is not null || arguments?.MetresPerPixel is not null;
    }

    /// <summary>Applies <c>--center</c> and <c>--zoom</c> once the map has a size, starting from the fitted view.</summary>
    private void ApplyLaunchView()
    {
        _viewPending = false;
        _map.FitWorld();
        var fitted = _map.Transform;
        _map.SetView(_context.Arguments?.Center ?? fitted.Centre, _context.Arguments?.MetresPerPixel ?? fitted.MetresPerPixel);
        _map.InvalidateOverlay();
        _sinceRefresh = double.MaxValue;
    }

    private string Zoom(InputEvent? source, double factor)
    {
        var pointer = PointerInMap(source);
        if (!new Rect2(Vector2.Zero, _map.Size).HasPoint(pointer))
        {
            pointer = _map.Size / 2;
        }

        _map.ZoomAt(pointer, factor);
        return string.Create(CultureInfo.InvariantCulture, $"zoom x{factor:0.###} at {_map.Transform.ToWorld(pointer)}, {_map.Transform.PixelsPerMetre:0.####} px/m");
    }

    private string StartDrag()
    {
        _dragging = true;
        return "drag started";
    }

    private string FitWorld()
    {
        _map.FitWorld();
        return "fit world";
    }

    private string? SelectAtPointer(InputEvent? source)
    {
        var pointer = PointerInMap(source);
        if (!new Rect2(Vector2.Zero, _map.Size).HasPoint(pointer))
        {
            return null;
        }

        var point = _map.Transform.ToWorld(pointer);
        SelectLocation(point);
        return point.ToString();
    }

    /// <summary>The world location under the pointer, or null when the pointer is not over the map.</summary>
    private WorldPoint? CursorPoint(InputEvent? source)
    {
        var pointer = PointerInMap(source);
        return new Rect2(Vector2.Zero, _map.Size).HasPoint(pointer) ? _map.Transform.ToWorld(pointer) : null;
    }

    private string StepLevel(int direction)
    {
        _map.LevelOverride = ViewLevels.Step(_map.LevelOverride ?? _map.EffectiveLevel, direction);
        _map.InvalidateOverlay();
        _sinceRefresh = double.MaxValue;
        UpdateToolsLine();
        _status.ShowStatus(_map.LevelOverride is { } level
            ? $"View level fixed at {ViewLevels.Describe(level)}; overlays and the inspector sample at it."
            : $"View level automatic: the zoom chooses (now {ViewLevels.Describe(_map.EffectiveLevel)}).");
        return _map.LevelOverride is { } chosen ? ViewLevels.Describe(chosen) : "automatic";
    }

    private string ToggleSurveyLines()
    {
        SetSurveyLines(!_map.ShowSurveyLines);
        return _map.ShowSurveyLines ? "shown" : "hidden";
    }

    private string ToggleResidency()
    {
        SetResidency(!_map.ShowResidency);
        return _map.ShowResidency ? "shown" : "hidden";
    }

    private string? PaintAtCursor(InputEvent? source)
    {
        if (CursorPoint(source) is not { } point)
        {
            _status.ShowStatus("Move the cursor over the map to paint.", LineStyle.Warning);
            return null;
        }

        var command = _tools.Paint(point);
        if (Submit(command) is { Rejection: null })
        {
            var cells = World.Ledger.Events[^1] is { Type: Sovereigns.Simulation.Events.WorldEventType.FieldPainted, Data: { } data } && data.TryGetValue("cells", out var changed) ? changed : "?";
            _status.ShowStatus(string.Create(CultureInfo.InvariantCulture,
                $"PaintScratch applied at {point}: value {command.Value:0.00}, radius {MapScale.FormatDistance(command.RadiusM)}, {cells} local cell(s) changed, {ClockText.Date(World.Now)}."));
        }

        return string.Create(CultureInfo.InvariantCulture, $"value {command.Value:0.00} radius {command.RadiusM:0.###} m at {point}");
    }

    private string StepRadius(int direction)
    {
        var changed = _tools.StepRadius(direction);
        _sinceRefresh = double.MaxValue;
        UpdateToolsLine();
        _status.ShowStatus(changed
            ? $"Paint radius {MapScale.FormatDistance(_tools.RadiusM)}."
            : $"Paint radius is already at its {(direction > 0 ? "largest" : "smallest")}, {MapScale.FormatDistance(_tools.RadiusM)}.");
        return MapScale.FormatDistance(_tools.RadiusM);
    }

    private string StepValue(int direction)
    {
        var changed = _tools.StepValue(direction);
        _sinceRefresh = double.MaxValue;
        UpdateToolsLine();
        var value = _tools.Value.ToString("0.00", CultureInfo.InvariantCulture);
        _status.ShowStatus(changed ? $"Paint value {value}." : $"Paint value is already at {value}, the {(direction > 0 ? "largest" : "smallest")}.");
        return value;
    }

    private string? AddSurveyPoint(InputEvent? source)
    {
        if (CursorPoint(source) is not { } point)
        {
            _status.ShowStatus("Move the cursor over the map to add a survey point.", LineStyle.Warning);
            return null;
        }

        if (!World.Extent.Contains(point))
        {
            _status.ShowStatus($"{point} is outside the world; survey lines stay inside it.", LineStyle.Warning);
            return "outside the world";
        }

        var added = _tools.AddPoint(point);
        _sinceRefresh = double.MaxValue;
        UpdateToolsLine();
        _status.ShowStatus(added
            ? $"Survey point {_tools.Pending.Count} added at {point}. Press {KeyNames(InputActions.SurveyFinish)} to draw the line."
            : "That point repeats the last one; move the cursor first.", added ? LineStyle.Normal : LineStyle.Warning);
        return $"point {_tools.Pending.Count} at {point}";
    }

    private string FinishSurveyLine()
    {
        if (_tools.Finish() is not { } command)
        {
            _status.ShowStatus($"A survey line needs at least two points; add them with {KeyNames(InputActions.SurveyAddPoint)}.", LineStyle.Warning);
            return "fewer than two points";
        }

        if (Submit(command) is { Rejection: null })
        {
            _tools.Accepted();
            UpdateToolsLine();
            _status.ShowStatus($"DrawSurveyLine {command.Label} applied with {command.Points.Count} points, {ClockText.Date(World.Now)}.");
        }

        return $"{command.Label} with {command.Points.Count} points";
    }

    private string CancelSurveyLine()
    {
        var dropped = _tools.Pending.Count;
        _tools.Cancel();
        _sinceRefresh = double.MaxValue;
        UpdateToolsLine();
        _status.ShowStatus(dropped == 0 ? "No survey line is pending." : $"Pending survey line dropped ({dropped} point(s)).");
        return $"dropped {dropped} point(s)";
    }

    private string RemoveSurveyLineNearSelection()
    {
        if (SelectedPoint is not { } point)
        {
            _status.ShowStatus("Select a location near a survey line first.", LineStyle.Warning);
            return "no selection";
        }

        var radius = PickRadiusPixels / _map.Transform.PixelsPerMetre;
        var nearest = World.Inspect(point, radius).NearbyFeatures.FirstOrDefault(hit => hit.Feature.Kind == FeatureKind.SurveyLine);
        var prefix = Kind.SurveyLine.Name + ":";
        if (nearest is null || !nearest.Feature.Id.StartsWith(prefix, StringComparison.Ordinal) ||
            !ulong.TryParse(nearest.Feature.Id[prefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            _status.ShowStatus($"No survey line within {MapScale.FormatDistance(radius)} of the selected location.", LineStyle.Warning);
            return "no survey line in reach";
        }

        if (Submit(new RemoveSurveyLine(new EntityId<Kind.SurveyLine>(number))) is { Rejection: null })
        {
            _status.ShowStatus($"RemoveSurveyLine {nearest.Feature.Id} \"{nearest.Feature.Label}\" applied, {ClockText.Date(World.Now)}.");
        }

        return nearest.Feature.Id;
    }

    private string PlaceMarkerAtSelection()
    {
        if (SelectedPoint is not { } point)
        {
            _status.ShowStatus("Select a location first (left click), then place a marker.", LineStyle.Warning);
            return "no selection";
        }

        var label = $"M{++_markerSerial}";
        if (Submit(new PlaceMarker(point, label)) is { Rejection: null })
        {
            _status.ShowStatus($"PlaceMarker {label} applied at {point}, {ClockText.Date(World.Now)}.");
        }

        return $"{label} at {point}";
    }

    private string RemoveMarkerNearSelection()
    {
        if (SelectedPoint is not { } point)
        {
            _status.ShowStatus("Select a location near a marker first.", LineStyle.Warning);
            return "no selection";
        }

        var radius = PickRadiusPixels / _map.Transform.PixelsPerMetre;
        var nearest = World.Inspect(point, radius).NearbyMarkers
            .OrderBy(marker => Math.Pow(marker.Position.X - point.X, 2) + Math.Pow(marker.Position.Y - point.Y, 2))
            .FirstOrDefault();
        if (nearest is null)
        {
            _status.ShowStatus($"No marker within {MapScale.FormatDistance(radius)} of the selected location.", LineStyle.Warning);
            return "no marker in reach";
        }

        if (Submit(new RemoveMarker(nearest.Id)) is { Rejection: null })
        {
            _status.ShowStatus($"RemoveMarker {nearest.Id} applied, {ClockText.Date(World.Now)}.");
        }

        return nearest.Id.ToString();
    }

    /// <summary>Applies a world command through the host; a refusal by the world is shown in the status bar.</summary>
    private JournalEntry? Submit(SimCommand command)
    {
        var entry = _context.Host.TrySubmit(command);
        if (entry?.Rejection is { } reason)
        {
            _status.ShowStatus($"{command.GetType().Name} refused: {reason}", LineStyle.Warning);
        }

        _sinceRefresh = double.MaxValue;
        _map.InvalidateOverlay();
        return entry;
    }

    /// <summary>Applies a pacing command to the world's time control; a refusal is shown in the status bar.</summary>
    private bool Control(ControlCommand command)
    {
        var refusal = _context.Host.TryControl(command);
        if (refusal is not null)
        {
            _status.ShowStatus($"{command.GetType().Name} refused: {refusal}", LineStyle.Warning);
        }

        _sinceRefresh = double.MaxValue;
        return refusal is null;
    }

    private string TogglePause()
    {
        var pause = !World.TimeControl.Paused;
        Control(pause ? new Pause() : new Resume());
        _status.ShowStatus(pause ? $"Time paused. Press {KeyNames(InputActions.TimeTogglePause)} to resume." : "Time running.");
        return pause ? "pause" : "resume";
    }

    private string StepTime()
    {
        if (World.PendingDecisions.Count > 0)
        {
            _status.ShowStatus($"A decision is pending; acknowledge it first ({KeyNames(InputActions.AcknowledgeDecision)}). Time cannot step.", LineStyle.Warning);
            return "blocked by a pending decision";
        }

        Control(new StepOnce());
        _status.ShowStatus($"One step ({World.StepMs} ms) requested.");
        return "step once";
    }

    private string SetSpeed(int preset)
    {
        var speed = TimeControl.Presets[preset];
        if (Control(new SetSpeed(speed)))
        {
            _status.ShowStatus($"Speed set to {ClockText.Speed(speed)}.");
        }

        return ClockText.Speed(speed);
    }

    private string SetBreakpointAtNextMidnight()
    {
        var at = WorldCalendar.StartOfDay(World.Now).Plus(WorldCalendar.MillisecondsPerDay);
        var label = $"B{++_breakpointSerial}";
        if (Submit(new SetBreakpoint(at, label)) is { Rejection: null })
        {
            _status.ShowStatus($"Breakpoint {label} set for {ClockText.Date(at)}; time stops there until it is acknowledged.");
        }

        return $"{label} at {ClockText.Date(at)}";
    }

    private string AcknowledgeOldestDecision()
    {
        if (World.PendingDecisions.Count == 0)
        {
            _status.ShowStatus("No decision is pending.", LineStyle.Warning);
            return "none pending";
        }

        var decision = World.PendingDecisions[0];
        if (Submit(new AcknowledgeDecision(decision.Event)) is { Rejection: null })
        {
            _status.ShowStatus($"Decision \"{decision.Label}\" acknowledged; time may continue.");
        }

        return $"{decision.Event} {decision.Label}";
    }

    private string KeyNames(string action)
    {
        var keys = _context.Bindings.Get(action).Select(binding => binding.DisplayName).ToList();
        return keys.Count == 0 ? action : string.Join(" / ", keys);
    }

    private void UpdateDecisionBanner() => _decisions.Show(World.PendingDecisions, KeyNames(InputActions.AcknowledgeDecision));

    private string StepLayer(int direction)
    {
        SetLayer(_layerIndex + direction);
        return ActiveLayer.Name;
    }

    private string ToggleHud()
    {
        _hud.Visible = !_hud.Visible;
        return _hud.Visible ? "shown" : "hidden";
    }

    private string WriteManualReport()
    {
        var path = _context.Host.WriteReport("manual report");
        _status.ShowStatus(path is null ? "Could not write the failure report; see the log." : $"Failure report written: {path}",
            path is null ? LineStyle.Warning : LineStyle.Normal);
        return path ?? "failed";
    }

    private Vector2 PointerInMap(InputEvent? source) =>
        source is InputEventMouse mouse ? mouse.Position - _map.GlobalPosition : _map.GetLocalMousePosition();

    private string OverlayText() => _map.Overlay is { } overlay
        ? string.Create(CultureInfo.InvariantCulture,
            $"{overlay.Field.Key}, {Grid.Name(overlay.Level)} level, {overlay.Columns} x {overlay.Rows} samples every {overlay.SpacingM:0.##} m, last query {overlay.SampleMilliseconds:0.0} ms")
        : "none";

    private DebugHudModel BuildHudModel()
    {
        if (_sinceChecksum >= ChecksumSeconds)
        {
            _sinceChecksum = 0;
            _checksum = World.ComputeChecksum()[..12];
        }

        var transform = _map.Transform;
        var chunkLevel = _map.LevelOverride ?? (ActiveLayer.IsFieldOverlay ? _map.EffectiveLevel : GridLevel.Local);
        var chunkField = ActiveLayer.Field is { } layerField && ActiveLayer.IsAvailable && World.Atlas.IsChunked(layerField.Key) ? layerField.Key : FieldCatalog.Scratch.Key;

        // Finer than the field's native level, values come from native chunks (plus cached refinement terms).
        var native = FieldCatalog.Get(chunkField).NativeLevel;
        chunkLevel = chunkLevel > native ? native : chunkLevel;
        var recent = _context.RecentLog;
        var lastProblem = recent.LastWarningOrError;
        var control = World.TimeControl;
        return new DebugHudModel(
            World.Seed,
            _context.Selection.FixtureReference,
            World.ScenarioId.ToString(),
            World.Now,
            World.Identity.Geography.HemisphereAt(World.Extent.North / 2),
            World.StepCount,
            World.StepMs,
            control.Paused,
            control.SimMillisecondsPerRealSecond,
            control.RequestedSteps,
            [.. World.PendingDecisions],
            KeyNames(InputActions.AcknowledgeDecision),
            World.Ledger.Count,
            transform.Centre,
            transform.MetresPerPixel,
            Grid.ChunkAt(chunkLevel, transform.Centre),
            World.Atlas.ResidencyAt(chunkField, chunkLevel, transform.Centre),
            chunkField,
            World.Atlas.CacheStatistics,
            OverlayText(),
            Engine.GetFramesPerSecond(),
            _frameMs,
            _context.Host.LastAdvanceCostMs,
            _context.Host.LastAdvanceSteps,
            GC.GetTotalMemory(forceFullCollection: false),
            (long)Performance.GetMonitor(Performance.Monitor.MemoryStatic),
            (int)Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount),
            recent.WarningCount,
            recent.ErrorCount,
            lastProblem is null ? null : $"{lastProblem.Severity} {lastProblem.Subsystem}: {lastProblem.Message}",
            World.Journal.Entries.Count,
            _checksum,
            _context.Build.Configuration,
            _context.Host.Failure is not null);
    }
}
