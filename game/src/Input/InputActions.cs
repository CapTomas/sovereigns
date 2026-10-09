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
    public const string Quit = "quit";

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
        new("HUD", [ToggleHud]),
        new("Report", [FailureReport]),
        new("Quit", [Quit]),
    ];
}
