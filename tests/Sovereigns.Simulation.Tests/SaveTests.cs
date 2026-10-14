using System.Text.Json.Nodes;
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
    [InlineData("format_version", "2", "newer than this build")]
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
