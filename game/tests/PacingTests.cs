using Sovereigns.Client.Simulation;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Kernel;

namespace Sovereigns.Client.Tests;

/// <summary>The rendering clock must not change simulation results (ADR-0001, ADR-0004 §4).</summary>
public static class PacingTests
{
    public static IEnumerable<TestCase> Cases() =>
    [
        new("pacing: 0.25 s x8, 0.1 s x20 and irregular frames reach the same time, step count and checksum", FramePacingIsIrrelevant),
        new("pacing: a long hitch catches up on at most 0.25 s of real time at the current speed and warns once", HitchIsCappedAndWarnsOnce),
    ];

    private sealed record Outcome(long ElapsedMs, long Steps, string Checksum);

    private static Outcome Run(IEnumerable<double> deltas)
    {
        using var workspace = new Workspace();
        var world = TestWorlds.LaunchFixture().World!;
        var host = TestWorlds.CreateHost(world, workspace.File("reports"));
        Check.True(host.TrySubmit(new PlaceMarker(new WorldPoint(1234.5, 67_890.25), "P1")) is { Rejection: null }, "command submission before the first frame");
        foreach (var delta in deltas)
        {
            host.Advance(delta);
        }

        Check.True(host.Failure is null, "host did not fail");
        return new Outcome(world.Now.Milliseconds - world.Identity.Start.Milliseconds, world.StepCount, world.ComputeChecksum());
    }

    private static Task FramePacingIsIrrelevant()
    {
        var quarter = Run(Enumerable.Repeat(0.25, 8));
        var tenth = Run(Enumerable.Repeat(0.1, 20));
        var irregular = Run([0.05, 0.15, 0.25, 0.1, 0.2, 0.05, 0.25, 0.05, 0.1, 0.25, 0.15, 0.2, 0.05, 0.15, 0.1, 0.2]);
        Check.Equal(2000L, quarter.ElapsedMs, "simulation time elapsed after two real seconds");
        Check.Equal(2L, quarter.Steps, "step count after two real seconds");
        Check.Equal(quarter, tenth, "0.1 s frames match 0.25 s frames");
        Check.Equal(quarter, irregular, "irregular frames match 0.25 s frames");
        return Task.CompletedTask;
    }

    private static Task HitchIsCappedAndWarnsOnce()
    {
        using var workspace = new Workspace();
        var world = TestWorlds.LaunchFixture().World!;
        var recent = new RecentLogBuffer(64);
        var host = TestWorlds.CreateHost(world, workspace.File("reports"), recent);

        // At 1 s/s the cap of a quarter second of real time is less than a step, so a hitch yields one step.
        host.Advance(30);
        Check.Equal(1L, world.StepCount, "steps after a 30 s hitch at 1 s/s");
        host.Advance(30);
        Check.Equal(2L, world.StepCount, "steps after a second hitch");
        host.Advance(0.4);
        Check.Equal(2L, world.StepCount, "the dropped backlog is not replayed later");
        Check.Equal(1, recent.WarningCount, "one warning for repeated hitches");

        // The cap is a quarter second of real time at the current speed: 15 simulated seconds at 1 min/s.
        Check.True(host.TryControl(new SetSpeed(TimeControl.Presets[1])) is null, "speed set");
        host.Advance(30);
        Check.Equal(2L + 15, world.StepCount, "steps after a 30 s hitch at 1 min/s");
        return Task.CompletedTask;
    }
}
