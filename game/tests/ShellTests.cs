using Sovereigns.Client.Boot;
using Sovereigns.Client.Controls;
using Sovereigns.Client.Ui;
using Sovereigns.Client.View;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Fields;
using Sovereigns.Simulation.Scenarios;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Client.Tests;

/// <summary>Boots the real Main scene and drives it through the real input path.</summary>
public sealed class ShellTests(ClientTestRunner runner)
{
    private const string MainScene = "res://scenes/Main.tscn";

    public IEnumerable<TestCase> Cases() =>
    [
        new("shell: boots F-EMPTY and reports its seed and scenario in the HUD", BootsFixture),
        new("shell: select and place marker through input are journaled and replay to the same checksum", InputIsJournaledAndReplays),
        new("shell: layers list every family, unavailable layers show No data, and the inspector lists 16 families", LayersAndInspector),
        new("shell: wheel zoom anchors at the cursor, middle-drag pans and recenter fits the world", CameraInput),
        new("shell: a bad launch argument or scenario is shown, not fatal", LaunchProblemsAreShown),
        new("shell: an injected failure writes a report, stops the host and shows a banner; replay reproduces it", FailureIsReported),
        new("shell: freeing the shell returns the orphan node count to its baseline and shuts down cleanly", NoOrphansAfterFree),
        new("shell: the close request and the quit action each log the shutdown checksum and request exit (must run last)", CloseAndQuit),
    ];

    private static double Orphans() => Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount);

    private async Task<Main> Boot(Workspace workspace, params string[] extra)
    {
        LaunchArguments.InjectForNextBoot(["--log", workspace.File("run.jsonl"), "--reports", workspace.File("reports"), "--bindings", workspace.File("no_overrides.json"), .. extra]);
        var main = GD.Load<PackedScene>(MainScene).Instantiate<Main>();
        runner.AddChild(main);
        await runner.Frames(4);
        // Cases drive the simulation clock explicitly; wall-clock pacing is covered by the pacing cases.
        main.SetProcess(false);
        return main;
    }

    private async Task Dispose(Main main)
    {
        main.Free();
        await runner.Frames(2);
    }

    private async Task BootsFixture()
    {
        using var workspace = new Workspace();
        var main = await Boot(workspace, "--fixture", "F-EMPTY");
        try
        {
            Check.True(main.Launch is { Succeeded: true }, "launch succeeded");
            Check.Equal(TestWorlds.FixtureSeed, main.Host!.World.Seed, "host world seed");
            Check.Equal(TestWorlds.FixtureScenario, main.Host.World.ScenarioId.ToString(), "host world scenario");
            main.Shell!.RefreshPanels();
            var hud = main.Shell.Hud.Text;
            Check.Contains(hud, "seed: 20261009", "HUD seed");
            Check.Contains(hud, "fixture: F-EMPTY@1", "HUD fixture");
            Check.Contains(hud, "scenario: core:scenario/empty_world", "HUD scenario");
            Check.Contains(hud, "active world chunk: unavailable", "HUD chunk line");
            Check.Contains(hud, "state checksum: ", "HUD checksum");
            Check.Contains(main.Shell.Status.HelpText, "Select: LMB", "help line from the bindings");
        }
        finally
        {
            await Dispose(main);
        }
    }

    private async Task InputIsJournaledAndReplays()
    {
        using var workspace = new Workspace();
        var main = await Boot(workspace, "--fixture", "F-EMPTY", "--record", workspace.File("journal.jsonl"));
        var scenario = main.Launch!.Selection!.Scenario;
        var world = main.Host!.World;
        string liveChecksum;
        SimTime end;
        try
        {
            var shell = main.Shell!;
            var cursor = shell.Map.GlobalPosition + (shell.Map.Size / 2) + new Vector2(37, -21);

            Check.True(shell.SelectedPoint is null, "nothing selected at start");
            Click(cursor);
            await runner.Frames(2);
            var selected = shell.SelectedPoint ?? throw new CheckFailedException("left click selected a location");
            var expected = shell.Map.Transform.ToWorld(cursor - shell.Map.GlobalPosition);
            Check.Near(expected.X, selected.X, 1e-6, "selected x is the point under the cursor");
            Check.Near(expected.Y, selected.Y, 1e-6, "selected y is the point under the cursor");

            Press(Key.M);
            await runner.Frames(2);
            Check.Equal(0, world.Journal.Entries.Count, "the command waits for the next simulation step");
            Check.Equal(1, world.PendingCommandCount, "one pending command");
            main.Host.Advance(1.0);
            var entry = world.Journal.Entries.Single();
            Check.True(entry.Command is PlaceMarker place && place.Position == selected && place.Label == "M1", "journal holds PlaceMarker at the selected point");
            Check.True(entry.Rejection is null, "world accepted the marker");
            Check.Equal(1, world.Markers.Count, "marker exists in the world");

            Press(Key.Delete);
            await runner.Frames(2);
            main.Host.Advance(2.0);
            Check.True(world.Journal.Entries[1].Command is RemoveMarker, "remove_marker journals RemoveMarker");
            Check.Equal(0, world.Markers.Count, "marker removed");

            Press(Key.M);
            await runner.Frames(2);
            main.Host.Advance(1.0);
            Check.Equal(3, world.Journal.Entries.Count, "three commands journaled");

            var actions = main.Inputs.Snapshot().Select(handled => handled.Action).ToList();
            Check.True(actions.Contains(InputActions.SelectLocation) && actions.Contains(InputActions.PlaceMarker) && actions.Contains(InputActions.RemoveMarker),
                $"input history lists the handled actions: {string.Join(",", actions)}");

            // The journal replays to the state the live client reached.
            liveChecksum = world.ComputeChecksum();
            end = world.Now;
            Check.Equal(liveChecksum, Replay.Run(scenario, world.Seed, world.Journal.Entries, end).ComputeChecksum(), "replay checksum equals the live checksum");
        }
        finally
        {
            await Dispose(main);
        }

        using var file = File.OpenText(workspace.File("journal.jsonl"));
        var recorded = CommandJournal.ReadJsonLines(file, "journal.jsonl");
        Check.Equal(3, recorded.Count, "--record wrote the journal at exit");
        Check.Equal(liveChecksum, Replay.Run(scenario, world.Seed, recorded, end).ComputeChecksum(), "replaying the recorded file matches");
    }

    private async Task LayersAndInspector()
    {
        using var workspace = new Workspace();
        var main = await Boot(workspace, "--fixture", "F-EMPTY");
        try
        {
            var shell = main.Shell!;
            Check.Equal(FieldFamilies.All.Count + 1, MapLayers.All.Count, "layer list: two drawable layers plus every family except geographic reference");
            Check.Equal(2, MapLayers.All.Count(layer => layer.IsAvailable), "two layers available");
            Check.True(MapLayers.All.Where(layer => !layer.IsAvailable).All(layer => layer.ListText.EndsWith("(unavailable)", StringComparison.Ordinal)), "unavailable layers are marked");

            Click(shell.Map.GlobalPosition + (shell.Map.Size / 2));
            await runner.Frames(2);
            shell.RefreshPanels();
            var inspection = shell.Inspector.Current!;
            Check.Equal(16, inspection.Families.Count, "inspector lists every family");
            Check.Equal(15, inspection.Families.Count(family => family.Availability == FieldAvailability.Unavailable), "fifteen unavailable families");
            var text = string.Join('\n', shell.Inspector.Lines.Select(line => line.Text));
            foreach (var family in FieldFamilies.All)
            {
                Check.Contains(text, family.Name, "inspector shows family");
            }

            Check.True(shell.Inspector.Lines.Any(line => line.Style == LineStyle.Dim && line.Text.Contains("Phase 07", StringComparison.Ordinal)), "unavailable readings are dim and name their phase");
            Check.True(!text.Contains("fake", StringComparison.OrdinalIgnoreCase), "no invented values");

            Check.Equal(MapLayer.WorldFrameKey, shell.ActiveLayer.Key, "starts on the world frame layer");
            Press(Key.Bracketright);
            await runner.Frames(2);
            Check.Equal(MapLayer.DeveloperMarkersKey, shell.ActiveLayer.Key, "layer_next selects the next layer");
            Press(Key.Bracketright);
            await runner.Frames(2);
            Check.True(!shell.ActiveLayer.IsAvailable, "third layer is unavailable");
            Check.Contains(shell.Legend.Text, "No data — unavailable:", "legend names the missing data");
            Check.Contains(shell.Legend.Text, shell.ActiveLayer.UnavailableReason!, "legend gives owner and phase");
            Press(Key.Bracketleft);
            await runner.Frames(1);
            Check.Equal(MapLayer.DeveloperMarkersKey, shell.ActiveLayer.Key, "layer_previous returns");

            Press(Key.F3);
            await runner.Frames(1);
            Check.True(!shell.Hud.Visible, "F3 hides the HUD");
            Press(Key.F3);
            await runner.Frames(1);
            Check.True(shell.Hud.Visible, "F3 shows the HUD again");

            var before = shell.Map.Transform.PixelsPerMetre;
            shell.Dispatch(InputActions.CameraZoomIn);
            Check.True(shell.Map.Transform.PixelsPerMetre > before, "zoom in changes the scale");
            shell.Dispatch(InputActions.CameraRecenter);
            Check.Near(before, shell.Map.Transform.PixelsPerMetre, before * 1e-9, "recenter fits the world again");
        }
        finally
        {
            await Dispose(main);
        }
    }

    private async Task CameraInput()
    {
        using var workspace = new Workspace();
        var main = await Boot(workspace, "--fixture", "F-EMPTY");
        try
        {
            var map = main.Shell!.Map;
            var fitted = map.Transform;
            var cursor = map.GlobalPosition + new Vector2(200, 150);
            var local = cursor - map.GlobalPosition;
            var under = fitted.ToWorld(local);

            foreach (var button in new[] { MouseButton.WheelUp, MouseButton.WheelUp, MouseButton.WheelDown })
            {
                PushMouse(button, true, cursor);
            }

            var zoomed = map.Transform;
            Check.Near(fitted.PixelsPerMetre * 1.25, zoomed.PixelsPerMetre, 1e-12, "two wheel-ups and one wheel-down net one zoom step");
            var (sx, sy) = zoomed.ToScreenPixels(under);
            Check.Near(local.X, sx, 1e-6, "the world point under the cursor stays at the cursor (x)");
            Check.Near(local.Y, sy, 1e-6, "the world point under the cursor stays at the cursor (y)");

            PushMouse(MouseButton.Middle, true, cursor);
            PushMotion(cursor, new Vector2(60, -30));
            PushMouse(MouseButton.Middle, false, cursor);
            var dragged = map.Transform;
            Check.Near(zoomed.CentreX - 60 / zoomed.PixelsPerMetre, dragged.CentreX, 1e-6, "dragging right moves the view west");
            Check.Near(zoomed.CentreY - 30 / zoomed.PixelsPerMetre, dragged.CentreY, 1e-6, "dragging up moves the view south");
            PushMotion(cursor, new Vector2(500, 500));
            Check.Equal(dragged.Centre, map.Transform.Centre, "motion after the button is released does not pan");

            Press(Key.Home);
            Check.Near(fitted.PixelsPerMetre, map.Transform.PixelsPerMetre, 1e-12, "Home fits the world again");
            Check.Near(fitted.CentreX, map.Transform.CentreX, 1e-9, "Home recentres x");
        }
        finally
        {
            await Dispose(main);
        }
    }

    private async Task LaunchProblemsAreShown()
    {
        using var workspace = new Workspace();
        var main = await Boot(workspace, "--scenario", "core:scenario/no_such_world", "--seed", "7");
        try
        {
            Check.True(main.Shell is null && main.Host is null, "no world is created for a bad scenario");
            Check.True(main.Launch is { Succeeded: false }, "launch failed");
            Check.Contains(string.Join(" ", main.Launch!.Problems), "no_such_world", "problem names the scenario");
            Check.True(main.GetChildren().OfType<ErrorBanner>().Any(banner => banner.Visible && banner.Text.Contains("no_such_world", StringComparison.Ordinal)), "problem is on screen");
        }
        finally
        {
            await Dispose(main);
        }

        var badArgument = await Boot(workspace, "--no-such-option", "1");
        try
        {
            Check.True(badArgument.Launch is { Succeeded: false }, "an unknown launch argument is reported");
            Check.True(badArgument.GetChildren().OfType<ErrorBanner>().Any(banner => banner.Text.Contains("--no-such-option", StringComparison.Ordinal)), "unknown argument is on screen");
        }
        finally
        {
            await Dispose(badArgument);
        }
    }

    private async Task FailureIsReported()
    {
        using var workspace = new Workspace();
        var main = await Boot(workspace, "--fixture", "F-EMPTY", "--fail-at-ms", "2000", "--marker", "40000,60000");
        try
        {
            var host = main.Host!;
            host.Advance(5.0);
            Check.True(host.Failure is { ReportPath: not null }, "the host stopped and wrote a report");
            var failure = host.Failure!;
            Check.True(File.Exists(failure.ReportPath), "report file exists");
            Check.Contains(failure.ReplayCommand, "replay", "reproduction command");
            Check.Equal(2000L, host.World.Now.Milliseconds, "the world stopped at the injected time");
            host.Advance(5.0);
            Check.Equal(2000L, host.World.Now.Milliseconds, "a failed host stops advancing");

            await runner.Frames(3);
            Check.True(main.Shell!.Banner.Visible && main.Shell.Banner.Text.Contains(failure.ReportPath!, StringComparison.Ordinal), "banner shows the report path");

            using var stream = File.OpenRead(failure.ReportPath!);
            var report = FailureReport.ReadFrom(stream);
            Check.Equal("client", report.Host, "report host");
            Check.Equal(2000L, report.SimTimeMs, "report time");
            Check.Equal(1, report.Commands.Count, "report holds the journal");
            var replayed = Replay.Run(main.Launch!.Selection!.Scenario, report.Seed, report.Commands, new SimTime(report.SimTimeMs));
            Check.Equal(report.StateChecksum, replayed.ComputeChecksum(), "replaying the report reproduces the checksum");

            var manual = new Workspace();
            try
            {
                var other = TestWorlds.CreateHost(TestWorlds.LaunchFixture().World!, manual.File("r"));
                Check.True(other.WriteReport("manual report") is { } path && File.Exists(path), "a manual report is written without stopping");
                Check.True(other.Failure is null, "manual report does not stop the host");
            }
            finally
            {
                manual.Dispose();
            }
        }
        finally
        {
            await Dispose(main);
        }
    }

    private async Task NoOrphansAfterFree()
    {
        using var workspace = new Workspace();
        await runner.Frames(2);
        var baseline = Orphans();
        var main = await Boot(workspace, "--fixture", "F-EMPTY", "--select", "50000,50000");
        await runner.Frames(10);
        main.Shell!.RefreshPanels();
        await Dispose(main);
        await runner.Frames(2);
        Check.Equal(baseline, Orphans(), "orphan node count after freeing the shell");
        var log = File.ReadAllLines(workspace.File("run.jsonl"));
        Check.True(log.Any(line => line.Contains("\"message\":\"shutdown\"", StringComparison.Ordinal) && line.Contains("\"checksum\"", StringComparison.Ordinal)), "the shutdown record with a checksum was logged");
    }

    private async Task CloseAndQuit()
    {
        // Asserts run synchronously after each trigger because the engine stops iterating once exit is requested.
        using var closeWorkspace = new Workspace();
        using var quitWorkspace = new Workspace();
        var closed = await Boot(closeWorkspace, "--fixture", "F-EMPTY");
        var quitted = await Boot(quitWorkspace, "--fixture", "F-EMPTY");
        Check.True(!closed.IsShutDown && !quitted.IsShutDown, "running clients are not shut down");

        closed.Notification((int)Node.NotificationWMCloseRequest);
        Check.True(closed.IsShutDown && !quitted.IsShutDown, "the window close request shuts down only its client");
        runner.GetViewport().PushInput(new InputEventKey { Keycode = Key.Q, CtrlPressed = true, Pressed = true }, inLocalCoords: true);
        Check.True(quitted.IsShutDown, "Ctrl+Q shuts down the client");

        foreach (var workspace in new[] { closeWorkspace, quitWorkspace })
        {
            var shutdown = File.ReadAllLines(workspace.File("run.jsonl")).Where(line => line.Contains("\"message\":\"shutdown\"", StringComparison.Ordinal)).ToList();
            Check.Equal(1, shutdown.Count, "exactly one shutdown record");
            Check.Contains(shutdown[0], "\"checksum\":", "shutdown record carries the state checksum");
        }

        closed.Free();
        quitted.Free();
    }

    // A headless display server has no window to deliver Input.ParseInputEvent to, so events enter at the
    // root viewport in its own coordinates. From there they follow the real route: _Input, GUI, then _UnhandledInput.
    private void Click(Vector2 position)
    {
        PushMouse(MouseButton.Left, true, position);
        PushMouse(MouseButton.Left, false, position);
    }

    private void PushMouse(MouseButton button, bool pressed, Vector2 position) =>
        runner.GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = button, Pressed = pressed, Position = position, GlobalPosition = position }, inLocalCoords: true);

    private void PushMotion(Vector2 position, Vector2 relative) =>
        runner.GetViewport().PushInput(new InputEventMouseMotion { Position = position, GlobalPosition = position, Relative = relative, ButtonMask = MouseButtonMask.Middle }, inLocalCoords: true);

    private void Press(Key key)
    {
        foreach (var pressed in new[] { true, false })
        {
            runner.GetViewport().PushInput(new InputEventKey { Keycode = key, Pressed = pressed }, inLocalCoords: true);
        }
    }
}
