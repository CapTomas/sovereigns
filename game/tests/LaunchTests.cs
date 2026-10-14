using Sovereigns.Client.Boot;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Scenarios;

namespace Sovereigns.Client.Tests;

/// <summary>Launch options and world selection, which the player and scripts reach through the command line.</summary>
public static class LaunchTests
{
    public static IEnumerable<TestCase> Cases() =>
    [
        new("launch: options parse and malformed ones are rejected with the usage", ParsesOptions),
        new("launch: fixture, scenario and seed selection follow the shared rules", SelectsWorlds),
    ];

    private static Task ParsesOptions()
    {
        var args = LaunchArguments.Parse(["--fixture", "F-EMPTY", "--fail-at-ms", "3000", "--select", "100.5,200", "--marker", "1,2", "--capture-frame", "30", "--layer", "snow_frozen"]);
        Check.Equal("F-EMPTY", args.Fixture, "fixture");
        Check.Equal(3000L, args.FailAtMs, "fail-at-ms");
        Check.Equal(new WorldPoint(100.5, 200), args.Select, "select");
        Check.Equal(new WorldPoint(1, 2), args.Marker, "marker");
        Check.Equal(30, args.CaptureFrame, "capture frame");
        Check.Equal(10, LaunchArguments.Parse([]).CaptureFrame, "default capture frame");

        foreach (var bad in new[] { new[] { "--bogus", "1" }, ["--fixture"], ["--seed", "1", "--seed", "2"], ["--select", "5"], ["--fail-at-ms", "-1"], ["fixture", "F-EMPTY"] })
        {
            var message = ExpectLaunchException(() => LaunchArguments.Parse(bad), $"arguments {string.Join(' ', bad)}");
            Check.True(message.Length > 0, "the message says what is wrong");
        }

        Check.Contains(ExpectLaunchException(() => LaunchArguments.Parse(["--bogus", "1"]), "unknown option"), "--fixture", "the usage lists the options");
        return Task.CompletedTask;
    }

    private static Task SelectsWorlds()
    {
        LaunchResult Launch(params string[] args)
        {
            var parsed = LaunchArguments.Parse(args);
            return WorldLauncher.Launch(parsed, ClientPaths.Resolve(parsed), Logger.None);
        }

        var fixture = Launch("--fixture", "F-EMPTY");
        Check.Equal(TestWorlds.FixtureSeed, fixture.World!.Seed, "fixture pins the seed");
        Check.Equal("F-EMPTY@1", fixture.Selection!.FixtureReference, "fixture reference");

        var explicitSeed = Launch("--scenario", TestWorlds.FixtureScenario, "--seed", "42");
        Check.Equal(42UL, explicitSeed.World!.Seed, "explicit seed");
        Check.True(explicitSeed.Selection!.FixtureReference is null, "no fixture");
        Check.Equal(WorldLauncher.DefaultSeed, Launch().World!.Seed, "default seed without arguments");

        Check.True(Launch("--fixture", "F-EMPTY", "--seed", "7") is { Succeeded: false }, "a fixture cannot be combined with a seed");
        Check.True(Launch("--seed", "7") is { Succeeded: false }, "a seed needs a scenario");
        Check.True(Launch("--fixture", "F-NOPE") is { Succeeded: false } missing && missing.Problems[0].Contains("F-NOPE", StringComparison.Ordinal), "an unknown fixture is named");
        Check.True(Launch("--content", "/no/such/content") is { Succeeded: false } noContent && noContent.Problems[0].Contains("/no/such/content", StringComparison.Ordinal), "a missing content directory is named");
        return Task.CompletedTask;
    }

    private static string ExpectLaunchException(Action action, string what)
    {
        try
        {
            action();
        }
        catch (LaunchException ex)
        {
            return ex.Message;
        }

        throw new CheckFailedException($"{what}: expected a LaunchException");
    }
}
