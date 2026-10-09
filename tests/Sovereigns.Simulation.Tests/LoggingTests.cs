using System.Text.Json;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Geometry;

namespace Sovereigns.Simulation.Tests;

public sealed class LoggingTests
{
    [Fact]
    public void WorldRecordsCarrySeveritySubsystemSeedSimTimeAndObjectIds()
    {
        var sink = new ListSink();
        var world = World.Create(TestSupport.Scenario(), seed: 31, new Logger([sink], LogSeverity.Debug));
        world.AdvanceTo(new Time.SimTime(2_000));
        world.Submit(new PlaceMarker(new WorldPoint(1, 1), "m"));
        world.Step();

        var created = sink.Records[0];
        var placed = Assert.Single(sink.Records, record => record.Message == "marker placed");

        Assert.Equal((LogSeverity.Info, "world", 31UL, 0L), (created.Severity, created.Subsystem, created.WorldSeed, created.SimTimeMs));
        Assert.Equal("test:scenario/plain", created.Data!["scenario"]);
        Assert.Equal((LogSeverity.Debug, 31UL, 2_000L), (placed.Severity, placed.WorldSeed, placed.SimTimeMs));
        Assert.Equal(["marker:1"], placed.ObjectIds);
    }

    [Fact]
    public void RecordsBelowTheMinimumSeverityAreDropped()
    {
        var sink = new ListSink();
        var world = World.Create(TestSupport.Scenario(), seed: 1, new Logger([sink], LogSeverity.Warning));
        world.Submit(new PlaceMarker(new WorldPoint(1, 1), "accepted"));
        world.Submit(new PlaceMarker(new WorldPoint(-1, 1), "rejected"));
        world.Step();

        var record = Assert.Single(sink.Records);
        Assert.Equal(LogSeverity.Warning, record.Severity);
    }

    [Fact]
    public void JsonLinesSinkWritesOneParseableObjectPerRecordWithHostWallTime()
    {
        using var text = new StringWriter();
        var wall = new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
        var logger = new Logger([new JsonLinesLogSink(text)], LogSeverity.Info, new FixedTimeProvider(wall));

        logger.For("test").Info("hello", ["marker:4"], new Dictionary<string, string> { ["k"] = "v" });
        logger.For("test").Error("boom", new InvalidOperationException("bad"));

        var lines = text.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        using var first = JsonDocument.Parse(lines[0]);
        Assert.Equal("info", first.RootElement.GetProperty("severity").GetString());
        Assert.Equal("test", first.RootElement.GetProperty("subsystem").GetString());
        Assert.Equal("marker:4", first.RootElement.GetProperty("object_ids")[0].GetString());
        Assert.Equal("v", first.RootElement.GetProperty("data").GetProperty("k").GetString());
        Assert.Equal(wall, first.RootElement.GetProperty("wall_time_utc").GetDateTimeOffset());
        Assert.False(first.RootElement.TryGetProperty("world_seed", out _));
        using var second = JsonDocument.Parse(lines[1]);
        Assert.Contains("InvalidOperationException: bad", second.RootElement.GetProperty("exception").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public void RecentBufferKeepsTheNewestRecordsAndCountsProblems()
    {
        var buffer = new RecentLogBuffer(capacity: 3);
        var log = new Logger([buffer], LogSeverity.Debug).For("test");

        log.Info("1");
        log.Warning("2");
        log.Error("3");
        log.Info("4");

        Assert.Equal(["2", "3", "4"], buffer.Snapshot().Select(record => record.Message));
        Assert.Equal((1, 1), (buffer.WarningCount, buffer.ErrorCount));
        Assert.Equal("3", buffer.LastWarningOrError?.Message);
    }

    [Fact]
    public void TextFormatIsOneReadableLine()
    {
        var record = new LogRecord(LogSeverity.Warning, "world", "command rejected", 5, 1_000, ["marker:2"],
            new Dictionary<string, string> { ["sequence"] = "3" });

        Assert.Equal("[WARNING] world seed=5 t=1000ms: command rejected [marker:2] {sequence=3}", TextLogSink.Format(record));
    }
}
