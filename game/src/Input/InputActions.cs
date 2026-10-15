namespace Sovereigns.Client.Controls;

/// <summary>One input action: its stable name, what it does and the bindings it has until the player rebinds it.</summary>
public sealed record ActionDefinition(string Name, string Description, IReadOnlyList<string> DefaultBindings, bool Repeats = false);

/// <summary>One group in the bottom help line: a label and the actions whose bindings it lists.</summary>
public sealed record HelpEntry(string Label, IReadOnlyList<string> Actions);

/// <summary>
/// Single catalog of the client's input actions and default bindings. Actions are registered into Godot's
/// InputMap at startup (see <see cref="InputMapRegistrar"/>), never in project.godot, so rebinding has one owner.
/// The namespace is Controls because a namespace named Input would hide Godot's Input class for the whole client.
/// </summary>
public static class InputActions
{
    public const string CameraPanLeft = "camera_pan_left";
    public const string CameraPanRight = "camera_pan_right";
    public const string CameraPanUp = "camera_pan_up";
    public const string CameraPanDown = "camera_pan_down";
    public const string CameraZoomIn = "camera_zoom_in";
    public const string CameraZoomOut = "camera_zoom_out";
    public const string CameraDrag = "camera_drag";
    public const string CameraRecenter = "camera_recenter";
    public const string SelectLocation = "select_location";
    public const string PlaceMarker = "place_marker";
    public const string RemoveMarker = "remove_marker";
    public const string LayerNext = "layer_next";
    public const string LayerPrevious = "layer_previous";
    public const string ToggleHud = "toggle_hud";
    public const string FailureReport = "failure_report";
    public const string TimeTogglePause = "time_toggle_pause";
    public const string TimeStep = "time_step";
    public const string TimeSpeed1 = "time_speed_1";
    public const string TimeSpeed2 = "time_speed_2";
    public const string TimeSpeed3 = "time_speed_3";
    public const string TimeSpeed4 = "time_speed_4";
    public const string TimeSpeed5 = "time_speed_5";
    public const string SetBreakpoint = "set_breakpoint";
    public const string AcknowledgeDecision = "acknowledge_decision";
    public const string LevelCoarser = "level_coarser";
    public const string LevelFiner = "level_finer";
    public const string ToggleSurveyLines = "toggle_survey_lines";
    public const string ToggleResidency = "toggle_residency";
    public const string PaintScratch = "paint_scratch";
    public const string PaintRadiusUp = "paint_radius_up";
    public const string PaintRadiusDown = "paint_radius_down";
    public const string PaintValueUp = "paint_value_up";
    public const string PaintValueDown = "paint_value_down";
    public const string SurveyAddPoint = "survey_add_point";
    public const string SurveyFinish = "survey_finish";
    public const string SurveyCancel = "survey_cancel";
    public const string SurveyRemove = "survey_remove";
    public const string Quit = "quit";

    /// <summary>The speed actions in preset order; action <c>i</c> selects <c>TimeControl.Presets[i]</c>.</summary>
    public static IReadOnlyList<string> TimeSpeeds { get; } = [TimeSpeed1, TimeSpeed2, TimeSpeed3, TimeSpeed4, TimeSpeed5];

    /// <summary>The preset index of a speed action, or -1 when <paramref name="action"/> is not one.</summary>
    public static int SpeedPresetOf(string action)
    {
        for (var i = 0; i < TimeSpeeds.Count; i++)
        {
            if (TimeSpeeds[i] == action)
            {
                return i;
            }
        }

        return -1;
    }

    public static IReadOnlyList<ActionDefinition> All { get; } =
    [
        new(CameraPanLeft, "Pan the map left", ["key:A", "key:Left"]),
        new(CameraPanRight, "Pan the map right", ["key:D", "key:Right"]),
        new(CameraPanUp, "Pan the map north", ["key:W", "key:Up"]),
        new(CameraPanDown, "Pan the map south", ["key:S", "key:Down"]),
        new(CameraZoomIn, "Zoom in at the cursor", ["mouse:wheel_up", "key:Equal"], Repeats: true),
        new(CameraZoomOut, "Zoom out at the cursor", ["mouse:wheel_down", "key:Minus"], Repeats: true),
        new(CameraDrag, "Hold and move the mouse to drag the map", ["mouse:middle"]),
        new(CameraRecenter, "Fit the whole world in view", ["key:Home", "key:R"]),
        new(SelectLocation, "Select the world location under the cursor", ["mouse:left"]),
        new(PlaceMarker, "Place a developer marker at the selected location", ["key:M"]),
        new(RemoveMarker, "Remove the marker nearest the selected location", ["key:Delete", "key:Backspace"]),
        new(LayerNext, "Select the next layer", ["key:Bracketright"]),
        new(LayerPrevious, "Select the previous layer", ["key:Bracketleft"]),
        new(ToggleHud, "Show or hide the debug HUD", ["key:F3"]),
        new(FailureReport, "Write a manual failure report without stopping", ["key:F9"]),
        new(TimeTogglePause, "Pause or resume simulation time", ["key:Space"]),
        new(TimeStep, "Advance exactly one simulation step (also while paused)", ["key:Period"]),
        new(TimeSpeed1, "Run at 1 simulated second per real second", ["key:Key1"]),
        new(TimeSpeed2, "Run at 1 simulated minute per real second", ["key:Key2"]),
        new(TimeSpeed3, "Run at 1 simulated hour per real second", ["key:Key3"]),
        new(TimeSpeed4, "Run at 1 simulated day per real second", ["key:Key4"]),
        new(TimeSpeed5, "Run at 1 simulated week per real second", ["key:Key5"]),
        new(SetBreakpoint, "Stop time at the next standard-time midnight until acknowledged", ["key:B"]),
        new(AcknowledgeDecision, "Acknowledge the oldest pending decision so time can continue", ["key:Enter"]),
        new(LevelCoarser, "Sample and inspect at the next coarser atlas level (automatic after macro)", ["key:Pageup"]),
        new(LevelFiner, "Sample and inspect at the next finer atlas level (automatic after tactical)", ["key:Pagedown"]),
        new(ToggleSurveyLines, "Show or hide survey lines", ["key:V"]),
        new(ToggleResidency, "Show or hide the chunk-residency overlay", ["key:C"]),
        new(PaintScratch, "Paint the developer scratch field at the cursor with the current radius and value", ["key:P"]),
        new(PaintRadiusUp, "Increase the scratch paint radius", ["key:Shift+Equal"]),
        new(PaintRadiusDown, "Decrease the scratch paint radius", ["key:Shift+Minus"]),
        new(PaintValueUp, "Raise the scratch paint value by 0.1", ["key:Shift+Bracketright"]),
        new(PaintValueDown, "Lower the scratch paint value by 0.1", ["key:Shift+Bracketleft"]),
        new(SurveyAddPoint, "Add the cursor position to the survey line being drawn", ["key:L"]),
        new(SurveyFinish, "Draw the pending survey line (at least two points)", ["key:K"]),
        new(SurveyCancel, "Drop the pending survey line", ["key:J"]),
        new(SurveyRemove, "Remove the survey line nearest the selected location", ["key:X"]),
        new(Quit, "Quit the client", ["key:Ctrl+Q", "key:Meta+Q"]),
    ];

    public static IReadOnlyList<HelpEntry> Help { get; } =
    [
        new("Pan", [CameraPanUp, CameraPanLeft, CameraPanDown, CameraPanRight]),
        new("Drag", [CameraDrag]),
        new("Zoom in/out", [CameraZoomIn, CameraZoomOut]),
        new("Fit world", [CameraRecenter]),
        new("Select", [SelectLocation]),
        new("Place marker", [PlaceMarker]),
        new("Remove marker", [RemoveMarker]),
        new("Layer prev/next", [LayerPrevious, LayerNext]),
        new("Pause", [TimeTogglePause]),
        new("Step", [TimeStep]),
        new("Speed", [TimeSpeed1, TimeSpeed2, TimeSpeed3, TimeSpeed4, TimeSpeed5]),
        new("Breakpoint", [SetBreakpoint]),
        new("Acknowledge", [AcknowledgeDecision]),
        new("Level coarser/finer", [LevelCoarser, LevelFiner]),
        new("Survey lines/residency", [ToggleSurveyLines, ToggleResidency]),
        new("Paint", [PaintScratch]),
        new("Radius -/+", [PaintRadiusDown, PaintRadiusUp]),
        new("Value -/+", [PaintValueDown, PaintValueUp]),
        new("Survey add/finish/cancel/remove", [SurveyAddPoint, SurveyFinish, SurveyCancel, SurveyRemove]),
        new("HUD", [ToggleHud]),
        new("Report", [FailureReport]),
        new("Quit", [Quit]),
    ];
}
