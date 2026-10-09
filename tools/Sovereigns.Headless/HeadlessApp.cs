using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Sovereigns.Simulation;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Content;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Scenarios;
using Sovereigns.Simulation.Serialization;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Headless;

/// <summary>Command-line host for the simulation without the game client (SOV-P01-T04, T08, T09, T13).</summary>
internal sealed class HeadlessApp(TextWriter stdout, TextWriter stderr)
{
    public const int Ok = 0;
    public const int Failed = 1;
    public const int UsageError = 2;
    public const int Mismatch = 3;

    private const string Usage = """
        Usage: sovereigns-headless <command> [options]

          run --fixture <F-ID|path> | --scenario <content-id> --seed <n>
              [--until-ms <ms>] [--commands <journal.jsonl>] [--record <journal.jsonl>]
              [--log <file.jsonl>] [--reports <dir>] [--fail-at-ms <ms>] [--repo <dir>]
              Runs the world headless and prints a JSON summary with the state checksum.
              --fail-at-ms injects a failure to exercise failure reporting.
          replay <failure-report.json> [--repo <dir>]
              Rebuilds the reported world from seed, scenario and commands and compares checksums,
              or re-runs a failing step to reproduce its exception.
          validate-content [--content <dir>] [--repo <dir>]
              Validates content definitions and prints actionable errors.

        Exit codes: 0 success, 1 simulation failure (report written), 2 usage or content error, 3 replay mismatch.
        """;

    private static readonly Dictionary<string, string[]> AllowedOptions = new(StringComparer.Ordinal)
    {
        ["run"] = ["fixture", "scenario", "seed", "until-ms", "commands", "record", "log", "reports", "fail-at-ms", "repo"],
        ["replay"] = ["repo"],
        ["validate-content"] = ["content", "repo"],
    };

    public int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            stdout.WriteLine(Usage);
            return args.Length == 0 ? UsageError : Ok;
        }

        try
        {
            var options = Options.Parse(args.Skip(1));
            if (AllowedOptions.TryGetValue(args[0], out var allowed) && options.Names.FirstOrDefault(name => !allowed.Contains(name)) is { } unknown)
            {
                throw new UsageException($"unknown option --{unknown} for {args[0]}; options: {string.Join(", ", allowed.Select(name => "--" + name))}");
            }

            return args[0] switch
            {
                "run" => RunWorld(options),
                "replay" => ReplayReport(options),
                "validate-content" => ValidateContent(options),
                _ => throw new UsageException($"unknown command '{args[0]}'"),
            };
        }
        catch (UsageException ex)
        {
            stderr.WriteLine($"error: {ex.Message}");
            stderr.WriteLine("Run 'sovereigns-headless --help' for usage.");
            return UsageError;
        }
    }

    private int ValidateContent(Options options)
    {
        var root = options.Get("content") ?? Path.Combine(RepoRoot(options), "content");
        var result = ContentCatalog.Load(root);
        foreach (var error in result.Errors)
        {
            stderr.WriteLine(error);
        }

        stdout.WriteLine(result.Succeeded
            ? $"content OK: {result.Catalog.Ids.Count} definitions in {root}"
            : $"content INVALID: {result.Errors.Count} error(s) in {root}");
        return result.Succeeded ? Ok : UsageError;
    }

    private int RunWorld(Options options)
    {
        var repo = RepoRoot(options);
        var catalog = LoadContent(repo);
        if (options.Get("fixture") is null && options.Get("scenario") is null)
        {
            throw new UsageException("give --fixture <F-ID>, or --scenario <content-id> with --seed <n>");
        }

        if (options.Get("scenario") is not null && options.Get("seed") is null)
        {
            throw new UsageException("--scenario needs --seed");
        }

        LaunchSelection selection;
        try
        {
            selection = LaunchSelection.Resolve(catalog, Path.Combine(repo, "content"), Path.Combine(repo, "tests", "fixtures"), repo,
                options.Get("fixture"), options.Get("scenario"), options.Get("seed"), defaultSeed: 0);
        }
        catch (LaunchException ex)
        {
            throw new UsageException(ex.Message);
        }

        var scenario = selection.Scenario;
        var seed = selection.Seed;
        var fixtureReference = selection.FixtureReference;
        var until = new SimTime(options.Get("until-ms") is { } untilText ? ParseInt64(untilText, "--until-ms") : scenario.DurationMs);
        var failAt = options.Get("fail-at-ms") is { } failText ? new SimTime(ParseInt64(failText, "--fail-at-ms")) : (SimTime?)null;
        var journal = options.Get("commands") is { } commandsPath ? ReadJournal(commandsPath) : [];
        var reports = options.Get("reports") ?? Path.Combine(repo, "artifacts", "reports");

        using var logFile = options.Get("log") is { } logPath ? CreateText(logPath) : null;
        var recent = new RecentLogBuffer(200);
        var sinks = new List<ILogSink> { new TextLogSink(stderr), recent };
        if (logFile is not null)
        {
            sinks.Add(new JsonLinesLogSink(logFile));
        }

        var logger = new Logger(sinks, LogSeverity.Info, TimeProvider.System);
        var log = logger.For("headless");
        var world = World.Create(scenario, seed, logger);
        if (until.Milliseconds < 0 || until.Milliseconds % scenario.StepMs != 0)
        {
            throw new UsageException($"--until-ms must be a non-negative multiple of the scenario step ({scenario.StepMs} ms)");
        }

        if (journal.FirstOrDefault(entry => entry.At.Milliseconds < 0 || entry.At.Milliseconds % scenario.StepMs != 0) is { } misaligned)
        {
            throw new UsageException($"journal entry {misaligned.Sequence} at {misaligned.At} is not on a {scenario.StepMs} ms step boundary of {scenario.Id}");
        }

        var stepCost = Stopwatch.StartNew();
        try
        {
            var next = 0;
            while (world.Now < until)
            {
                if (failAt is { } failure && world.Now >= failure)
                {
                    throw new InjectedFailureException($"failure injected by --fail-at-ms at {world.Now}");
                }

                while (next < journal.Count && journal[next].At == world.Now)
                {
                    world.Submit(journal[next++].Command);
                }

                world.Step();
            }

            if (next < journal.Count)
            {
                log.Warning($"{journal.Count - next} journal command(s) fall after the end time and were not applied");
            }
        }
#pragma warning disable CA1031 // Any simulation failure becomes a failure report and exit code 1.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            log.Error("simulation failed", ex);
            var report = FailureReport.Capture(world, "headless", ex.Message, ex, BuildInfo.Describe(typeof(HeadlessApp).Assembly), recent);
            string reportPath;
            try
            {
                reportPath = report.WriteToDirectory(reports);
            }
#pragma warning disable CA1031 // A failure while reporting must not hide the original failure.
            catch (Exception writeError)
#pragma warning restore CA1031
            {
                stderr.WriteLine($"could not write the failure report to {reports}: {writeError.Message}");
                return Failed;
            }

            stderr.WriteLine($"failure report: {reportPath}");
            stderr.WriteLine($"reproduce: sovereigns-headless replay {reportPath}");
            return Failed;
        }

        stepCost.Stop();
        if (options.Get("record") is { } recordPath)
        {
            using var writer = CreateText(recordPath);
            CommandJournal.WriteJsonLines(world.Journal.Entries, writer);
        }

        var summary = new RunSummary(fixtureReference, world.ScenarioId.ToString(), world.Seed, world.Now.Milliseconds,
            world.StepCount, world.Journal.Entries.Count, world.Markers.Count, world.ComputeChecksum(),
            stepCost.Elapsed.TotalMilliseconds, GC.GetTotalMemory(forceFullCollection: false),
            Environment.WorkingSet);
        stdout.WriteLine(JsonSerializer.Serialize(summary, SimJson.Options));
        return Ok;
    }

    private int ReplayReport(Options options)
    {
        var path = options.Positional ?? throw new UsageException("replay needs a failure report path");
        FailureReport report;
        try
        {
            using var stream = File.OpenRead(path);
            report = FailureReport.ReadFrom(stream);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            throw new UsageException($"cannot read failure report {path}: {ex.Message}");
        }

        var repo = RepoRoot(options);
        ScenarioDefinition scenario;
        try
        {
            scenario = LaunchSelection.Resolve(LoadContent(repo), Path.Combine(repo, "content"), null, repo,
                fixture: null, report.Scenario, report.Seed.ToString(CultureInfo.InvariantCulture), defaultSeed: 0).Scenario;
        }
        catch (LaunchException ex)
        {
            throw new UsageException(ex.Message);
        }

        World world;
        try
        {
            world = Replay.Run(scenario, report.Seed, report.Commands, new SimTime(report.SimTimeMs));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            stdout.WriteLine($"replay MISMATCH: {ex.Message}");
            return Mismatch;
        }

        stdout.WriteLine($"replayed seed {report.Seed}, {report.Scenario}, {report.Commands.Count} command(s) to {world.Now}");
        stdout.WriteLine($"reason: {report.Reason}");
        if (report.FailedInStep)
        {
            return ReproduceFailingStep(world, report);
        }

        var checksum = world.ComputeChecksum();
        if (report.StateChecksum is null)
        {
            stdout.WriteLine($"report has no checksum; replayed checksum {checksum}");
            return Ok;
        }

        var matches = checksum == report.StateChecksum;
        stdout.WriteLine(matches ? $"checksum matches: {checksum}" : $"checksum MISMATCH: report {report.StateChecksum}, replay {checksum}");
        return matches ? Ok : Mismatch;
    }

    /// <summary>Submits the failing step's inputs to the rebuilt world and steps once, expecting the reported failure.</summary>
    private int ReproduceFailingStep(World world, FailureReport report)
    {
        foreach (var command in report.PendingCommands)
        {
            world.Submit(command);
        }

        try
        {
            world.Step();
        }
#pragma warning disable CA1031 // Any exception here is the reproduced failure being reported.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            stdout.WriteLine($"failure reproduced in the step at {report.SimTimeMs} ms with {report.PendingCommands.Count} pending command(s): {ex.GetType().Name}: {ex.Message}");
            return Ok;
        }

        stdout.WriteLine($"failure NOT reproduced: the step at {report.SimTimeMs} ms completed with {report.PendingCommands.Count} pending command(s)");
        return Mismatch;
    }

    private ContentCatalog LoadContent(string repo)
    {
        var result = ContentCatalog.Load(Path.Combine(repo, "content"));
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                stderr.WriteLine(error);
            }

            throw new UsageException("content is invalid; fix the errors above (sovereigns-headless validate-content)");
        }

        return result.Catalog;
    }

    private static IReadOnlyList<JournalEntry> ReadJournal(string path)
    {
        try
        {
            using var reader = File.OpenText(path);
            return CommandJournal.ReadJsonLines(reader, path);
        }
        catch (Exception ex) when (ex is IOException or FormatException or UnauthorizedAccessException)
        {
            throw new UsageException($"cannot read command journal: {ex.Message}");
        }
    }

    private static StreamWriter CreateText(string path)
    {
        if (Path.GetDirectoryName(Path.GetFullPath(path)) is { } directory)
        {
            Directory.CreateDirectory(directory);
        }

        return new StreamWriter(path) { NewLine = "\n" };
    }

    private static string RepoRoot(Options options)
    {
        if (options.Get("repo") is { } repo)
        {
            return Path.GetFullPath(repo);
        }

        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "global.json")) && Directory.Exists(Path.Combine(directory.FullName, "content")))
                {
                    return directory.FullName;
                }
            }
        }

        throw new UsageException("cannot find the repository root (global.json and content/); pass --repo <dir>");
    }

    private static long ParseInt64(string text, string option) =>
        long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new UsageException($"{option} must be a whole number, found '{text}'");

    private sealed record RunSummary(
        string? Fixture,
        string Scenario,
        ulong Seed,
        long SimTimeMs,
        long Steps,
        int Commands,
        int Markers,
        string Checksum,
        double WallMs,
        long ManagedHeapBytes,
        long WorkingSetBytes);

    private sealed class Options
    {
        private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

        public string? Positional { get; private set; }

        public IEnumerable<string> Names => _values.Keys;

        public string? Get(string name) => _values.GetValueOrDefault(name);

        public static Options Parse(IEnumerable<string> args)
        {
            var options = new Options();
            using var arg = args.GetEnumerator();
            while (arg.MoveNext())
            {
                if (!arg.Current.StartsWith("--", StringComparison.Ordinal))
                {
                    options.Positional = options.Positional is null ? arg.Current : throw new UsageException($"unexpected argument '{arg.Current}'");
                    continue;
                }

                var name = arg.Current[2..];
                if (!arg.MoveNext())
                {
                    throw new UsageException($"--{name} needs a value");
                }

                if (!options._values.TryAdd(name, arg.Current))
                {
                    throw new UsageException($"--{name} given twice");
                }
            }

            return options;
        }
    }
}

internal sealed class UsageException(string message) : Exception(message);

/// <summary>Deliberate failure requested with --fail-at-ms, used to exercise failure reporting.</summary>
internal sealed class InjectedFailureException(string message) : Exception(message);
