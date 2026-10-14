using Sovereigns.Simulation.Content;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Scenarios;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Tests;

internal static class TestSupport
{
    /// <summary>A small world starting at the epoch, 1 January of year 1, in the northern mid-latitudes.</summary>
    public static ScenarioDefinition Scenario(long stepMs = 1000, double east = 10_000, double north = 8_000, long durationMs = 60_000,
        double southLatitudeDeg = 45, CalendarStart? start = null) =>
        new(ContentId.Parse("test:scenario/plain"), "test scenario", new WorldExtent(east, north),
            new GeographyDefinition(southLatitudeDeg, east / 2), start ?? new CalendarStart(1, 1, 1), stepMs, durationMs);

    /// <summary>The world time of the repository's empty-world scenario start, 1203-03-01 06:00.</summary>
    public static SimTime EmptyWorldStart => WorldCalendar.FromCalendar(1203, 3, 1, 6);

    public static string RepoRoot { get; } = FindRepoRoot();

    public static string ContentRoot => Path.Combine(RepoRoot, "content");

    public static string NewTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "sovereigns-tests", Path.GetRandomFileName());
        Directory.CreateDirectory(path);
        return path;
    }

    private static string FindRepoRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "global.json")) && Directory.Exists(Path.Combine(directory.FullName, "content")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("repository root not found above " + AppContext.BaseDirectory);
    }
}

/// <summary>Collects records in memory.</summary>
internal sealed class ListSink : ILogSink
{
    public List<LogRecord> Records { get; } = [];

    public void Write(LogRecord record) => Records.Add(record);
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
