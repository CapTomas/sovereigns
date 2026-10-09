using Sovereigns.Client.Simulation;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Geometry;

namespace Sovereigns.Client.Tests;

/// <summary>The rendering clock must not change simulation results (ADR-0001, ADR-0004 §4).</summary>
public static class PacingTests
{
    public static IEnumerable<TestCase> Cases() =>
    [
        new("pacing: 0.25 s x8, 2 s x1 and 0.1 s x20 frames reach the same time, step count and checksum", FramePacingIsIrrelevant),
        new("pacing: a long hitch runs at most 10 steps per frame and warns once", HitchIsCappedAndWarnsOnce),
    ];

    private sealed record Outcome(long NowMs, long Steps, string Checksum);

    private static Outcome Run(IEnumerable<double> deltas)
    {
        using var workspace = new Workspace();
        var world = TestWorlds.LaunchFixture().World!;
        var host = TestWorlds.CreateHost(world, workspace.File("reports"));
        Check.True(host.TrySubmit(new PlaceMarker(new WorldPoint(1234.5, 67_890.25), "P1")), "command submission before the first frame");
        foreach (var delta in deltas)
        {
            host.Advance(delta);
        }

        Check.True(host.Failure is null, "host did not fail");
        return new Outcome(world.Now.Milliseconds, world.StepCount, world.ComputeChecksum());
    }

    private static Task FramePacingIsIrrelevant()
    {
        var quarter = Run(Enumerable.Repeat(0.25, 8));
        var single = Run([2.0]);
        var tenth = Run(Enumerable.Repeat(0.1, 20));
        Check.Equal(2000L, quarter.NowMs, "simulation time after two real seconds");
        Check.Equal(2L, quarter.Steps, "step count after two real seconds");
        Check.Equal(quarter, single, "2 s single frame matches 0.25 s frames");
        Check.Equal(quarter, tenth, "0.1 s frames match 0.25 s frames");
        return Task.CompletedTask;
    }

    private static Task HitchIsCappedAndWarnsOnce()
    {
        using var workspace = new Workspace();
        var world = TestWorlds.LaunchFixture().World!;
        var recent = new RecentLogBuffer(64);
        var host = TestWorlds.CreateHost(world, workspace.File("reports"), recent);
        host.Advance(30);
        Check.Equal(10L, world.StepCount, "steps after a 30 s hitch");
        host.Advance(30);
        Check.Equal(20L, world.StepCount, "steps after a second hitch");
        host.Advance(0.4);
        Check.Equal(20L, world.StepCount, "the dropped backlog is not replayed later");
        Check.Equal(1, recent.WarningCount, "one warning for repeated hitches");
        return Task.CompletedTask;
    }
}
