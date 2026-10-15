using Sovereigns.Client.Boot;
using Sovereigns.Simulation.Atlas;
using Sovereigns.Simulation.Commands;
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
        var args = LaunchArguments.Parse(["--fixture", "F-EMPTY", "--fail-after-ms", "3000", "--select", "100.5,200", "--marker", "1,2", "--capture-frame", "30", "--layer", "snow_frozen", "--paused", "--speed", "4"]);
        Check.Equal("F-EMPTY", args.Fixture, "fixture");
        Check.Equal(3000L, args.FailAfterMs, "fail-after-ms");
        Check.True(args.Paused, "--paused is a flag without a value");
        Check.Equal(4, args.Speed, "speed preset");
        Check.True(!LaunchArguments.Parse([]).Paused && LaunchArguments.Parse([]).Speed is null, "time options are off by default");
        Check.Equal(new WorldPoint(100.5, 200), args.Select, "select");
        Check.Equal(new WorldPoint(1, 2), args.Marker, "marker");
        Check.Equal(30, args.CaptureFrame, "capture frame");
        Check.Equal(10, LaunchArguments.Parse([]).CaptureFrame, "default capture frame");

        var atlas = LaunchArguments.Parse(["--paint", "50000,50000,600,1;51000,50000,200,0.25", "--line", "1000,2000;3000,4000;5000,3000|10,10;20,20", "--center", "50000,50500", "--zoom", "2.5", "--level", "Local", "--residency", "--no-hud"]);
        Check.Equal(2, atlas.Paint.Count, "two paint strokes");
        Check.Equal(new PaintScratch(new WorldPoint(51_000, 50_000), 200, 0.25), atlas.Paint[1], "second stroke");
        Check.Equal(2, atlas.Lines.Count, "two survey lines");
        Check.Equal(3, atlas.Lines[0].Count, "first line has three points");
        Check.Equal(new WorldPoint(50_000, 50_500), atlas.Center, "center");
        Check.Equal(2.5, atlas.MetresPerPixel, "zoom in metres per pixel");
        Check.Equal(GridLevel.Local, atlas.Level, "level by name, case-insensitive");
        Check.True(atlas.Residency && atlas.HideHud, "--residency and --no-hud are flags");
        Check.True(LaunchArguments.Parse([]) is { Paint.Count: 0, Lines.Count: 0, Center: null, MetresPerPixel: null, Level: null, Residency: false, HideHud: false }, "atlas options are off by default");

        foreach (var bad in new[] { new[] { "--bogus", "1" }, ["--fixture"], ["--seed", "1", "--seed", "2"], ["--select", "5"], ["--fail-after-ms", "-1"], ["--fail-at-ms", "1"], ["fixture", "F-EMPTY"], ["--speed", "0"], ["--speed", "6"], ["--speed", "fast"], ["--paused", "--paused"], ["--paint", "1,2,3"], ["--paint", "1,2,6000,1"], ["--paint", "1,2,3,2"], ["--paint", "1,2,0,1"], ["--line", "1,2"], ["--line", "1,2;3"], ["--zoom", "0"], ["--zoom", "near"], ["--level", "fine"], ["--level", "1"], ["--center", "1"] })
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
        Check.Equal("F-EMPTY@2", fixture.Selection!.FixtureReference, "fixture reference");

        var explicitSeed = Launch("--scenario", TestWorlds.FixtureScenario, "--seed", "42");
        Check.Equal(42UL, explicitSeed.World!.Seed, "explicit seed");
        Check.True(explicitSeed.Selection!.FixtureReference is null, "no fixture");
        Check.Equal(WorldLauncher.DefaultSeed, Launch().World!.Seed, "default seed without arguments");

        Check.Equal(0, fixture.Script!.Count, "F-EMPTY has no command stream");
        var kernel = Launch("--fixture", "F-KERNEL");
        Check.Equal(20261014UL, kernel.World!.Seed, "F-KERNEL pins its seed");
        Check.True(kernel.Script is { Count: > 0 } script && script.All(entry => entry.At >= kernel.World.Now), "F-KERNEL loads its command stream, none before the start");
        Check.True(kernel.World.Identity.Id != fixture.World.Identity.Id, "each launch creates a world with a fresh ID");

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
