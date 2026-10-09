using System.Globalization;
using Sovereigns.Client.Boot;
using Sovereigns.Client.Controls;
using Sovereigns.Client.Diagnostics;
using Sovereigns.Client.Simulation;
using Sovereigns.Client.View;
using Sovereigns.Simulation;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Scenarios;
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
/// The reusable 2D inspection shell (SOV-P01-T10, T11): pan/zoom map, layer list, location inspector, debug HUD and
/// the dispatch of input actions. Every view reads the one <see cref="World"/> held by the host; placing or
/// removing a marker only submits a command, so the world journals it with its sequence and time.
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
    private readonly DebugHud _hud = new();
    private readonly LayerLegend _legend = new();
    private readonly StatusBar _status = new();
    private readonly ErrorBanner _banner = new();
    private int _layerIndex;
    private int _markerSerial;
    private bool _dragging;
    private bool _failureShown;
    private double _sinceRefresh = double.MaxValue;
    private double _sinceChecksum = double.MaxValue;
    private double _frameMs = 16.7;
    private string _checksum = "";

    public InspectionShell(ShellContext context)
    {
        _context = context;
    }

    public WorldView Map => _map;

    public InspectorPanel Inspector => _inspector;

    public DebugHud Hud => _hud;

    public LayerLegend Legend => _legend;

    public StatusBar Status => _status;

    public ErrorBanner Banner => _banner;

    public WorldPoint? SelectedPoint { get; private set; }

    public MapLayer ActiveLayer => MapLayers.All[_layerIndex];

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
        left.AddChild(_layers);
        body.AddChild(left);

        _map.World = World;
        _map.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _map.SizeFlagsVertical = SizeFlags.ExpandFill;
        _map.AddChild(_hud);
        _map.AddChild(_legend);
        body.AddChild(_map);
        body.AddChild(_inspector);
        root.AddChild(_status);
        AddChild(_banner);

        _layers.LayerChosen += SetLayer;
        _status.SetHelp(HelpText.Build(_context.Bindings));
        ApplyLaunchArguments();
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

        _sinceRefresh += delta;
        _sinceChecksum += delta;
        if (_sinceRefresh >= RefreshSeconds)
        {
            RefreshPanels();
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
        var count = MapLayers.All.Count;
        _layerIndex = ((index % count) + count) % count;
        _map.ActiveLayer = ActiveLayer;
        _layers.Highlight(_layerIndex);
        _legend.Show(ActiveLayer, World.Markers.Count);
    }

    /// <summary>Re-queries the world and redraws the inspector, legend and HUD.</summary>
    public void RefreshPanels()
    {
        _sinceRefresh = 0;
        if (SelectedPoint is { } point)
        {
            var radius = PickRadiusPixels / _map.Transform.PixelsPerMetre;
            _inspector.Show(World.Inspect(point, radius), radius);
        }
        else
        {
            _inspector.ShowNothing();
        }

        _legend.Show(ActiveLayer, World.Markers.Count);
        _hud.Update(BuildHudModel());
    }

    private void ApplyLaunchArguments()
    {
        var arguments = _context.Arguments;
        if (arguments?.Layer is { } layerName)
        {
            if (MapLayers.IndexOf(layerName) is >= 0 and var index)
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

        if (arguments?.Marker is { } marker)
        {
            _markerSerial++;
            _context.Host.TrySubmit(new PlaceMarker(marker, $"M{_markerSerial}"));
        }
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

    private string PlaceMarkerAtSelection()
    {
        if (SelectedPoint is not { } point)
        {
            _status.ShowStatus("Select a location first (left click), then place a marker.", LineStyle.Warning);
            return "no selection";
        }

        var label = $"M{++_markerSerial}";
        if (_context.Host.TrySubmit(new PlaceMarker(point, label)))
        {
            _status.ShowStatus($"PlaceMarker {label} submitted at {point}; the world applies it at its next step ({World.StepMs} ms).");
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

        _context.Host.TrySubmit(new RemoveMarker(nearest.Id));
        _status.ShowStatus($"RemoveMarker {nearest.Id} submitted; the world applies it at its next step.");
        return nearest.Id.ToString();
    }

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

    private DebugHudModel BuildHudModel()
    {
        if (_sinceChecksum >= ChecksumSeconds)
        {
            _sinceChecksum = 0;
            _checksum = World.ComputeChecksum()[..12];
        }

        var transform = _map.Transform;
        var recent = _context.RecentLog;
        var lastProblem = recent.LastWarningOrError;
        return new DebugHudModel(
            World.Seed,
            _context.Selection.FixtureReference,
            World.ScenarioId.ToString(),
            World.Now,
            World.StepCount,
            World.StepMs,
            transform.Centre,
            transform.MetresPerPixel,
            Engine.GetFramesPerSecond(),
            _frameMs,
            _context.Host.LastStepCostMs,
            GC.GetTotalMemory(forceFullCollection: false),
            (long)Performance.GetMonitor(Performance.Monitor.MemoryStatic),
            (int)Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount),
            recent.WarningCount,
            recent.ErrorCount,
            lastProblem is null ? null : $"{lastProblem.Severity} {lastProblem.Subsystem}: {lastProblem.Message}",
            World.Journal.Entries.Count,
            World.PendingCommandCount,
            _checksum,
            _context.Build.Configuration,
            _context.Host.Failure is not null);
    }
}
