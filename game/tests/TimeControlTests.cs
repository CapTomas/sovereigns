using Sovereigns.Client.Boot;
using Sovereigns.Client.Simulation;
using Sovereigns.Simulation;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Events;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Kernel;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Client.Tests;

/// <summary>
/// Pause, step, speed and fixture command streams on the host (SOV-P02-T11, T16). Pacing changes how far the world
/// advances per real second, never what happens at a given simulation time.
/// </summary>
public static class TimeControlTests
{
    public static IEnumerable<TestCase> Cases() =>
    [
        new("time: a paused host does not advance for any real time, and a requested step advances exactly one step", PausedHostStepsOnRequest),
        new("time: each speed preset changes how far one real second advances", SpeedPresetsScaleAdvance),
        new("time: a fixture command stream is submitted when world time reaches each entry, and its decisions are acknowledged by the stream", FixtureStreamAppliesAtRecordedTimes),
        new("time: a decision the fixture stream cannot acknowledge stops time until the player acknowledges it", StreamDecisionWaitsForThePlayer),
        new("time: very different frame sequences, pauses and speed changes reach the same F-KERNEL state, journal and checksum", FramePatternsDoNotChangeTheWorld),
    ];

    private static (LaunchResult Launch, SimulationHost Host) LaunchHost(Workspace workspace, string name, string fixture)
    {
        var launch = TestWorlds.LaunchFixture(fixture: fixture);
        return (launch, TestWorlds.CreateHost(launch.World!, workspace.File(name), script: launch.Script));
    }

    private static Task PausedHostStepsOnRequest()
    {
        using var workspace = new Workspace();
        var (launch, host) = LaunchHost(workspace, "reports", "F-EMPTY");
        var world = launch.World!;
        var start = world.Now;

        Check.True(host.TryControl(new Pause()) is null && world.TimeControl.Paused, "Pause is applied");
        TestWorlds.Frames(host, 400, 0.25);
        Check.Equal(start, world.Now, "100 real seconds while paused");

        host.TryControl(new StepOnce());
        Check.Equal(start, world.Now, "a requested step waits for the next frame");
        host.Advance(0.016);
        Check.Equal(start.Plus(world.StepMs), world.Now, "one requested step advances exactly one step");
        Check.Equal(0, world.TimeControl.RequestedSteps, "the request is used up");
        TestWorlds.Frames(host, 40, 0.25);
        Check.Equal(start.Plus(world.StepMs), world.Now, "no further movement without another request");

        for (var i = 0; i < 3; i++)
        {
            host.TryControl(new StepOnce());
        }

        TestWorlds.Frames(host, 10, 0.25);
        Check.Equal(start.Plus(4 * world.StepMs), world.Now, "three more requests advance exactly three more steps, even over many frames");

        Check.True(host.TryControl(new Resume()) is null && !world.TimeControl.Paused, "Resume is applied");
        TestWorlds.Frames(host, 4, 0.25);
        Check.Equal(start.Plus(5 * world.StepMs), world.Now, "running again advances at the current speed");

        Check.Equal(0, world.Journal.Entries.Count, "pacing is not journaled");
        Check.True(host.TryControl(new SetSpeed(0)) is { Length: > 0 }, "a zero speed is refused with a reason");
        return Task.CompletedTask;
    }

    private static Task SpeedPresetsScaleAdvance()
    {
        using var workspace = new Workspace();
        for (var preset = 0; preset < TimeControl.Presets.Count; preset++)
        {
            var (launch, host) = LaunchHost(workspace, $"reports{preset}", "F-EMPTY");
            var world = launch.World!;
            var start = world.Now;
            Check.True(host.TryControl(new SetSpeed(TimeControl.Presets[preset])) is null, "speed applied");

            // Four quarter-second frames are one real second, the most the kernel catches up on per frame is a quarter.
            TestWorlds.Frames(host, 4, 0.25);
            Check.Equal(start.Plus(TimeControl.Presets[preset]), world.Now, $"one real second at preset {preset + 1}");
            Check.Equal(TimeControl.Presets[preset] / world.StepMs, world.StepCount, $"steps at preset {preset + 1}");
        }

        return Task.CompletedTask;
    }

    private static Task FixtureStreamAppliesAtRecordedTimes()
    {
        using var workspace = new Workspace();
        var (launch, host) = LaunchHost(workspace, "reports", "F-KERNEL");
        var world = launch.World!;
        var script = launch.Script!;
        var camp = script.First(entry => entry.Command is PlaceMarker { Label: "camp" });
        var ford = script.First(entry => entry.Command is PlaceMarker { Label: "ford" });
        var acknowledge = script.First(entry => entry.Command is AcknowledgeDecision);
        Check.Equal(world.Identity.Start, camp.At, "the stream starts with a command at the world's start");
        Check.True(ford.At > camp.At, "ford comes later");
        Check.Equal(0, world.Journal.Entries.Count, "nothing is submitted before the first frame");

        host.TryControl(new SetSpeed(TimeControl.Presets[3]));
        host.Advance(1 / 60.0);
        Check.True(world.Markers.Count == 1 && world.Markers[0].PlacedAt == camp.At, "the entry due at the start is submitted at the start");
        Check.True(world.Now < ford.At, "the world has not reached ford yet");

        for (var frame = 0; frame < 10 && world.Markers.Count < 2; frame++)
        {
            host.Advance(1 / 60.0);
        }

        Check.Equal(2, world.Markers.Count, "ford was placed");
        Check.Equal(ford.At, world.Now, "the world stopped exactly at the entry's time rather than overshooting it");
        Check.Equal(ford.At, world.Markers[1].PlacedAt, "the marker is placed at the recorded time");

        for (var frame = 0; frame < 400 && (host.ScriptedCommandsRemaining > 0 || world.Now <= acknowledge.At); frame++)
        {
            host.Advance(0.25);
        }

        Check.True(host.Failure is null, "host did not fail");
        Check.True(world.Now > acknowledge.At && world.PendingDecisions.Count == 0, "time passed the decision once the stream acknowledged it");
        var applied = world.Journal.Entries.Single(entry => entry.Command is AcknowledgeDecision && entry.At == acknowledge.At);
        Check.True(applied.Rejection is null, "the stream's acknowledgement was accepted");
        var muster = world.Ledger.Events.Single(worldEvent => worldEvent.Type == WorldEventType.DecisionRaised && worldEvent.Data?["label"] == "muster");
        Check.Equal(acknowledge.At, muster.At, "the decision was raised at the recorded time");
        Check.True(world.Journal.Entries.Zip(script).All(pair => pair.First.At == pair.Second.At && pair.First.Command == pair.Second.Command), "the journal repeats the stream");
        return Task.CompletedTask;
    }

    private static Task StreamDecisionWaitsForThePlayer()
    {
        using var workspace = new Workspace();
        var (launch, host) = LaunchHost(workspace, "reports", "F-KERNEL");
        var world = launch.World!;
        var acknowledge = launch.Script!.First(entry => entry.Command is AcknowledgeDecision);

        // An extra command shifts the event IDs the stream recorded, so its acknowledgement names the wrong event and is refused.
        Check.True(host.TrySubmit(new PlaceMarker(new WorldPoint(40_000, 60_000), "extra")) is { Rejection: null }, "extra command accepted");
        host.TryControl(new SetSpeed(TimeControl.Presets[4]));
        for (var frame = 0; frame < 400 && world.PendingDecisions.Count == 0; frame++)
        {
            host.Advance(0.25);
        }

        Check.Equal(1, world.PendingDecisions.Count, "the decision is pending");
        Check.Equal(acknowledge.At, world.Now, "time stopped at the decision");
        Check.Equal(1, world.Journal.Entries.Count(entry => entry.Rejection is not null), "the stream's acknowledgement was refused and journaled as such");
        TestWorlds.Frames(host, 40, 0.25);
        Check.Equal(acknowledge.At, world.Now, "time stays stopped, at any speed, until the player acknowledges");

        var decision = world.PendingDecisions[0];
        Check.True(host.TrySubmit(new AcknowledgeDecision(decision.Event)) is { Rejection: null }, "the player's acknowledgement is accepted");
        host.Advance(0.25);
        Check.True(world.Now > acknowledge.At && host.Failure is null, "time continues");
        return Task.CompletedTask;
    }

    private static Task FramePatternsDoNotChangeTheWorld()
    {
        using var workspace = new Workspace();
        var (steadyLaunch, steady) = LaunchHost(workspace, "steady", "F-KERNEL");
        var (_, erratic) = LaunchHost(workspace, "erratic", "F-KERNEL");
        var end = steadyLaunch.Script![^1].At.Plus(2 * WorldCalendar.MillisecondsPerDay);

        steady.TryControl(new SetSpeed(TimeControl.Presets[3]));
        var steadyFrames = 0;
        while (steady.World.Now < end && steadyFrames++ < 200_000)
        {
            steady.Advance(1 / 60.0, end);
        }

        // Irregular frames of 0.05 to 0.25 s, with hitches, pauses, single steps and speed changes in between.
        long[] speeds = [TimeControl.Presets[2], TimeControl.Presets[3], TimeControl.Presets[4], TimeControl.Presets[1], TimeControl.Presets[4]];
        var chosenSpeeds = new HashSet<long>();
        var pauses = 0;
        var erraticFrames = 0;
        while (erratic.World.Now < end && erraticFrames < 200_000)
        {
            if (erraticFrames % 25 == 0)
            {
                var speed = speeds[erraticFrames / 25 % speeds.Length];
                chosenSpeeds.Add(speed);
                erratic.TryControl(new SetSpeed(speed));
            }

            if (erraticFrames % 40 == 7)
            {
                pauses++;
                erratic.TryControl(new Pause());
                TestWorlds.Frames(erratic, 6, 0.2);
                erratic.TryControl(new StepOnce());
                erratic.TryControl(new StepOnce());
                TestWorlds.Frames(erratic, 3, 0.1);
                erratic.TryControl(new Resume());
            }

            var delta = erraticFrames % 53 == 0 ? 1.5 : 0.05 + (erraticFrames * 37 % 100) / 100.0 * 0.2;
            erratic.Advance(delta, end);
            erraticFrames++;
        }

        Check.True(steady.Failure is null && erratic.Failure is null, "neither host failed");
        Check.Equal(end, steady.World.Now, "steady host reached the end");
        Check.Equal(end, erratic.World.Now, "erratic host reached the end");
        Check.True(erraticFrames > 100 && steadyFrames > 10_000, $"the frame patterns differ a great deal ({steadyFrames} steady frames, {erraticFrames} erratic frames)");
        Check.True(chosenSpeeds.Count >= 4 && pauses >= 3, "the erratic host changed speed and paused repeatedly");

        var a = steady.World;
        var b = erratic.World;
        Check.Equal(a.ComputeChecksum(), b.ComputeChecksum(), "checksums match");
        Check.Equal(a.Ledger.Count, b.Ledger.Count, "ledger sizes match");
        Check.True(a.Ledger.Events.Any(e => e.Type == WorldEventType.SeasonBegan) && a.Ledger.Events.Count(e => e.Type == WorldEventType.DecisionRaised) == 2, "the run crossed seasons and both stream decisions");
        Check.Equal(null, Replay.FirstDivergence(a.Journal.Entries, b.Journal.Entries), "journals match entry for entry");
        Check.Equal(steadyLaunch.Script!.Count, a.Journal.Entries.Count, "every stream command was journaled once");
        Check.True(a.Journal.Entries.All(entry => entry.Command is not ControlCommand), "pacing commands are not in the journal");
        return Task.CompletedTask;
    }
}
