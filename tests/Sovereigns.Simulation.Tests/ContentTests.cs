using Sovereigns.Simulation.Content;
using Sovereigns.Simulation.Scenarios;

namespace Sovereigns.Simulation.Tests;

public sealed class ContentTests
{
    private const string ValidScenario = """
        {
          "id": "core:scenario/sample",
          "description": "sample",
          "extent_m": { "east": 1000, "north": 500 },
          "step_ms": 1000,
          "duration_ms": 5000
        }
        """;

    [Fact]
    public void RepositoryContentIsValidAndContainsTheDefaultScenario()
    {
        var result = ContentCatalog.Load(TestSupport.ContentRoot);

        Assert.True(result.Succeeded, string.Join(Environment.NewLine, result.Errors));
        Assert.True(result.Catalog.TryGet<ScenarioDefinition>(LaunchSelection.DefaultScenario, out var scenario));
        Assert.True(scenario.ExtentM.IsValid);
    }

    [Theory]
    [InlineData("core:scenario/empty_world", true)]
    [InlineData("mod_2:crop/winter_wheat", true)]
    [InlineData("core:scenario", false)]
    [InlineData("Core:scenario/x", false)]
    [InlineData("core:scenario/x/y", false)]
    [InlineData("core:scen-ario/x", false)]
    [InlineData("core:scenario/1x", false)]
    [InlineData("a:b:c/d", false)]
    [InlineData("", false)]
    public void ContentIdsFollowNamespaceKindName(string text, bool valid) =>
        Assert.Equal(valid, ContentId.TryParse(text, out _, out _));

    [Fact]
    public void ValidDefinitionLoads()
    {
        var root = Tree(("core/scenario/sample.json", ValidScenario));

        var result = ContentCatalog.Load(root);

        Assert.True(result.Succeeded, string.Join(Environment.NewLine, result.Errors));
        Assert.Equal(1000, result.Catalog.Get<ScenarioDefinition>(ContentId.Parse("core:scenario/sample")).ExtentM.East);
    }

    [Fact]
    public void UnknownMemberNamesTheFileLineAndAllowedMembers()
    {
        var root = Tree(("core/scenario/sample.json", ValidScenario.Replace("\"step_ms\"", "\"stepms\"", StringComparison.Ordinal)));

        var error = Assert.Single(ContentCatalog.Load(root).Errors);

        Assert.Equal("content/core/scenario/sample.json", error.File);
        Assert.Equal(5, error.Line);
        Assert.Contains("stepms", error.ToString(), StringComparison.Ordinal);
        Assert.Contains("members are: id, description, extent_m, step_ms, duration_ms", error.Hint, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("\"step_ms\": 1000", "\"step_ms\": \"fast\"", "$.step_ms")]
    [InlineData("\"step_ms\": 1000", "\"step_ms\": 1.5", "$.step_ms")]
    [InlineData("\"description\": \"sample\"", "\"description\": null", "$.description")]
    [InlineData("\"id\": \"core:scenario/sample\"", "\"id\": \"core:Scenario/sample\"", "$.id")]
    public void MistypedValuesNameTheirJsonPath(string original, string replacement, string path)
    {
        var root = Tree(("core/scenario/sample.json", ValidScenario.Replace(original, replacement, StringComparison.Ordinal)));

        var error = Assert.Single(ContentCatalog.Load(root).Errors);

        Assert.Equal(path, error.JsonPath);
        Assert.NotNull(error.Line);
    }

    [Fact]
    public void MissingRequiredMemberIsReported()
    {
        var root = Tree(("core/scenario/sample.json", ValidScenario.Replace("\"duration_ms\": 5000", "\"duration_ms_x\": 1", StringComparison.Ordinal)));

        var errors = ContentCatalog.Load(root).Errors;

        Assert.Contains(errors, error => error.Message.Contains("duration_ms", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("\"step_ms\": 1000", "\"step_ms\": 0", "$.step_ms")]
    [InlineData("\"duration_ms\": 5000", "\"duration_ms\": 5500", "$.duration_ms")]
    [InlineData("\"east\": 1000", "\"east\": -1", "$.extent_m")]
    [InlineData("\"description\": \"sample\"", "\"description\": \" \"", "$.description")]
    public void SemanticRulesAreChecked(string original, string replacement, string path)
    {
        var root = Tree(("core/scenario/sample.json", ValidScenario.Replace(original, replacement, StringComparison.Ordinal)));

        var error = Assert.Single(ContentCatalog.Load(root).Errors);

        Assert.Equal(path, error.JsonPath);
    }

    [Fact]
    public void IdMustMatchTheFileLocation()
    {
        var root = Tree(("core/scenario/other.json", ValidScenario));

        var error = Assert.Single(ContentCatalog.Load(root).Errors);

        Assert.Equal("$.id", error.JsonPath);
        Assert.Contains("set id to 'core:scenario/other'", error.Hint, StringComparison.Ordinal);
    }

    [Fact]
    public void LayoutProblemsAreAllReportedTogether()
    {
        var root = Tree(
            ("core/scenario/sample.json", ValidScenario),
            ("core/scenario/Bad-Name.json", "{}"),
            ("core/scenario/notes.txt", ""),
            ("core/stray.json", "{}"),
            ("core/crops/wheat.json", "{}"),
            ("my-mod/scenario/x.json", "{}"),
            ("loose.json", "{}"),
            ("README.md", "notes are fine"));

        var errors = ContentCatalog.Load(root).Errors.Select(error => error.ToString()).ToList();

        Assert.Equal(6, errors.Count);
        Assert.Contains(errors, error => error.Contains("Bad-Name.json", StringComparison.Ordinal) && error.Contains("not a valid ID segment", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("notes.txt", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("stray.json", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("unknown content kind 'crops'", StringComparison.Ordinal) && error.Contains("use one of: scenario", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("namespace directory 'my-mod'", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("loose.json", StringComparison.Ordinal));
    }

    [Fact]
    public void UnknownReferenceSuggestsTheClosestDefinitionOfTheExpectedKind()
    {
        var root = Tree(
            ("core/commodity/wheat.json", """{ "id": "core:commodity/wheat" }"""),
            ("core/commodity/barley.json", """{ "id": "core:commodity/barley" }"""),
            ("core/recipe/bread.json", """{ "id": "core:recipe/bread", "inputs": ["core:commodity/wheat", "core:commodity/wheet"] }"""));

        var error = Assert.Single(ContentCatalog.Load(root, TestKinds).Errors);

        Assert.Equal("$.inputs[1]", error.JsonPath);
        Assert.Contains("unknown commodity reference 'core:commodity/wheet'", error.Message, StringComparison.Ordinal);
        Assert.Contains("did you mean 'core:commodity/wheat'?", error.Hint, StringComparison.Ordinal);
    }

    [Fact]
    public void ReferenceToTheWrongKindIsReported()
    {
        var root = Tree(
            ("core/recipe/flour.json", """{ "id": "core:recipe/flour", "inputs": [] }"""),
            ("core/recipe/bread.json", """{ "id": "core:recipe/bread", "inputs": ["core:recipe/flour"] }"""));

        var error = Assert.Single(ContentCatalog.Load(root, TestKinds).Errors);

        Assert.Contains("is a recipe, but this field needs a commodity", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void HiddenFilesAreIgnored()
    {
        var root = Tree(
            ("core/scenario/sample.json", ValidScenario),
            (".DS_Store", "x"),
            ("core/.DS_Store", "x"),
            ("core/scenario/.sample.json.swp", "x"),
            (".git/config", "x"));

        Assert.True(ContentCatalog.Load(root).Succeeded);
    }

    [Fact]
    public void MissingDirectoryIsAnError() =>
        Assert.False(ContentCatalog.Load(Path.Combine(TestSupport.NewTempDirectory(), "absent")).Succeeded);

    private static readonly Dictionary<string, Type> TestKinds = new()
    {
        ["commodity"] = typeof(TestCommodity),
        ["recipe"] = typeof(TestRecipe),
    };

    private static string Tree(params (string Path, string Text)[] files)
    {
        var root = Path.Combine(TestSupport.NewTempDirectory(), "content");
        foreach (var (path, text) in files)
        {
            var full = Path.Combine(root, path);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, text);
        }

        Directory.CreateDirectory(root);
        return root;
    }

    private sealed record TestCommodity(ContentId Id) : IContentDefinition
    {
        public IEnumerable<ContentIssue> Validate() => [];

        public IEnumerable<ContentReference> References() => [];
    }

    private sealed record TestRecipe(ContentId Id, IReadOnlyList<ContentId> Inputs) : IContentDefinition
    {
        public IEnumerable<ContentIssue> Validate() => [];

        public IEnumerable<ContentReference> References() =>
            Inputs.Select((input, index) => new ContentReference($"$.inputs[{index}]", input, "commodity"));
    }
}
