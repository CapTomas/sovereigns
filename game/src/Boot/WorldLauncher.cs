using Sovereigns.Simulation;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Content;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Identity;
using Sovereigns.Simulation.Scenarios;

namespace Sovereigns.Client.Boot;

/// <summary>The world to run with the fixture command stream it must replay, or the reasons it cannot be created.</summary>
public sealed record LaunchResult(World? World, LaunchSelection? Selection, IReadOnlyList<string> Problems, IReadOnlyList<JournalEntry>? Script = null)
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
            var script = selection.CommandsPath is { } path ? ReadScript(path, selection.Scenario) : [];

            // The world's identity is the host's to choose (ADR-0006); nothing in the simulation depends on it.
            var id = WorldId.FromBytes(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
            return new(World.Create(selection.Scenario, selection.Seed, logger, id), selection, [], script);
        }
        catch (LaunchException ex)
        {
            return new(null, null, [ex.Message]);
        }
    }

    private static List<JournalEntry> ReadScript(string path, ScenarioDefinition scenario)
    {
        List<JournalEntry> entries;
        try
        {
            using var reader = File.OpenText(path);
            entries = [.. CommandJournal.ReadJsonLines(reader, path)];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
        {
            throw new LaunchException($"cannot read the fixture command journal {path}: {ex.Message}");
        }

        if (entries.FirstOrDefault(entry => entry.At < scenario.StartTime || (entry.At.Milliseconds - scenario.StartTime.Milliseconds) % scenario.StepMs != 0) is { } misaligned)
        {
            throw new LaunchException($"fixture command journal {path}: entry {misaligned.Sequence} at {misaligned.At} is before the scenario start {scenario.StartTime} or not on a {scenario.StepMs} ms step boundary");
        }

        return entries;
    }
}
