using System.Text.Json;
using Sovereigns.Headless;

namespace Sovereigns.Simulation.Tests;

/// <summary>Fixture invocation, failure reports and content validation through the headless runner (SOV-P01-T04, T08, T09, T13).</summary>
public sealed class HeadlessTests
{
    [Fact]
    public void FixtureRunIsRepeatable()
    {
        var first = Run("run", "--fixture", "F-EMPTY", "--until-ms", "60000");
        var second = Run("run", "--fixture", "F-EMPTY", "--until-ms", "60000");

        Assert.Equal(HeadlessApp.Ok, first.Code);
        using var a = JsonDocument.Parse(first.Out);
        using var b = JsonDocument.Parse(second.Out);
        Assert.Equal("F-EMPTY@1", a.RootElement.GetProperty("fixture").GetString());
        Assert.Equal(60_000, a.RootElement.GetProperty("sim_time_ms").GetInt64());
        Assert.Equal(a.RootElement.GetProperty("checksum").GetString(), b.RootElement.GetProperty("checksum").GetString());
    }

    [Fact]
    public void ScenarioNameAndSeedSelectTheSameWorldAsTheFixture()
    {
        var byFixture = Run("run", "--fixture", "F-EMPTY", "--until-ms", "5000");
        var byName = Run("run", "--scenario", "core:scenario/empty_world", "--seed", "20261009", "--until-ms", "5000");

        Assert.Equal(Checksum(byFixture.Out), Checksum(byName.Out));
    }

    [Fact]
    public void RecordedJournalReplaysThroughTheRunner()
    {
        var directory = TestSupport.NewTempDirectory();
        var journal = Path.Combine(directory, "journal.jsonl");
        File.WriteAllText(journal, """
            {"sequence":1,"at":2000,"command":{"type":"place_marker","position":{"x":500,"y":700},"label":"ford"}}
            {"sequence":2,"at":2000,"command":{"type":"place_marker","position":{"x":-1,"y":700},"label":"outside"},"rejection":"marker position (-1 m E, 700 m N) is outside the world"}

            """);
        var record = Path.Combine(directory, "out.jsonl");

        var result = Run("run", "--fixture", "F-EMPTY", "--until-ms", "10000", "--commands", journal, "--record", record);

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

        var failed = Run("run", "--fixture", "F-EMPTY", "--fail-at-ms", "30000", "--reports", reports, "--log", log);

        Assert.Equal(HeadlessApp.Failed, failed.Code);
        var report = Assert.Single(Directory.GetFiles(reports, "failure-*.json"));
        Assert.EndsWith("failure-seed20261009-t30000ms.json", report, StringComparison.Ordinal);
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
        Run("run", "--fixture", "F-EMPTY", "--fail-at-ms", "3000", "--reports", reports);
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
        File.WriteAllText(journal, """{"sequence":1,"at":1000,"command":{"type":"place_marker","position":{"x":500,"y":700},"label":"ford"}}""");
        Run("run", "--fixture", "F-EMPTY", "--commands", journal, "--fail-at-ms", "5000", "--reports", directory);
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
        Run("run", "--fixture", "F-EMPTY", "--fail-at-ms", "2000", "--reports", directory);
        var report = Assert.Single(Directory.GetFiles(directory, "failure-*.json"));
        // Claim the step at 2000 ms failed with one pending command; replaying it completes normally.
        File.WriteAllText(report, File.ReadAllText(report)
            .Replace("\"failed_in_step\": false", "\"failed_in_step\": true", StringComparison.Ordinal)
            .Replace("\"pending_commands\": []", "\"pending_commands\": [{\"type\": \"remove_marker\", \"marker\": 1}]", StringComparison.Ordinal));

        var replay = Run("replay", report);

        Assert.Equal(HeadlessApp.Mismatch, replay.Code);
        Assert.Contains("failure NOT reproduced", replay.Out, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(new[] { "run" }, "give --fixture")]
    [InlineData(new[] { "run", "--fixture", "F-VALLEY" }, "is blank")]
    [InlineData(new[] { "run", "--scenario", "core:scenario/empty_world" }, "--scenario needs --seed")]
    [InlineData(new[] { "run", "--fixture", "F-EMPTY", "--until-ms", "1500" }, "multiple of the scenario step")]
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
        File.WriteAllText(journal, """{"sequence":1,"at":1500,"command":{"type":"remove_marker","marker":1}}""");

        var result = Run("run", "--fixture", "F-EMPTY", "--commands", journal);

        Assert.Equal(HeadlessApp.UsageError, result.Code);
        Assert.Contains("not on a 1000 ms step boundary", result.Err, StringComparison.Ordinal);
    }

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
