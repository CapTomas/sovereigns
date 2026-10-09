using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Tests;

public sealed class FailureReportTests
{
    [Fact]
    public void ReportCapturesSeedTimeCommandsInputsAndLogContextAndReplays()
    {
        var recent = new RecentLogBuffer(10);
        var scenario = TestSupport.Scenario();
        var world = World.Create(scenario, seed: 77, new Logger([recent], LogSeverity.Debug));
        world.Submit(new PlaceMarker(new WorldPoint(3, 4), "suspect"));
        world.AdvanceTo(new SimTime(9_000));
        HostInputEvent[] inputs = [new(120, 2.5, "place_marker", "(3, 4)")];

        var report = FailureReport.Capture(world, "test", "invariant broken", new InvalidOperationException("boom"),
            BuildInfo.Describe(typeof(World).Assembly), recent, inputs);
        using var stream = new MemoryStream();
        report.WriteTo(stream);
        stream.Position = 0;
        var read = FailureReport.ReadFrom(stream);

        Assert.Equal((77UL, "test:scenario/plain", 9_000L), (read.Seed, read.Scenario, read.SimTimeMs));
        Assert.Equal(world.ComputeChecksum(), read.StateChecksum);
        Assert.Equal(world.Journal.Entries, read.Commands);
        Assert.Equal(inputs, read.Inputs);
        Assert.Contains(read.RecentLog, record => record.Message == "marker placed" && record.SimTimeMs == 0);
        Assert.Contains("InvalidOperationException: boom", read.Exception, StringComparison.Ordinal);
        Assert.Equal("failure-seed77-t9000ms.json", read.FileName);

        var replayed = Replay.Run(scenario, read.Seed, read.Commands, new SimTime(read.SimTimeMs));
        Assert.Equal(read.StateChecksum, replayed.ComputeChecksum());
    }

    [Fact]
    public void FailureInsideAStepReportsTheStepInputsAndReproducesTheFailure()
    {
        var scenario = TestSupport.Scenario();
        var world = World.Create(scenario, seed: 5);
        world.Submit(new PlaceMarker(new WorldPoint(1, 1), "before"));
        world.AdvanceTo(new SimTime(3_000));
        world.Submit(new PlaceMarker(new WorldPoint(2, 2), "same step, applied"));
        world.Submit(new UnhandledCommand());
        world.Submit(new PlaceMarker(new WorldPoint(3, 3), "queued behind the failure"));

        var thrown = Assert.Throws<NotSupportedException>(world.Step);
        var report = FailureReport.Capture(world, "test", thrown.Message, thrown, BuildInfo.Describe(typeof(World).Assembly));

        Assert.True(world.StepInProgress);
        Assert.Throws<InvalidOperationException>(world.Step);
        Assert.True(report.FailedInStep);
        Assert.Null(report.StateChecksum);
        Assert.Equal(3_000, report.SimTimeMs);
        Assert.Equal(["before"], report.Commands.Select(entry => ((PlaceMarker)entry.Command).Label));
        Assert.Equal(3, report.PendingCommands.Count);
        Assert.IsType<UnhandledCommand>(report.PendingCommands[1]);

        var rebuilt = Replay.Run(scenario, report.Seed, report.Commands, new SimTime(report.SimTimeMs));
        foreach (var command in report.PendingCommands)
        {
            rebuilt.Submit(command);
        }

        Assert.Throws<NotSupportedException>(rebuilt.Step);
    }

    [Fact]
    public void ReportsFromTheSameSeedAndTimeDoNotOverwriteEachOther()
    {
        var directory = TestSupport.NewTempDirectory();
        var world = World.Create(TestSupport.Scenario(), seed: 9);
        var build = BuildInfo.Describe(typeof(World).Assembly);

        var first = FailureReport.Capture(world, "test", "step failed", null, build).WriteToDirectory(directory);
        var second = FailureReport.Capture(world, "test", "manual report", null, build).WriteToDirectory(directory);

        Assert.EndsWith("failure-seed9-t0ms.json", first, StringComparison.Ordinal);
        Assert.EndsWith("failure-seed9-t0ms-2.json", second, StringComparison.Ordinal);
        using var stream = File.OpenRead(first);
        Assert.Equal("step failed", FailureReport.ReadFrom(stream).Reason);
    }

    [Fact]
    public void ReportWithAnotherFormatIsRejected()
    {
        using var stream = new MemoryStream("""{"format":99}"""u8.ToArray());

        Assert.ThrowsAny<System.Text.Json.JsonException>(() => FailureReport.ReadFrom(stream));
    }

    /// <summary>A command type the world has no handler for; it stands in for a defect inside a step.</summary>
    private sealed record UnhandledCommand : SimCommand;
}
