using System.Text.Json.Nodes;
using Sovereigns.Simulation.Atlas;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Identity;
using Sovereigns.Simulation.Persistence;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Tests;

/// <summary>SOV-P02-T12, T17: save format, integrity checks and reference validation (ADR-0008).</summary>
public sealed class SaveTests
{
    private const long Day = WorldCalendar.MillisecondsPerDay;

    [Fact]
    public void SaveLoadContinueEqualsAnUninterruptedRun()
    {
        var uninterrupted = Timeline(World.Create(TestSupport.Scenario(), seed: 8, id: new WorldId(5)), fromDay: 0);
        var first = Timeline(World.Create(TestSupport.Scenario(), seed: 8, id: new WorldId(5)), fromDay: 0, toDay: 12);
        Assert.Single(first.PendingDecisions);

        var bytes = Save(first);
        var loaded = SaveFile.Load(new MemoryStream(bytes));

        Assert.Equal(first.ComputeChecksum(), loaded.ComputeChecksum());
        Assert.Equal(first.Identity, loaded.Identity);
        Assert.Equal(first.Journal.NextSequence, loaded.Journal.NextSequence);
        Assert.NotNull(loaded.BaseSaveSha256);
        Assert.Equal(uninterrupted.ComputeChecksum(), Timeline(loaded, fromDay: 12).ComputeChecksum());
    }

    [Fact]
    public void SavingTwiceGivesIdenticalBytes()
    {
        var world = Timeline(World.Create(TestSupport.Scenario(), seed: 8), fromDay: 0, toDay: 40);

        Assert.Equal(Save(world), Save(SaveFile.Load(new MemoryStream(Save(world)))));
    }

    [Fact]
    public void AFaultedWorldCannotBeSaved()
    {
        var world = World.Create(TestSupport.Scenario(), seed: 8);
        Assert.Throws<NotSupportedException>(() => world.Submit(new UnhandledCommand()));

        Assert.Throws<InvalidOperationException>(() => SaveFile.Capture(world));
    }

    [Theory]
    [InlineData("format", "\"other-save\"", "is not 'sovereigns-save'")]
    [InlineData("format_version", "3", "newer than this build")]
    [InlineData("format_version", "0", "no declared migration")]
    [InlineData("rules_version", "999", "rules version 999")]
    public void HeaderMismatchesAreRefused(string field, string value, string expected)
    {
        var json = JsonNode.Parse(Save(Timeline(World.Create(TestSupport.Scenario(), seed: 8), fromDay: 0, toDay: 5)))!;
        json[field] = JsonNode.Parse(value);

        var error = Assert.Throws<SaveLoadException>(() => SaveFile.Load(Stream(json)));
        Assert.Contains(expected, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnEditedPayloadFailsItsIntegrityCheck()
    {
        var json = JsonNode.Parse(Save(Timeline(World.Create(TestSupport.Scenario(), seed: 8), fromDay: 0, toDay: 5)))!;
        json["payload"]!["state"]!["markers"]![0]!["label"] = "forged";

        var error = Assert.Throws<SaveLoadException>(() => SaveFile.Load(Stream(json)));
        Assert.Contains("SHA-256", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("markers", "id", "999", "marker:999 was never issued")]
    [InlineData("breakpoints", "set_by", "999", "missing set event event:999")]
    [InlineData("pending_decisions", "event", "1", "missing event event:1")]
    public void BrokenReferencesAreReportedNotInvented(string collection, string member, string value, string expected)
    {
        var world = Timeline(World.Create(TestSupport.Scenario(), seed: 8), fromDay: 0, toDay: 12);
        world.Submit(new SetBreakpoint(world.Now.Plus(50 * Day), "later"));
        var json = JsonNode.Parse(Save(world))!;
        json["payload"]!["state"]![collection]![0]![member] = JsonNode.Parse(value);
        Rehash(json);

        var error = Assert.Throws<SaveLoadException>(() => SaveFile.Load(Stream(json)));
        Assert.Contains(expected, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AtlasStateSurvivesSaveAndLoad()
    {
        var world = World.Create(TestSupport.Scenario(), seed: 8);
        world.Submit(new PaintScratch(new WorldPoint(2_000, 2_000), 300, 0.75));
        world.Submit(new DrawSurveyLine([new WorldPoint(100, 100), new WorldPoint(5_000, 3_000), new WorldPoint(9_000, 100)], "ridge"));
        world.AdvanceTo(new SimTime(Day));
        var point = new WorldPoint(2_100, 1_950);

        var loaded = SaveFile.Load(new MemoryStream(Save(world)));

        Assert.Equal(world.ComputeChecksum(), loaded.ComputeChecksum());
        Assert.Equal(world.Atlas.Sample("scratch", point).Values, loaded.Atlas.Sample("scratch", point).Values);
        Assert.Equal(world.Atlas.Sample("scratch_gradient", point).Values, loaded.Atlas.Sample("scratch_gradient", point).Values);
        Assert.Equal(world.Features[0].Points, loaded.Features[0].Points);
        Assert.Equal(world.Features[0].Id, loaded.Features[0].Id);
    }

    [Fact]
    public void AFormatOneSaveMigratesAndContinuesLikeTheUninterruptedRun()
    {
        // SOV-P03-T15: a real Phase 02 save (F-KERNEL on day 20, format 1) loads through the declared migration,
        // and continuing the fixture's journal reaches the same state as running the fixture in one go.
        var repo = TestSupport.RepoRoot;
        using var stream = File.OpenRead(Path.Combine(repo, "tests", "Sovereigns.Simulation.Tests", "Data", "save-format1-F-KERNEL-day20.json"));
        var loaded = SaveFile.Load(stream);
        var journal = CommandJournal.ReadJsonLines(new StringReader(File.ReadAllText(Path.Combine(repo, "tests", "fixtures", "F-KERNEL.commands.jsonl"))), "F-KERNEL");
        var catalog = Content.ContentCatalog.Load(TestSupport.ContentRoot).Catalog;
        var scenario = catalog.Get<Scenarios.ScenarioDefinition>(Content.ContentId.Parse("core:scenario/kernel_year"));
        var end = scenario.StartTime.Plus(scenario.DurationMs);

        var continued = Replay.Continue(loaded, [.. journal.Where(entry => entry.Sequence >= loaded.Journal.NextSequence)], end);
        var uninterrupted = Replay.Run(scenario, 20261014, journal, end);

        Assert.Equal(WorldCalendar.FromCalendar(1202, 12, 5, 6), loaded.Identity.Start.Plus(20 * Day));
        Assert.Empty(loaded.Features);
        Assert.Equal(uninterrupted.ComputeChecksum(), continued.ComputeChecksum());
    }

    [Theory]
    [InlineData("field", "\"elevation\"", "is not stored by this build")]
    [InlineData("schema", "2", "schema 2")]
    [InlineData("level", "\"regional\"", "at Regional")]
    public void FieldDataThisBuildCannotHonourIsRefused(string member, string value, string expected)
    {
        var world = World.Create(TestSupport.Scenario(), seed: 8);
        world.Submit(new PaintScratch(new WorldPoint(2_000, 2_000), 50, 0.5));
        var json = JsonNode.Parse(Save(world))!;
        json["payload"]!["state"]!["fields"]![0]![member] = JsonNode.Parse(value);
        Rehash(json);

        var error = Assert.Throws<SaveLoadException>(() => SaveFile.Load(Stream(json)));
        Assert.Contains(expected, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ChangeTimesSurviveSaveAndLoadIncludingErasures()
    {
        var world = World.Create(TestSupport.Scenario(), seed: 8);
        world.Submit(new PaintScratch(new WorldPoint(2_000, 2_000), 300, 0.75));
        world.AdvanceTo(new SimTime(Day));
        world.Submit(new PaintScratch(new WorldPoint(2_100, 2_000), 100, 0));

        var loaded = SaveFile.Load(new MemoryStream(Save(world)));

        foreach (var level in Enum.GetValues<GridLevel>())
        {
            foreach (var field in new[] { "scratch", "scratch_gradient" })
            {
                foreach (var point in new WorldPoint[] { new(2_100, 2_000), new(1_850, 2_000), new(5_000, 5_000) })
                {
                    Assert.Equal(world.Atlas.Sample(field, point, level).ChangedAt, loaded.Atlas.Sample(field, point, level).ChangedAt);
                }
            }
        }

        Assert.Equal(new SimTime(Day), loaded.Atlas.Sample("scratch", new WorldPoint(2_100, 2_000)).ChangedAt);
    }

    [Theory]
    [InlineData("features", "id", "\"survey:01\"", "was never issued")]
    [InlineData("features", "label", "\"a\\u0001b\"", "without control characters")]
    [InlineData("features", "points", "[{\"x\": 100, \"y\": 100}, {\"x\": 100, \"y\": 100}]", "distinct consecutive points")]
    [InlineData("features", "created_at", "-1000", "outside the world's history")]
    [InlineData("overrides", "at", "-1000", "a time outside the world's history")]
    public void AtlasRecordsThatBreakTheirRulesAreReported(string section, string member, string value, string expected)
    {
        var world = World.Create(TestSupport.Scenario(), seed: 8);
        world.AdvanceTo(new SimTime(Day));
        world.Submit(new PaintScratch(new WorldPoint(2_000, 2_000), 50, 0.5));
        world.Submit(new DrawSurveyLine([new WorldPoint(100, 100), new WorldPoint(900, 100)], "road"));
        var json = JsonNode.Parse(Save(world))!;
        var state = json["payload"]!["state"]!;
        var target = section == "features" ? state["features"]![0]! : state["fields"]![0]!["overrides"]![0]!;
        target[member] = JsonNode.Parse(value);
        Rehash(json);

        var error = Assert.Throws<SaveLoadException>(() => SaveFile.Load(Stream(json)));
        Assert.Contains(expected, error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("identity", "generator_version", "3", "generator version 3")]
    [InlineData("state", "id_counters", "{\"event\": 99, \"wizard\": 1}", "'wizard'")]
    [InlineData("state", "commands_applied", "1", "names command #2")]
    public void IdentityAndStateInconsistenciesAreReported(string section, string member, string value, string expected)
    {
        var json = JsonNode.Parse(Save(Timeline(World.Create(TestSupport.Scenario(), seed: 8), fromDay: 0, toDay: 12)))!;
        json["payload"]![section]![member] = JsonNode.Parse(value);
        Rehash(json);

        var error = Assert.Throws<SaveLoadException>(() => SaveFile.Load(Stream(json)));
        Assert.Contains(expected, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnOutOfOrderLedgerIsReportedAsAProblem()
    {
        var json = JsonNode.Parse(Save(Timeline(World.Create(TestSupport.Scenario(), seed: 8), fromDay: 0, toDay: 12)))!;
        var ledger = json["payload"]!["ledger"]!.AsArray();
        var first = ledger[0]!.DeepClone();
        ledger.RemoveAt(0);
        ledger.Add(first);
        Rehash(json);

        var error = Assert.Throws<SaveLoadException>(() => SaveFile.Load(Stream(json)));
        Assert.Contains("does not follow", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A fixed command timeline: markers, a breakpoint on day 12 that stops time and is acknowledged when reached,
    /// more markers. Runs the steps on days [<paramref name="fromDay"/>, <paramref name="toDay"/>), then advances to
    /// <paramref name="toDay"/>.
    /// </summary>
    private static World Timeline(World world, int fromDay, int toDay = 60)
    {
        (int Day, Func<World, SimCommand> Command)[] steps =
        [
            (0, _ => new PlaceMarker(new WorldPoint(100, 200), "a")),
            (0, _ => new SetBreakpoint(new SimTime(12 * Day), "check")),
            (5, _ => new PlaceMarker(new WorldPoint(300, 400), "b")),
            (12, w => new AcknowledgeDecision(w.PendingDecisions.Single().Event)),
            (12, _ => new RemoveMarker(new Ids.EntityId<Ids.Kind.Marker>(1))),
            (30, _ => new PlaceMarker(new WorldPoint(500, 600), "c")),
        ];
        foreach (var (day, command) in steps.Where(step => step.Day >= fromDay && step.Day < toDay))
        {
            Assert.Equal(new SimTime(day * Day), world.AdvanceTo(new SimTime(day * Day)));
            Assert.Null(world.Submit(command(world)).Rejection);
        }

        world.AdvanceTo(new SimTime(toDay * Day));
        return world;
    }

    private static byte[] Save(World world)
    {
        using var stream = new MemoryStream();
        SaveFile.Capture(world).WriteTo(stream);
        return stream.ToArray();
    }

    private static MemoryStream Stream(JsonNode json) => new(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString()));

    /// <summary>Recomputes the payload hash, so that a test reaches the reference checks behind the integrity check.</summary>
    private static void Rehash(JsonNode json)
    {
        var payload = System.Text.Json.JsonSerializer.Deserialize<SavePayload>(json["payload"]!.ToJsonString(), Serialization.SimJson.Options)!;
        var bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(payload, Serialization.SimJson.Options);
        json["payload_sha256"] = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes));
    }

    private sealed record UnhandledCommand : SimCommand;
}
