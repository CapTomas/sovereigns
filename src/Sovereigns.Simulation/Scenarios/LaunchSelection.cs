using System.Globalization;
using System.Text.Json;
using Sovereigns.Simulation.Content;

namespace Sovereigns.Simulation.Scenarios;

/// <summary>Raised when a fixture, scenario or seed selection cannot be honored; the message says how to fix it.</summary>
public sealed class LaunchException(string message) : Exception(message);

/// <summary>The scenario and seed a host will run, and the fixture that pinned them, if any.</summary>
public sealed record LaunchSelection(ScenarioDefinition Scenario, ulong Seed, string? FixtureReference)
{
    /// <summary>Scenario used when a host is started without a selection.</summary>
    public static readonly ContentId DefaultScenario = ContentId.Parse("core:scenario/empty_world");

    /// <summary>
    /// Resolves the same selection rules for every host (SOV-P01-T08): either a fixture, which pins seed and
    /// scenario, or a scenario ID with a seed. Without either, <see cref="DefaultScenario"/> runs with
    /// <paramref name="defaultSeed"/>.
    /// </summary>
    /// <param name="fixture">Fixture ID such as <c>F-EMPTY</c>, resolved in <paramref name="fixturesDirectory"/>, or a manifest path.</param>
    /// <param name="repositoryRoot">Directory that fixture scenario paths are relative to.</param>
    public static LaunchSelection Resolve(ContentCatalog catalog, string contentRoot, string? fixturesDirectory, string? repositoryRoot,
        string? fixture, string? scenario, string? seed, ulong defaultSeed)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if (fixture is not null)
        {
            if (scenario is not null || seed is not null)
            {
                throw new LaunchException("--fixture pins the seed and scenario; do not combine it with --scenario or --seed");
            }

            var manifest = LoadFixture(fixture, fixturesDirectory);
            if (repositoryRoot is null ||
                !ContentCatalog.TryIdFromPath(contentRoot, Path.Combine(repositoryRoot, manifest.Scenario!), out var fixtureScenario))
            {
                throw new LaunchException($"fixture {manifest.Reference} scenario '{manifest.Scenario}' is not a content/<namespace>/scenario/<name>.json path");
            }

            return new(GetScenario(catalog, fixtureScenario.ToString()), manifest.Seed!.Value, manifest.Reference);
        }

        if (scenario is null && seed is not null)
        {
            throw new LaunchException($"--seed needs --scenario, for example --scenario {DefaultScenario} --seed {seed}");
        }

        return new(GetScenario(catalog, scenario ?? DefaultScenario.ToString()), seed is null ? defaultSeed : ParseSeed(seed), null);
    }

    public static ulong ParseSeed(string text) =>
        ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new LaunchException($"--seed must be a whole number from 0 to {ulong.MaxValue}, found '{text}'");

    private static ScenarioDefinition GetScenario(ContentCatalog catalog, string idText)
    {
        if (!ContentId.TryParse(idText, out var id, out var error))
        {
            throw new LaunchException($"scenario: {error}");
        }

        try
        {
            return catalog.Get<ScenarioDefinition>(id);
        }
        catch (KeyNotFoundException ex)
        {
            throw new LaunchException(ex.Message);
        }
    }

    private static FixtureManifest LoadFixture(string fixture, string? fixturesDirectory)
    {
        var path = fixture.EndsWith(".json", StringComparison.Ordinal) || fixturesDirectory is null
            ? fixture
            : Path.Combine(fixturesDirectory, fixture + ".json");
        if (!File.Exists(path))
        {
            throw new LaunchException($"fixture {fixture} not found at {path}");
        }

        FixtureManifest manifest;
        try
        {
            manifest = FixtureManifest.Load(path);
        }
        catch (JsonException ex)
        {
            throw new LaunchException($"fixture {path} is malformed: {ex.Message}");
        }

        return manifest.IsDefined
            ? manifest
            : throw new LaunchException($"fixture {manifest.Reference} is {manifest.Status}; it has no seed and scenario yet");
    }
}
