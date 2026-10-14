using Sovereigns.Client.Boot;
using Sovereigns.Client.Diagnostics;
using Sovereigns.Client.Simulation;
using Sovereigns.Simulation;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Scenarios;

namespace Sovereigns.Client.Tests;

/// <summary>A scratch directory removed when the test case ends.</summary>
public sealed class Workspace : IDisposable
{
    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("sovereigns-client-tests-");

    public string Path => _directory.FullName;

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose() => _directory.Delete(recursive: true);
}

public static class TestWorlds
{
    public const ulong FixtureSeed = 20261009;
    public const string FixtureScenario = "core:scenario/empty_world";

    public static LaunchResult LaunchFixture(Logger? logger = null)
    {
        var args = LaunchArguments.Parse(["--fixture", "F-EMPTY"]);
        var result = WorldLauncher.Launch(args, ClientPaths.Resolve(args), logger ?? Logger.None);
        Check.True(result.Succeeded, $"F-EMPTY must launch: {string.Join("; ", result.Problems)}");
        return result;
    }

    public static SimulationHost CreateHost(World world, string reportsDirectory, RecentLogBuffer? recent = null, long? failAtMs = null)
    {
        var recentLog = recent ?? new RecentLogBuffer(64);
        var logger = new Logger([recentLog], LogSeverity.Info);
        var reporter = new FailureReporter(reportsDirectory, null, logger.For("test"), BuildInfo.Describe(typeof(TestWorlds).Assembly), recentLog, new InputHistory(16));
        return new SimulationHost(world, logger, reporter, failAtMs);
    }
}
