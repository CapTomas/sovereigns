using System.Text.Json;
using Sovereigns.Client.Controls;

namespace Sovereigns.Client.Tests;

/// <summary>The rebindable input-action layer (SOV-P01-T12).</summary>
public static class InputTests
{
    public static IEnumerable<TestCase> Cases() =>
    [
        new("input: default actions and events are registered in the InputMap", DefaultsAreRegistered),
        new("input: rebind, save and load round-trip through a file; only overrides are saved", RebindRoundTrip),
        new("input: conflicts are detected and invalid rebinding changes nothing", ConflictsAndInvalidRebind),
        new("input: malformed overrides are skipped with warnings", MalformedOverridesAreSkipped),
        new("input: the help line follows the bindings", HelpFollowsBindings),
    ];

    private static Task DefaultsAreRegistered()
    {
        var bindings = InputBindings.Defaults();
        InputMapRegistrar.Apply(bindings);
        foreach (var action in InputActions.All)
        {
            Check.True(InputMap.HasAction(action.Name), $"action {action.Name} is registered");
            Check.Equal(action.DefaultBindings.Count, InputMap.ActionGetEvents(action.Name).Count, $"event count of {action.Name}");
        }

        Check.True(InputMap.EventIsAction(new InputEventKey { Keycode = Key.A, Pressed = true }, InputActions.CameraPanLeft), "A pans left");
        Check.True(InputMap.EventIsAction(new InputEventMouseButton { ButtonIndex = MouseButton.WheelUp, Pressed = true }, InputActions.CameraZoomIn), "wheel up zooms in");
        Check.True(InputMap.EventIsAction(new InputEventMouseButton { ButtonIndex = MouseButton.Middle, Pressed = true }, InputActions.CameraDrag), "middle mouse drags");
        Check.True(InputMap.EventIsAction(new InputEventKey { Keycode = Key.Q, CtrlPressed = true, Pressed = true }, InputActions.Quit, exactMatch: true), "Ctrl+Q quits");
        Check.True(!InputMap.EventIsAction(new InputEventKey { Keycode = Key.Q, Pressed = true }, InputActions.Quit, exactMatch: true), "plain Q does not quit");
        Check.True(!InputMap.EventIsAction(new InputEventKey { Keycode = Key.M, CtrlPressed = true, Pressed = true }, InputActions.PlaceMarker, exactMatch: true), "Ctrl+M is not place_marker");
        Check.Equal(0, bindings.FindConflicts().Count, "default bindings have no conflicts");
        return Task.CompletedTask;
    }

    private static Task RebindRoundTrip()
    {
        using var workspace = new Workspace();
        var path = workspace.File("input_bindings.json");
        var bindings = InputBindings.Defaults();
        bindings.Rebind(InputActions.PlaceMarker, ["key:P", "key:Shift+M"]);
        bindings.Rebind(InputActions.CameraDrag, ["mouse:right"]);
        bindings.Save(path);

        using (var document = JsonDocument.Parse(File.ReadAllText(path)))
        {
            Check.Equal(InputBindings.FileVersion, document.RootElement.GetProperty("version").GetInt32(), "the file records its format version");
            Check.Equal(2, document.RootElement.GetProperty("bindings").EnumerateObject().Count(), "only the two rebound actions are saved");
        }

        var warnings = new List<string>();
        var loaded = InputBindings.Load(path, warnings.Add);
        Check.Equal(0, warnings.Count, "no warnings loading a saved file");
        foreach (var action in InputActions.All)
        {
            Check.Equal(string.Join(",", bindings.Get(action.Name)), string.Join(",", loaded.Get(action.Name)), $"bindings of {action.Name} after load");
        }

        Check.Equal("key:P,key:Shift+M", string.Join(",", loaded.Get(InputActions.PlaceMarker)), "rebound place_marker");

        InputMapRegistrar.Apply(loaded);
        Check.Equal(2, InputMap.ActionGetEvents(InputActions.PlaceMarker).Count, "InputMap has both new place_marker events");
        Check.True(InputMap.EventIsAction(new InputEventKey { Keycode = Key.P, Pressed = true }, InputActions.PlaceMarker), "P now places markers");
        Check.True(!InputMap.EventIsAction(new InputEventKey { Keycode = Key.M, Pressed = true }, InputActions.PlaceMarker), "M no longer places markers");
        InputMapRegistrar.Apply(InputBindings.Defaults());

        Check.Equal(0, InputBindings.Load(workspace.File("missing.json"), warnings.Add).FindConflicts().Count, "a missing file means defaults");
        Check.Equal(0, warnings.Count, "a missing file is not a warning");
        return Task.CompletedTask;
    }

    private static Task ConflictsAndInvalidRebind()
    {
        var bindings = InputBindings.Defaults();
        bindings.Rebind(InputActions.RemoveMarker, ["key:M"]);
        var conflicts = bindings.FindConflicts();
        Check.Equal(1, conflicts.Count, "one conflict");
        Check.Equal("key:M", conflicts[0].Binding.ToString(), "conflicting binding");
        Check.Equal($"{InputActions.PlaceMarker},{InputActions.RemoveMarker}", string.Join(",", conflicts[0].Actions), "conflicting actions");

        var before = string.Join(",", bindings.Get(InputActions.PlaceMarker));
        Throws<ArgumentException>(() => bindings.Rebind(InputActions.PlaceMarker, ["key:P", "key:NotAKey"]), "an invalid binding is rejected");
        Throws<ArgumentException>(() => bindings.Rebind("no_such_action", ["key:P"]), "an unknown action is rejected");
        Check.Equal(before, string.Join(",", bindings.Get(InputActions.PlaceMarker)), "a rejected rebind changes nothing");
        return Task.CompletedTask;
    }

    private static Task MalformedOverridesAreSkipped()
    {
        using var workspace = new Workspace();
        var path = workspace.File("input_bindings.json");
        File.WriteAllText(path, """
            {
              "version": 1,
              "bindings": {
                "bogus_action": ["key:X"],
                "camera_pan_left": ["key:NotAKey"],
                "place_marker": ["key:P", "mouse:nope"],
                "remove_marker": "key:Delete",
                "toggle_hud": [],
                "layer_next": [7]
              }
            }
            """);
        var warnings = new List<string>();
        var loaded = InputBindings.Load(path, warnings.Add);
        Check.Equal(5, warnings.Count, $"warnings: {string.Join(" | ", warnings)}");
        Check.Equal("key:A,key:Left", string.Join(",", loaded.Get(InputActions.CameraPanLeft)), "all-malformed override keeps defaults");
        Check.Equal("key:P", string.Join(",", loaded.Get(InputActions.PlaceMarker)), "the valid binding of a partly malformed override applies");
        Check.Equal("key:Delete,key:Backspace", string.Join(",", loaded.Get(InputActions.RemoveMarker)), "a non-list override keeps defaults");
        Check.Equal(0, loaded.Get(InputActions.ToggleHud).Count, "an explicit empty list unbinds the action");
        Check.Equal("key:Bracketright", string.Join(",", loaded.Get(InputActions.LayerNext)), "a non-string binding keeps defaults");

        foreach (var (text, expected) in new[]
        {
            ("""{ "camera_pan_left": ["key:Q"] }""", "expected {\"version\""),
            ("""{ "version": 2, "bindings": { "camera_pan_left": ["key:Q"] } }""", "version 2 is not supported"),
        })
        {
            File.WriteAllText(path, text);
            warnings.Clear();
            loaded = InputBindings.Load(path, warnings.Add);
            Check.Equal(1, warnings.Count, $"one warning for {text}");
            Check.Contains(warnings[0], expected, "the warning names the format problem");
            Check.Equal("key:A,key:Left", string.Join(",", loaded.Get(InputActions.CameraPanLeft)), "an unsupported file means defaults");
        }

        File.WriteAllText(path, "{ not json");
        warnings.Clear();
        loaded = InputBindings.Load(path, warnings.Add);
        Check.Equal(1, warnings.Count, "unreadable file warns once");
        Check.Equal("key:Home,key:R", string.Join(",", loaded.Get(InputActions.CameraRecenter)), "unreadable file means defaults");
        return Task.CompletedTask;
    }

    private static Task HelpFollowsBindings()
    {
        var bindings = InputBindings.Defaults();
        Check.Contains(HelpText.Build(bindings), "Place marker: M", "default help");
        bindings.Rebind(InputActions.PlaceMarker, ["key:P"]);
        var help = HelpText.Build(bindings);
        Check.Contains(help, "Place marker: P", "rebound help");
        Check.True(!help.Contains("Place marker: M", StringComparison.Ordinal), "old binding no longer listed");
        return Task.CompletedTask;
    }

    private static void Throws<T>(Action action, string what)
        where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }

        throw new CheckFailedException($"{what}: expected {typeof(T).Name}");
    }
}
