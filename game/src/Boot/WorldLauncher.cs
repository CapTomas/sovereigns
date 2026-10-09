using Sovereigns.Simulation;
using Sovereigns.Simulation.Content;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Scenarios;

namespace Sovereigns.Client.Boot;

/// <summary>The world to run, or the reasons it cannot be created.</summary>
public sealed record LaunchResult(World? World, LaunchSelection? Selection, IReadOnlyList<string> Problems)
{
    public bool Succeeded => World is not null && Selection is not null;
}

/// <summary>Loads content and resolves the same fixture, scenario and seed selection rules as the headless host (SOV-P01-T08).</summary>
public static class WorldLauncher
{
    /// <summary>Seed used when neither a fixture nor a seed is given.</summary>
    public const ulong DefaultSeed = 1;

    public static LaunchResult Launch(LaunchArguments args, ClientPaths paths, Logger logger)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(paths);
        var content = ContentCatalog.Load(paths.ContentDirectory);
        if (!content.Succeeded)
        {
            return new(null, null,
            [
                $"Content in {paths.ContentDirectory} is invalid. Fix these files, or pass --content <dir>:",
                .. content.Errors.Select(error => error.ToString()),
            ]);
        }

        try
        {
            var selection = LaunchSelection.Resolve(content.Catalog, paths.ContentDirectory, paths.FixturesDirectory, paths.RepositoryRoot,
                args.Fixture, args.Scenario, args.Seed, DefaultSeed);
            return new(World.Create(selection.Scenario, selection.Seed, logger), selection, []);
        }
        catch (LaunchException ex)
        {
            return new(null, null, [ex.Message]);
        }
    }
}
