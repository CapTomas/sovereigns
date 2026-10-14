using Sovereigns.Simulation.Content;
using Sovereigns.Simulation.Scenarios;

namespace Sovereigns.Simulation.Tests;

public sealed class LaunchSelectionTests
{
    private static readonly ContentCatalog Catalog = ContentCatalog.Load(TestSupport.ContentRoot).Catalog;
    private static readonly string Fixtures = Path.Combine(TestSupport.RepoRoot, "tests", "fixtures");

    private static LaunchSelection Resolve(string? fixture = null, string? scenario = null, string? seed = null) =>
        LaunchSelection.Resolve(Catalog, TestSupport.ContentRoot, Fixtures, TestSupport.RepoRoot, fixture, scenario, seed, defaultSeed: 5);

    [Fact]
    public void FixturePinsSeedAndScenario()
    {
        var selection = Resolve(fixture: "F-EMPTY");

        Assert.Equal("F-EMPTY@1", selection.FixtureReference);
        Assert.Equal(20261009UL, selection.Seed);
        Assert.Equal(LaunchSelection.DefaultScenario, selection.Scenario.Id);
    }

    [Fact]
    public void ScenarioAndSeedSelectAWorldByName()
    {
        var selection = Resolve(scenario: "core:scenario/empty_world", seed: "18446744073709551615");

        Assert.Equal(ulong.MaxValue, selection.Seed);
        Assert.Null(selection.FixtureReference);
    }

    [Fact]
    public void NoSelectionUsesTheDefaultScenarioAndHostSeed() =>
        Assert.Equal((LaunchSelection.DefaultScenario, 5UL), (Resolve().Scenario.Id, Resolve().Seed));

    [Theory]
    [InlineData("F-EMPTY", "core:scenario/empty_world", null, "do not combine")]
    [InlineData(null, null, "3", "--seed needs --scenario")]
    [InlineData(null, "core:scenario/empty_wrld", "3", "did you mean 'core:scenario/empty_world'?")]
    [InlineData(null, "empty_world", "3", "not namespace:kind/name")]
    [InlineData(null, "core:scenario/empty_world", "-3", "--seed must be a whole number")]
    [InlineData("F-VALLEY", null, null, "F-VALLEY@1 is blank")]
    [InlineData("F-NOPE", null, null, "not found")]
    public void InvalidSelectionsExplainTheFix(string? fixture, string? scenario, string? seed, string expected)
    {
        var error = Assert.Throws<LaunchException>(() => Resolve(fixture, scenario, seed));

        Assert.Contains(expected, error.Message, StringComparison.Ordinal);
    }
}
