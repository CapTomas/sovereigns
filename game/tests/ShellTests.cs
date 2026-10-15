using Sovereigns.Client.Boot;
using Sovereigns.Client.Controls;
using Sovereigns.Client.Ui;
using Sovereigns.Client.View;
using Sovereigns.Simulation.Atlas;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Fields;
using Sovereigns.Simulation.Kernel;
using Sovereigns.Simulation.Scenarios;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Client.Tests;

/// <summary>Boots the real Main scene and drives it through the real input path.</summary>
public sealed partial class ShellTests(ClientTestRunner runner)
{
    private const string MainScene = "res://scenes/Main.tscn";

    public IEnumerable<TestCase> Cases() =>
    [
        new("shell: boots F-EMPTY and reports its seed and scenario in the HUD", BootsFixture),
        new("shell: select and place marker through input are journaled and replay to the same checksum", InputIsJournaledAndReplays),
        new("shell: layers list every field by family, unavailable layers show No data, and the inspector lists 17 families", LayersAndInspector),
        new("shell: wheel zoom anchors at the cursor, middle-drag pans and recenter fits the world", CameraInput),
        new("shell: a bad launch argument or scenario is shown, not fatal", LaunchProblemsAreShown),
        new("shell: time actions set pause, step and speed on the world's time control, never enter the journal, and are kept in the input history", TimeActionsControlPacing),
        new("shell: --paused and --speed start the world paused at the chosen speed", StartupTimeOptions),
        new("shell: B stops time at the next midnight even at 1 week/s, shows the pending decision, and Enter acknowledges it", BreakpointStopsTimeUntilAcknowledged),
        .. AtlasCases(),
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
            Check.Contains(hud, "fixture: F-EMPTY@2", "HUD fixture");
            Check.Contains(hud, "scenario: core:scenario/empty_world", "HUD scenario");
            Check.Contains(hud, "date: 1 Mar 1203, 06:00:00", "HUD standard date and time, not raw milliseconds");
            Check.Contains(hud, "season: Spring (northern hemisphere)", "HUD season and hemisphere");
            Check.Contains(hud, "time: running at 1 s/s", "HUD time-control state");
            Check.Contains(hud, "pending decisions: none", "HUD pending decisions");
            Check.Contains(hud, "ledger events: 1", "HUD ledger count");
            Check.Contains(hud, "steps: 0", "HUD step count");
            Check.True(!hud.Contains("SOV-P02-T11", StringComparison.Ordinal), "the time-controls placeholder is gone");
            Check.Contains(main.Shell.Events.Title, "Recent events (1 of 1)", "events panel title");
            Check.Contains(main.Shell.Events.Lines[0].Text, "WorldCreated", "events panel lists the creation event");
            Check.Contains(hud, "active chunk: local chunk (48, 48) (Unloaded) of scratch", "HUD chunk line: the chunk under the camera centre, not yet resident");
            Check.Contains(hud, "cache: 0 chunks, 0.0 MiB of 64.0 MiB, loads 0, evictions 0", "HUD cache line");
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
            var entry = world.Journal.Entries.Single();
            Check.True(entry.Command is PlaceMarker place && place.Position == selected && place.Label == "M1", "journal holds PlaceMarker at the selected point");
            Check.True(entry.Rejection is null, "world accepted the marker");
            Check.Equal(1, world.Markers.Count, "marker exists in the world as soon as the command is applied");
            main.Host.Advance(1.0);

            Press(Key.Delete);
            await runner.Frames(2);
            main.Host.Advance(1.0);
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
            Check.Equal(2 + FieldCatalog.All.Count, shell.Layers.Count, "layer list: the world frame, the markers and every atlas field");
            Check.Equal(2 + 4, shell.Layers.Count(layer => layer.IsAvailable), "two base layers and the four available fields are drawable");
            Check.True(shell.Layers.Where(layer => !layer.IsAvailable).All(layer => layer.ListText.Contains("(unavailable — ", StringComparison.Ordinal)), "unavailable layers are marked");

            Click(shell.Map.GlobalPosition + (shell.Map.Size / 2));
            await runner.Frames(2);
            shell.RefreshPanels();
            var inspection = shell.Inspector.Current!;
            Check.Equal(17, inspection.Families.Count, "inspector lists every family and the developer tools");
            Check.Equal(15, inspection.Families.Count(family => family.Availability == FieldAvailability.Unavailable), "fifteen unavailable families");
            var text = string.Join('\n', shell.Inspector.Lines.Select(line => line.Text));
            foreach (var family in FieldFamilies.All.Append(FieldCatalog.Developer))
            {
                Check.Contains(text, family.Name, "inspector shows family");
            }

            Check.True(shell.Inspector.Lines.Any(line => line.Style == LineStyle.Dim && line.Text.Contains("Phase 07", StringComparison.Ordinal)), "unavailable readings are dim and name their phase");
            Check.True(shell.Inspector.Lines.Any(line => line.Text.Contains("owner: World foundation", StringComparison.Ordinal)) &&
                shell.Inspector.Lines.Any(line => line.Text.Contains("classification: analytic: φ = φ₀", StringComparison.Ordinal)), "readings list their provenance");
            Check.True(!text.Contains("fake", StringComparison.OrdinalIgnoreCase), "no invented values");

            Check.Equal(MapLayer.WorldFrameKey, shell.ActiveLayer.Key, "starts on the world frame layer");
            Press(Key.Bracketright);
            await runner.Frames(2);
            Check.Equal(MapLayer.DeveloperMarkersKey, shell.ActiveLayer.Key, "layer_next selects the next layer");
            Press(Key.Bracketright);
            await runner.Frames(2);
            Check.Equal(FieldCatalog.Latitude.Key, shell.ActiveLayer.Key, "the next layer is the first atlas field, latitude, which is drawable");
            Check.True(shell.ActiveLayer.IsFieldOverlay, "latitude is drawn as an overlay");
            Check.True(shell.Legend.Text.Contains("level: macro (4.096 km)", StringComparison.Ordinal), "its legend names the level");

            shell.SetLayer(shell.Layers.ToList().FindIndex(layer => !layer.IsAvailable));
            await runner.Frames(2);
            Check.True(!shell.ActiveLayer.IsAvailable, "an unavailable layer can be selected");
            Check.Contains(shell.Legend.Text, "No data — unavailable:", "legend names the missing data");
            Check.Contains(shell.Legend.Text, shell.ActiveLayer.UnavailableReason!, "legend gives owner and phase");
            Check.True(shell.Map.Overlay is null, "an unavailable layer has no overlay to draw");
            shell.SetLayer(1);
            Press(Key.Bracketleft);
            await runner.Frames(1);
            Check.Equal(MapLayer.WorldFrameKey, shell.ActiveLayer.Key, "layer_previous steps back");
            shell.SetLayer(1);

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

    private async Task TimeActionsControlPacing()
    {
        using var workspace = new Workspace();
        var main = await Boot(workspace, "--fixture", "F-EMPTY");
        try
        {
            var world = main.Host!.World;
            var shell = main.Shell!;
            var control = world.TimeControl;
            var checksum = world.ComputeChecksum();
            Check.True(!control.Paused && control.SimMillisecondsPerRealSecond == TimeControl.Presets[0], "starts running at 1 s/s");

            Press(Key.Space);
            Check.True(control.Paused, "Space pauses");
            Press(Key.Space);
            Check.True(!control.Paused, "Space again resumes");
            Press(Key.Period);
            Check.Equal(1, control.RequestedSteps, "Period requests one step");
            main.Host.Advance(0.016);
            Check.Equal(0, control.RequestedSteps, "the host uses the requested step");
            Check.Equal(1L, world.StepCount, "and the world advanced exactly one step");

            for (var preset = 0; preset < TimeControl.Presets.Count; preset++)
            {
                Press((Key)((int)Key.Key1 + preset));
                Check.Equal(TimeControl.Presets[preset], control.SimMillisecondsPerRealSecond, $"key {preset + 1} selects the preset");
            }

            Press(Key.Space);
            shell.RefreshPanels();
            Check.Contains(shell.Hud.Text, "time: PAUSED (speed 1 week/s)", "HUD shows the paused state and speed");
            Check.Equal(0, world.Journal.Entries.Count, "pacing never enters the journal");

            Press(Key.B);
            Check.Equal(1, world.Journal.Entries.Count, "a breakpoint is a world command and is journaled");
            var set = world.Journal.Entries[0];
            var midnight = WorldCalendar.StartOfDay(world.Now).Plus(WorldCalendar.MillisecondsPerDay);
            Check.True(set.Command is SetBreakpoint { Label: "B1" } breakpoint && breakpoint.At == midnight && set.Rejection is null, "B sets breakpoint B1 at the next standard-time midnight");
            Check.Contains(shell.Status.HelpText, "Speed: 1/2/3/4/5", "the help line lists the speed keys");
            Check.Contains(shell.Status.HelpText, "Acknowledge: Enter", "the help line lists the acknowledge key");

            Press(Key.Enter);
            Check.Equal(1, world.Journal.Entries.Count, "Enter with no decision pending submits nothing");

            var actions = main.Inputs.Snapshot().Select(handled => handled.Action).ToList();
            string[] timeActions = [InputActions.TimeTogglePause, InputActions.TimeStep, .. InputActions.TimeSpeeds, InputActions.SetBreakpoint, InputActions.AcknowledgeDecision];
            foreach (var action in timeActions)
            {
                Check.True(actions.Contains(action), $"input history lists {action}");
            }

            // Pacing left no trace in the state: the same world without any of it has the same checksum, apart from the one step and breakpoint.
            var plain = TestWorlds.LaunchFixture().World!;
            plain.AdvanceTo(plain.Now.Plus(plain.StepMs));
            plain.Submit(new SetBreakpoint(midnight, "B1"));
            Check.Equal(plain.ComputeChecksum(), world.ComputeChecksum(), "pausing, stepping and speed changes do not change the checksum");
            Check.True(checksum != world.ComputeChecksum(), "the step and the breakpoint did");
        }
        finally
        {
            await Dispose(main);
        }
    }

    private async Task StartupTimeOptions()
    {
        using var workspace = new Workspace();
        var main = await Boot(workspace, "--fixture", "F-EMPTY", "--paused", "--speed", "3");
        try
        {
            var world = main.Host!.World;
            Check.True(world.TimeControl.Paused, "--paused pauses time at startup");
            Check.Equal(TimeControl.Presets[2], world.TimeControl.SimMillisecondsPerRealSecond, "--speed 3 is 1 h/s");
            TestWorlds.Frames(main.Host, 40, 0.25);
            Check.Equal(world.Identity.Start, world.Now, "a paused world does not advance");
            main.Shell!.RefreshPanels();
            Check.Contains(main.Shell.Hud.Text, "time: PAUSED (speed 1 h/s)", "HUD shows the startup state");
            Check.Equal(0, world.Journal.Entries.Count, "startup pacing is not journaled");
        }
        finally
        {
            await Dispose(main);
        }
    }

    private async Task BreakpointStopsTimeUntilAcknowledged()
    {
        using var workspace = new Workspace();
        var main = await Boot(workspace, "--fixture", "F-EMPTY", "--speed", "5");
        try
        {
            var host = main.Host!;
            var world = host.World;
            var shell = main.Shell!;
            Check.Equal(TimeControl.Presets[4], world.TimeControl.SimMillisecondsPerRealSecond, "--speed 5 is 1 week/s");

            Press(Key.B);
            var midnight = world.Breakpoints.Single().At;
            Check.Equal(WorldCalendar.StartOfDay(world.Now).Plus(WorldCalendar.MillisecondsPerDay), midnight, "the breakpoint is at the next midnight");

            // At 1 week/s a quarter second of real time is 1.75 days, so only the decision can stop the world at midnight.
            TestWorlds.Frames(host, 8, 0.25);
            Check.Equal(midnight, world.Now, "time stopped exactly at the breakpoint");
            Check.Equal(1, world.PendingDecisions.Count, "one decision is pending");
            await runner.Frames(2);
            shell.RefreshPanels();
            Check.True(shell.Decisions.Visible, "the decision strip is visible");
            Check.Equal("Decision pending: B1 — press Enter to acknowledge", shell.Decisions.Text, "the strip names the decision and the key");
            Check.Contains(shell.Hud.Text, "DECISION PENDING: B1", "HUD lists the pending decision");
            Check.Contains(shell.Hud.Text, "STOPPED by a pending decision", "HUD shows time is stopped");
            Check.Contains(shell.Events.Lines[0].Text, "DecisionRaised", "the events panel lists the raised decision");

            Press(Key.Period);
            Check.Equal(0, world.TimeControl.RequestedSteps, "a step is not queued behind a pending decision");
            TestWorlds.Frames(host, 8, 0.25);
            Check.Equal(midnight, world.Now, "time stays stopped until the decision is acknowledged");

            var decision = world.PendingDecisions[0];
            Press(Key.Enter);
            Check.Equal(0, world.PendingDecisions.Count, "Enter acknowledges the oldest pending decision");
            Check.True(world.Journal.Entries[^1].Command is AcknowledgeDecision acknowledge && acknowledge.Decision == decision.Event && world.Journal.Entries[^1].Rejection is null,
                "the acknowledgement is journaled with the decision's event ID");
            host.Advance(0.25);
            Check.True(world.Now > midnight, "time continues after the acknowledgement");
            await runner.Frames(2);
            shell.RefreshPanels();
            Check.True(!shell.Decisions.Visible, "the decision strip hides");
            Check.Contains(shell.Hud.Text, "time: running at 1 week/s", "HUD shows time running again");
            Check.True(shell.Events.Lines.Any(line => line.Text.EndsWith("DecisionAcknowledged", StringComparison.Ordinal)), "the events panel lists the acknowledgement");
        }
        finally
        {
            await Dispose(main);
        }
    }

    private async Task FailureIsReported()
    {
        using var workspace = new Workspace();
        var main = await Boot(workspace, "--fixture", "F-EMPTY", "--fail-after-ms", "2000", "--marker", "40000,60000");
        try
        {
            var host = main.Host!;
            var failAt = host.World.Identity.Start.Milliseconds + 2000;
            TestWorlds.Frames(host, 12, 0.25);
            Check.True(host.Failure is { ReportPath: not null }, "the host stopped and wrote a report");
            var failure = host.Failure!;
            Check.True(File.Exists(failure.ReportPath), "report file exists");
            Check.Contains(failure.ReplayCommand, "replay", "reproduction command");
            Check.Equal(failAt, host.World.Now.Milliseconds, "the world stopped at the injected time, relative to its start");
            TestWorlds.Frames(host, 8, 0.25);
            Check.Equal(failAt, host.World.Now.Milliseconds, "a failed host stops advancing");

            await runner.Frames(3);
            Check.True(main.Shell!.Banner.Visible && main.Shell.Banner.Text.Contains(failure.ReportPath!, StringComparison.Ordinal), "banner shows the report path");

            using var stream = File.OpenRead(failure.ReportPath!);
            var report = FailureReport.ReadFrom(stream);
            Check.Equal("client", report.Host, "report host");
            Check.Equal(failAt, report.SimTimeMs, "report time");
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

    private void Press(Key key, bool shift = false)
    {
        foreach (var pressed in new[] { true, false })
        {
            runner.GetViewport().PushInput(new InputEventKey { Keycode = key, ShiftPressed = shift, Pressed = pressed }, inLocalCoords: true);
        }
    }
}
