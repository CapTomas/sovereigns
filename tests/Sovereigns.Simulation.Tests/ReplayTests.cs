using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Markers;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Tests;

public sealed class ReplayTests
{
    [Fact]
    public void JournalWrittenToJsonLinesReplaysToTheSameState()
    {
        var scenario = TestSupport.Scenario(stepMs: 500);
        var original = World.Create(scenario, seed: 99);
        original.AdvanceTo(new SimTime(1_000));
        original.Submit(new PlaceMarker(new WorldPoint(100.25, 200.5), "a"));
        original.Submit(new PlaceMarker(new WorldPoint(50_000, 1), "rejected: outside"));
        original.Step();
        original.AdvanceTo(new SimTime(4_000));
        original.Submit(new RemoveMarker(new MarkerId(1)));
        original.Submit(new PlaceMarker(new WorldPoint(7, 8), "b"));
        original.AdvanceTo(new SimTime(10_000));

        using var text = new StringWriter();
        CommandJournal.WriteJsonLines(original.Journal.Entries, text);
        var journal = CommandJournal.ReadJsonLines(new StringReader(text.ToString()), "memory");
        var replayed = Replay.Run(scenario, 99, journal, original.Now);

        Assert.Equal(original.Journal.Entries, journal);
        Assert.Equal(original.ComputeChecksum(), replayed.ComputeChecksum());
        Assert.Equal(original.Journal.Entries, replayed.Journal.Entries);
    }

    [Fact]
    public void JournalUsesStableSnakeCaseJson()
    {
        var world = World.Create(TestSupport.Scenario(), seed: 1);
        world.Submit(new PlaceMarker(new WorldPoint(1.5, 2), "x"));
        world.Step();
        using var text = new StringWriter();

        CommandJournal.WriteJsonLines(world.Journal.Entries, text);

        Assert.Equal("""{"sequence":1,"at":0,"command":{"type":"place_marker","position":{"x":1.5,"y":2},"label":"x"}}""" + "\n", text.ToString());
    }

    [Theory]
    [InlineData("""{"sequence":2,"at":0,"command":{"type":"remove_marker","marker":1}}""", "expected sequence 1")]
    [InlineData("""{"sequence":1,"at":0,"command":{"type":"teleport"}}""", "invalid journal entry")]
    [InlineData("""{"sequence":1,"at":0}""", "invalid journal entry")]
    [InlineData("not json", "invalid journal entry")]
    public void MalformedJournalLinesAreRejectedWithTheirLineNumber(string line, string expected)
    {
        var error = Assert.Throws<FormatException>(() => CommandJournal.ReadJsonLines(new StringReader(line), "j.jsonl"));

        Assert.StartsWith("j.jsonl:1:", error.Message, StringComparison.Ordinal);
        Assert.Contains(expected, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void JournalTimeMustNotGoBackwards()
    {
        const string text = """
            {"sequence":1,"at":2000,"command":{"type":"remove_marker","marker":1}}
            {"sequence":2,"at":1000,"command":{"type":"remove_marker","marker":1}}
            """;

        var error = Assert.Throws<FormatException>(() => CommandJournal.ReadJsonLines(new StringReader(text), "j.jsonl"));

        Assert.StartsWith("j.jsonl:2:", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectedNonFiniteCommandsStillRoundTripAndReplay()
    {
        var scenario = TestSupport.Scenario();
        var world = World.Create(scenario, seed: 3);
        world.Submit(new PlaceMarker(new WorldPoint(double.NaN, double.PositiveInfinity), "bad"));
        world.AdvanceTo(new SimTime(2_000));
        using var text = new StringWriter();

        CommandJournal.WriteJsonLines(world.Journal.Entries, text);
        var journal = CommandJournal.ReadJsonLines(new StringReader(text.ToString()), "memory");

        Assert.Contains("\"x\":\"NaN\"", text.ToString(), StringComparison.Ordinal);
        Assert.NotNull(Assert.Single(journal).Rejection);
        Assert.Equal(world.ComputeChecksum(), Replay.Run(scenario, 3, journal, world.Now).ComputeChecksum());
    }

    [Fact]
    public void ReplayRefusesAJournalThatDoesNotReproduce()
    {
        var scenario = TestSupport.Scenario();
        JournalEntry[] tampered = [new(1, SimTime.Zero, new PlaceMarker(new WorldPoint(1, 1), "a"), Rejection: "invented")];

        var error = Assert.Throws<InvalidOperationException>(() => Replay.Run(scenario, 1, tampered, new SimTime(1_000)));

        Assert.Contains("diverged", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReplayRefusesAnEndTimeBeforeTheLastCommand()
    {
        JournalEntry[] journal = [new(1, new SimTime(5_000), new PlaceMarker(new WorldPoint(1, 1), "a"))];

        Assert.Throws<InvalidOperationException>(() => Replay.Run(TestSupport.Scenario(), 1, journal, new SimTime(5_000)));
    }
}
