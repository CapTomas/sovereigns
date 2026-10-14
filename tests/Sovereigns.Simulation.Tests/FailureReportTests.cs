using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Identity;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Tests;

public sealed class FailureReportTests
{
    [Fact]
    public void ReportCapturesIdentitySeedTimeCommandsInputsAndLogContextAndReplays()
    {
        var recent = new RecentLogBuffer(10);
        var scenario = TestSupport.Scenario();
        var world = World.Create(scenario, seed: 77, new Logger([recent], LogSeverity.Debug), new WorldId(0xABCDEF));
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
        Assert.Equal("00000000000000000000000000abcdef", read.WorldId);
        Assert.Equal((FaultStage.None, (SimCommand?)null, (string?)null), (read.FailedDuring, read.FailingCommand, read.BaseSaveSha256));
        Assert.Equal(world.ComputeChecksum(), read.StateChecksum);
        Assert.Equal(world.Journal.Entries, read.Commands);
        Assert.Equal(inputs, read.Inputs);
        Assert.Contains(read.RecentLog, record => record.Message == "event MarkerPlaced" && record.SimTimeMs == 0);
        Assert.Contains("InvalidOperationException: boom", read.Exception, StringComparison.Ordinal);
        Assert.Equal("failure-seed77-t9000ms.json", read.FileName);

        var replayed = Replay.Run(scenario, read.Seed, read.Commands, new SimTime(read.SimTimeMs));
        Assert.Equal(read.StateChecksum, replayed.ComputeChecksum());
    }

    [Fact]
    public void FailingCommandFaultsTheWorldAndTheReportReproducesIt()
    {
        var scenario = TestSupport.Scenario();
        var world = World.Create(scenario, seed: 5);
        world.Submit(new PlaceMarker(new WorldPoint(1, 1), "before"));
        world.AdvanceTo(new SimTime(3_000));

        var thrown = Assert.Throws<NotSupportedException>(() => world.Submit(new UnhandledCommand()));
        var report = FailureReport.Capture(world, "test", thrown.Message, thrown, BuildInfo.Describe(typeof(World).Assembly));

        Assert.Equal(FaultStage.Command, world.Fault);
        Assert.Throws<InvalidOperationException>(() => world.Step());
        Assert.Throws<InvalidOperationException>(() => world.Submit(new PlaceMarker(new WorldPoint(2, 2), "refused")));
        Assert.Equal((FaultStage.Command, 3_000L), (report.FailedDuring, report.SimTimeMs));
        Assert.IsType<UnhandledCommand>(report.FailingCommand);
        Assert.Null(report.StateChecksum);
        Assert.Single(report.Commands);

        var rebuilt = Replay.Run(scenario, report.Seed, report.Commands, new SimTime(report.SimTimeMs));
        Assert.Throws<NotSupportedException>(() => rebuilt.Submit(report.FailingCommand!));
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

    /// <summary>A command type the world has no handler for; it stands in for a defect while applying a command.</summary>
    private sealed record UnhandledCommand : SimCommand;
}
