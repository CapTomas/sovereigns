using System.Text.Json;
using Sovereigns.Headless;

namespace Sovereigns.Simulation.Tests;

/// <summary>Fixture invocation, failure reports and content validation through the headless runner (SOV-P01-T04, T08, T09, T13).</summary>
public sealed class HeadlessTests
{
    /// <summary>World time in milliseconds at the start of fixture F-EMPTY.</summary>
    private static readonly long Start = TestSupport.EmptyWorldStart.Milliseconds;

    [Fact]
    public void FixtureRunIsRepeatable()
    {
        var first = Run("run", "--fixture", "F-EMPTY", "--for-ms", "60000");
        var second = Run("run", "--fixture", "F-EMPTY", "--for-ms", "60000");

        Assert.Equal(HeadlessApp.Ok, first.Code);
        using var a = JsonDocument.Parse(first.Out);
        using var b = JsonDocument.Parse(second.Out);
        Assert.Equal("F-EMPTY@2", a.RootElement.GetProperty("fixture").GetString());
        Assert.Equal(Start + 60_000, a.RootElement.GetProperty("sim_time_ms").GetInt64());
        Assert.Equal("1203-03-01T06:01:00.000", a.RootElement.GetProperty("now").GetString());
        Assert.NotEqual(a.RootElement.GetProperty("world_id").GetString(), b.RootElement.GetProperty("world_id").GetString());
        Assert.Equal(a.RootElement.GetProperty("checksum").GetString(), b.RootElement.GetProperty("checksum").GetString());
    }

    [Fact]
    public void ScenarioNameAndSeedSelectTheSameWorldAsTheFixture()
    {
        var byFixture = Run("run", "--fixture", "F-EMPTY", "--for-ms", "5000");
        var byName = Run("run", "--scenario", "core:scenario/empty_world", "--seed", "20261009", "--for-ms", "5000");

        Assert.Equal(Checksum(byFixture.Out), Checksum(byName.Out));
    }

    [Fact]
    public void RecordedJournalReplaysThroughTheRunner()
    {
        var directory = TestSupport.NewTempDirectory();
        var journal = Path.Combine(directory, "journal.jsonl");
        var at = Start + 2_000;
        File.WriteAllText(journal, $$$"""
            {"sequence":1,"at":{{{at}}},"command":{"type":"place_marker","position":{"x":500,"y":700},"label":"ford"}}
            {"sequence":2,"at":{{{at}}},"command":{"type":"place_marker","position":{"x":-1,"y":700},"label":"outside"},"rejection":"marker position (-1 m E, 700 m N) is outside the world"}

            """);
        var record = Path.Combine(directory, "out.jsonl");

        var result = Run("run", "--fixture", "F-EMPTY", "--for-ms", "10000", "--commands", journal, "--record", record);

        Assert.Equal(HeadlessApp.Ok, result.Code);
        using var summary = JsonDocument.Parse(result.Out);
        Assert.Equal(2, summary.RootElement.GetProperty("commands").GetInt32());
        Assert.Equal(1, summary.RootElement.GetProperty("markers").GetInt32());
        Assert.Equal(File.ReadAllText(journal).Trim(), File.ReadAllText(record).Trim());
    }

    [Fact]
    public void InjectedFailureWritesAReportThatReplaysToTheSameChecksum()
    {
        var reports = TestSupport.NewTempDirectory();
        var log = Path.Combine(reports, "run.jsonl");

        var failed = Run("run", "--fixture", "F-EMPTY", "--fail-after-ms", "30000", "--reports", reports, "--log", log);

        Assert.Equal(HeadlessApp.Failed, failed.Code);
        var report = Assert.Single(Directory.GetFiles(reports, "failure-*.json"));
        Assert.EndsWith($"failure-seed20261009-t{Start + 30_000}ms.json", report, StringComparison.Ordinal);
        Assert.Contains("simulation failed", File.ReadAllText(log), StringComparison.Ordinal);
        Assert.Contains("reproduce: sovereigns-headless replay", failed.Err, StringComparison.Ordinal);

        var replay = Run("replay", report);

        Assert.Equal(HeadlessApp.Ok, replay.Code);
        Assert.Contains("checksum matches", replay.Out, StringComparison.Ordinal);
    }

    [Fact]
    public void TamperedReportIsAMismatch()
    {
        var reports = TestSupport.NewTempDirectory();
        Run("run", "--fixture", "F-EMPTY", "--fail-after-ms", "3000", "--reports", reports);
        var report = Assert.Single(Directory.GetFiles(reports, "failure-*.json"));
        var text = File.ReadAllText(report);
        var checksum = JsonDocument.Parse(text).RootElement.GetProperty("state_checksum").GetString()!;
        File.WriteAllText(report, text.Replace(checksum, new string('0', 64), StringComparison.Ordinal));

        Assert.Equal(HeadlessApp.Mismatch, Run("replay", report).Code);
    }

    [Fact]
    public void ReportWhoseCommandsNoLongerReplayIsAMismatchNotACrash()
    {
        var directory = TestSupport.NewTempDirectory();
        var journal = Path.Combine(directory, "journal.jsonl");
        File.WriteAllText(journal, $$$"""{"sequence":1,"at":{{{Start + 1_000}}},"command":{"type":"place_marker","position":{"x":500,"y":700},"label":"ford"}}""");
        Run("run", "--fixture", "F-EMPTY", "--commands", journal, "--fail-after-ms", "5000", "--reports", directory);
        var report = Assert.Single(Directory.GetFiles(directory, "failure-*.json"));
        // An edit that the original run would have rejected: the replayed journal now differs from the recorded one.
        File.WriteAllText(report, File.ReadAllText(report).Replace("\"x\": 500", "\"x\": -500", StringComparison.Ordinal));

        var replay = Run("replay", report);

        Assert.Equal(HeadlessApp.Mismatch, replay.Code);
        Assert.Contains("replay MISMATCH", replay.Out, StringComparison.Ordinal);
    }

    [Fact]
    public void FailedStepThatNoLongerFailsIsReportedAsNotReproduced()
    {
        var directory = TestSupport.NewTempDirectory();
        Run("run", "--fixture", "F-EMPTY", "--fail-after-ms", "2000", "--reports", directory);
        var report = Assert.Single(Directory.GetFiles(directory, "failure-*.json"));
        // Claim the step at 2000 ms failed with one pending command; replaying it completes normally.
        File.WriteAllText(report, File.ReadAllText(report)
            .Replace("\"failed_during\": \"none\"", "\"failed_during\": \"step\"", StringComparison.Ordinal));

        var replay = Run("replay", report);

        Assert.Equal(HeadlessApp.Mismatch, replay.Code);
        Assert.Contains("failure NOT reproduced", replay.Out, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(new[] { "run" }, "give exactly one of")]
    [InlineData(new[] { "run", "--fixture", "F-VALLEY" }, "is blank")]
    [InlineData(new[] { "run", "--scenario", "core:scenario/empty_world" }, "--scenario needs --seed")]
    [InlineData(new[] { "run", "--fixture", "F-EMPTY", "--for-ms", "1500" }, "whole number of 1000 ms steps")]
    [InlineData(new[] { "run", "--fixture", "F-EMPTY", "--for-ms", "1000", "--until-ms", "1000" }, "not both")]
    [InlineData(new[] { "run", "--fixture" }, "--fixture needs a value")]
    [InlineData(new[] { "frobnicate" }, "unknown command")]
    [InlineData(new[] { "run", "--fixture", "F-EMPTY", "--comands", "j.jsonl" }, "unknown option --comands for run")]
    [InlineData(new[] { "validate-content", "--seed", "1" }, "unknown option --seed")]
    public void UsageErrorsExplainTheFix(string[] args, string expected)
    {
        var result = Run(args);

        Assert.Equal(HeadlessApp.UsageError, result.Code);
        Assert.Contains(expected, result.Err, StringComparison.Ordinal);
    }

    [Fact]
    public void JournalOffTheStepGridIsRejectedBeforeRunning()
    {
        var journal = Path.Combine(TestSupport.NewTempDirectory(), "journal.jsonl");
        File.WriteAllText(journal, $$$"""{"sequence":1,"at":{{{Start + 1_500}}},"command":{"type":"remove_marker","marker":1}}""");

        var result = Run("run", "--fixture", "F-EMPTY", "--commands", journal);

        Assert.Equal(HeadlessApp.UsageError, result.Code);
        Assert.Contains("1000 ms step boundary", result.Err, StringComparison.Ordinal);
    }

    [Fact]
    public void KernelFixtureAppliesItsCommandStreamThroughDecisions()
    {
        var run = Run("run", "--fixture", "F-KERNEL");

        Assert.Equal(HeadlessApp.Ok, run.Code);
        using var summary = JsonDocument.Parse(run.Out);
        var root = summary.RootElement;
        Assert.Equal("F-KERNEL@1", root.GetProperty("fixture").GetString());
        Assert.Equal("1203-12-20T06:00:00.000", root.GetProperty("now").GetString());
        Assert.Equal((9, 3, 18, 0, false), (root.GetProperty("commands").GetInt32(), root.GetProperty("markers").GetInt32(),
            root.GetProperty("events").GetInt32(), root.GetProperty("pending_decisions").GetInt32(), root.GetProperty("stopped_at_decision").GetBoolean()));
        Assert.Equal(Checksum(run.Out), Checksum(Run("run", "--fixture", "F-KERNEL").Out));
    }

    [Fact]
    public void RunStopsAtADecisionTheJournalDoesNotAcknowledge()
    {
        var journal = Path.Combine(TestSupport.NewTempDirectory(), "journal.jsonl");
        File.WriteAllText(journal, $$$"""{"sequence":1,"at":{{{Start}}},"command":{"type":"set_breakpoint","at":{{{Start + 600_000}}},"label":"stop"}}""");

        var run = Run("run", "--fixture", "F-EMPTY", "--commands", journal);

        using var summary = JsonDocument.Parse(run.Out);
        Assert.True(summary.RootElement.GetProperty("stopped_at_decision").GetBoolean());
        Assert.Equal(Start + 600_000, summary.RootElement.GetProperty("sim_time_ms").GetInt64());
    }

    [Fact]
    public void SaveAndLoadContinueTheFixtureToTheSameState()
    {
        var directory = TestSupport.NewTempDirectory();
        var save = Path.Combine(directory, "day100.json");
        var full = Run("run", "--fixture", "F-KERNEL");
        var firstPart = Run("run", "--fixture", "F-KERNEL", "--for-ms", (100 * 86_400_000L).ToString(System.Globalization.CultureInfo.InvariantCulture), "--save", save);
        var commands = Path.Combine(directory, "rest.jsonl");
        var start = WorldCalendarStart("F-KERNEL");
        File.WriteAllLines(commands, File.ReadAllLines(Path.Combine(TestSupport.RepoRoot, "tests", "fixtures", "F-KERNEL.commands.jsonl"))
            .Where(line => JsonDocument.Parse(line).RootElement.GetProperty("at").GetInt64() >= start + 100 * 86_400_000L));

        var rest = Run("run", "--load", save, "--for-ms", (300 * 86_400_000L).ToString(System.Globalization.CultureInfo.InvariantCulture), "--commands", commands);

        Assert.Equal((HeadlessApp.Ok, HeadlessApp.Ok), (firstPart.Code, rest.Code));
        Assert.Equal(Checksum(full.Out), Checksum(rest.Out));
        using var summary = JsonDocument.Parse(rest.Out);
        Assert.Matches("^[0-9a-f]{64}$", summary.RootElement.GetProperty("base_save").GetString()!);
    }

    [Fact]
    public void FailureAfterLoadingReplaysFromThatSave()
    {
        var directory = TestSupport.NewTempDirectory();
        var save = Path.Combine(directory, "start.json");
        Run("run", "--fixture", "F-EMPTY", "--for-ms", "60000", "--save", save);
        Run("run", "--load", save, "--for-ms", "60000", "--fail-after-ms", "30000", "--reports", directory);
        var report = Assert.Single(Directory.GetFiles(directory, "failure-*.json"));

        Assert.Contains("--save", Run("replay", report).Err, StringComparison.Ordinal);
        var replay = Run("replay", report, "--save", save);

        Assert.Equal(HeadlessApp.Ok, replay.Code);
        Assert.Contains("checksum matches", replay.Out, StringComparison.Ordinal);
    }

    private static long WorldCalendarStart(string fixture) =>
        JsonDocument.Parse(Run("run", "--fixture", fixture, "--for-ms", "0").Out).RootElement.GetProperty("sim_time_ms").GetInt64();

    [Fact]
    public void ValidateContentAcceptsTheRepositoryContent()
    {
        var result = Run("validate-content");

        Assert.Equal(HeadlessApp.Ok, result.Code);
        Assert.StartsWith("content OK", result.Out, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateContentRejectsBrokenContent()
    {
        var root = Path.Combine(TestSupport.NewTempDirectory(), "content");
        Directory.CreateDirectory(Path.Combine(root, "core", "scenario"));
        File.WriteAllText(Path.Combine(root, "core", "scenario", "broken.json"), """{ "id": "core:scenario/broken" }""");

        var result = Run("validate-content", "--content", root);

        Assert.Equal(HeadlessApp.UsageError, result.Code);
        Assert.Contains("content/core/scenario/broken.json", result.Err, StringComparison.Ordinal);
    }

    private static string Checksum(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("checksum").GetString()!;
    }

    private static (int Code, string Out, string Err) Run(params string[] args)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var code = new HeadlessApp(stdout, stderr).Run([args[0], "--repo", TestSupport.RepoRoot, .. args[1..]]);
        return (code, stdout.ToString(), stderr.ToString());
    }
}
