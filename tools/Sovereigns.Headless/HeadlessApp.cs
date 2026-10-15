using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Sovereigns.Simulation;
using Sovereigns.Simulation.Atlas;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Content;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Identity;
using Sovereigns.Simulation.Persistence;
using Sovereigns.Simulation.Random;
using Sovereigns.Simulation.Scenarios;
using Sovereigns.Simulation.Serialization;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Headless;

/// <summary>Command-line host for the simulation without the game client (SOV-P01-T04, T08, T09, T13; SOV-P02-T12).</summary>
internal sealed class HeadlessApp(TextWriter stdout, TextWriter stderr)
{
    public const int Ok = 0;
    public const int Failed = 1;
    public const int UsageError = 2;
    public const int Mismatch = 3;

    private const string Usage = """
        Usage: sovereigns-headless <command> [options]

          run (--fixture <F-ID|path> | --scenario <content-id> --seed <n> | --load <save.json>)
              [--for-ms <ms> | --until-ms <ms>] [--commands <journal.jsonl>] [--record <journal.jsonl>]
              [--save <save.json>] [--log <file.jsonl>] [--reports <dir>] [--fail-after-ms <ms>] [--repo <dir>]
              Runs the world headless and prints a JSON summary with the state checksum. --for-ms is relative to
              the start (default: the scenario duration; 0 after --load); --until-ms is absolute world time.
              A fixture's own command journal applies unless --commands is given. --save writes a save at the end.
              --fail-after-ms injects a failure that many milliseconds after the start, to exercise failure reports.
          replay <failure-report.json> [--save <save.json>] [--repo <dir>]
              Rebuilds the reported world from its scenario, or from the save the report names, applies its commands
              and compares checksums, or reproduces the failing command or step.
          validate-content [--content <dir>] [--repo <dir>]
              Validates content definitions and prints actionable errors.
          bench [--fixture <F-ID> | --scenario <content-id> --seed <n>] [--samples <n>] [--repo <dir>]
              Measures atlas queries, chunk churn, aggregation and memory on a painted world (default F-EMPTY) and prints
              JSON. Measurements only: budgets come from agreed reference hardware (QUALITY_BAR).

        Exit codes: 0 success, 1 simulation failure (report written), 2 usage or content error, 3 replay mismatch.
        """;

    private static readonly string[] WorldSources = ["fixture", "scenario", "load"];

    private static readonly Dictionary<string, string[]> AllowedOptions = new(StringComparer.Ordinal)
    {
        ["run"] = ["fixture", "scenario", "seed", "load", "for-ms", "until-ms", "commands", "record", "save", "log", "reports", "fail-after-ms", "repo"],
        ["replay"] = ["save", "repo"],
        ["validate-content"] = ["content", "repo"],
        ["bench"] = ["fixture", "scenario", "seed", "samples", "repo"],
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
                "bench" => Bench(options),
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
        var sources = WorldSources.Count(name => options.Get(name) is not null);
        if (sources != 1)
        {
            throw new UsageException("give exactly one of --fixture <F-ID>, --scenario <content-id> with --seed <n>, or --load <save.json>");
        }

        if (options.Get("scenario") is not null && options.Get("seed") is null)
        {
            throw new UsageException("--scenario needs --seed");
        }

        if (options.Get("for-ms") is not null && options.Get("until-ms") is not null)
        {
            throw new UsageException("give --for-ms or --until-ms, not both");
        }

        using var logFile = options.Get("log") is { } logPath ? CreateText(logPath) : null;
        var recent = new RecentLogBuffer(200);
        var sinks = new List<ILogSink> { new TextLogSink(stderr), recent };
        if (logFile is not null)
        {
            sinks.Add(new JsonLinesLogSink(logFile));
        }

        var logger = new Logger(sinks, LogSeverity.Info, TimeProvider.System);
        var log = logger.For("headless");
        World world;
        string? fixtureReference = null;
        string? commandsPath = options.Get("commands");
        long defaultDuration;
        if (options.Get("load") is { } savePath)
        {
            world = LoadSave(savePath, logger);
            defaultDuration = 0;
        }
        else
        {
            LaunchSelection selection;
            try
            {
                selection = LaunchSelection.Resolve(LoadContent(repo), Path.Combine(repo, "content"), Path.Combine(repo, "tests", "fixtures"), repo,
                    options.Get("fixture"), options.Get("scenario"), options.Get("seed"), defaultSeed: 0);
            }
            catch (LaunchException ex)
            {
                throw new UsageException(ex.Message);
            }

            fixtureReference = selection.FixtureReference;
            commandsPath ??= selection.CommandsPath;
            defaultDuration = selection.Scenario.DurationMs;
            world = World.Create(selection.Scenario, selection.Seed, logger, NewWorldId());
        }

        var start = world.Now;
        var until = options.Get("until-ms") is { } untilText
            ? new SimTime(ParseInt64(untilText, "--until-ms"))
            : start.Plus(options.Get("for-ms") is { } forText ? ParseInt64(forText, "--for-ms") : defaultDuration);
        if (until < start || (until.Milliseconds - start.Milliseconds) % world.StepMs != 0)
        {
            throw new UsageException($"the end time {until} must be at or after {start} by a whole number of {world.StepMs} ms steps");
        }

        SimTime? failAt = options.Get("fail-after-ms") is { } failText ? start.Plus(ParseInt64(failText, "--fail-after-ms")) : null;
        if (failAt is { } failure && (failure < start || (failure.Milliseconds - start.Milliseconds) % world.StepMs != 0))
        {
            throw new UsageException($"--fail-after-ms must be a non-negative whole number of {world.StepMs} ms steps");
        }
        var journal = commandsPath is null ? [] : ReadJournal(commandsPath);
        if (journal.FirstOrDefault(entry => entry.At < start || (entry.At.Milliseconds - start.Milliseconds) % world.StepMs != 0) is { } misaligned)
        {
            throw new UsageException($"journal entry {misaligned.Sequence} at {misaligned.At} is before {start} or not on a {world.StepMs} ms step boundary of {world.ScenarioId}");
        }

        var stepCost = Stopwatch.StartNew();
        var stoppedAtDecision = false;
        try
        {
            SimTime AdvanceTo(SimTime target)
            {
                if (failAt is { } failure && failure <= target)
                {
                    if (world.AdvanceTo(failure) == failure)
                    {
                        throw new InjectedFailureException($"failure injected by --fail-after-ms at {failure}");
                    }

                    return world.Now;
                }

                return world.AdvanceTo(target);
            }

            var applied = 0;
            foreach (var entry in journal.TakeWhile(entry => entry.At <= until))
            {
                if (AdvanceTo(entry.At) != entry.At)
                {
                    break;
                }

                if (entry.Command is ControlCommand control)
                {
                    world.Control(control);
                }
                else
                {
                    world.Submit(entry.Command);
                }

                applied++;
            }

            stoppedAtDecision = AdvanceTo(until) != until;
            if (applied < journal.Count)
            {
                log.Warning($"{journal.Count - applied} journal command(s) were not applied: they fall after the end, or a decision stopped time first");
            }
        }
#pragma warning disable CA1031 // Any simulation failure becomes a failure report and exit code 1.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            log.Error("simulation failed", ex);
            return WriteFailureReport(world, ex, recent, options.Get("reports") ?? Path.Combine(repo, "artifacts", "reports"));
        }

        stepCost.Stop();
        if (stoppedAtDecision)
        {
            log.Warning($"stopped at {world.Now} by pending decision(s): {string.Join(", ", world.PendingDecisions.Select(decision => $"{decision.Event} {decision.Label}"))}");
        }

        if (options.Get("record") is { } recordPath)
        {
            using var writer = CreateText(recordPath);
            CommandJournal.WriteJsonLines(world.Journal.Entries, writer);
        }

        if (options.Get("save") is { } savePathOut)
        {
            using var stream = CreateFile(savePathOut);
            SaveFile.Capture(world).WriteTo(stream);
        }

        var summary = new RunSummary(fixtureReference, world.ScenarioId.ToString(), world.Identity.Id.ToString(), world.BaseSaveSha256,
            AtlasProbe(world),
            world.Seed, WorldCalendar.ToCalendar(world.Identity.Start).ToString(), WorldCalendar.ToCalendar(world.Now).ToString(),
            world.Now.Milliseconds, world.StepCount, world.Journal.Entries.Count, world.Markers.Count, world.Ledger.Count,
            world.PendingDecisions.Count, stoppedAtDecision, world.ComputeChecksum(), stepCost.Elapsed.TotalMilliseconds,
            GC.GetTotalMemory(forceFullCollection: false), Environment.WorkingSet);
        stdout.WriteLine(JsonSerializer.Serialize(summary, SimJson.Options));
        return Ok;
    }

    /// <summary>SOV-P03-T16: atlas measurements on this machine. No thresholds; compare runs on the same hardware.</summary>
    private int Bench(Options options)
    {
        var repo = RepoRoot(options);
        LaunchSelection selection;
        try
        {
            selection = LaunchSelection.Resolve(LoadContent(repo), Path.Combine(repo, "content"), Path.Combine(repo, "tests", "fixtures"), repo,
                options.Get("scenario") is null ? options.Get("fixture") ?? "F-EMPTY" : null, options.Get("scenario"), options.Get("seed"), defaultSeed: 0);
        }
        catch (LaunchException ex)
        {
            throw new UsageException(ex.Message);
        }

        var samples = options.Get("samples") is { } text ? (int)ParseInt64(text, "--samples") : 100_000;
        if (samples < 100)
        {
            throw new UsageException("--samples must be at least 100");
        }

        var world = World.Create(selection.Scenario, selection.Seed, Logger.None, NewWorldId());
        var extent = world.Extent;
        var strokes = RandomStream.For(selection.Seed, "bench.strokes");
        for (var k = 0; k < 40; k++)
        {
            world.Submit(new PaintScratch(new WorldPoint(strokes.NextDouble() * extent.East, strokes.NextDouble() * extent.North),
                50 + strokes.NextDouble() * 1_500, strokes.NextDouble()));
        }

        var heapBefore = GC.GetTotalMemory(forceFullCollection: true);
        var cases = new List<BenchCase>();
        void Measure(string name, string field, GridLevel? level, int count, long? budget = null)
        {
            world.Atlas.Unload();
            world.Atlas.BudgetBytes = budget ?? FieldAtlas.DefaultBudgetBytes;
            var points = RandomStream.For(selection.Seed, "bench.points");
            var statsBefore = world.Atlas.CacheStatistics;
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: false);
            var clock = Stopwatch.StartNew();
            for (var k = 0; k < count; k++)
            {
                world.Atlas.Sample(field, new WorldPoint(points.NextDouble() * extent.East, points.NextDouble() * extent.North), level);
            }

            clock.Stop();
            var stats = world.Atlas.CacheStatistics;
            cases.Add(new BenchCase(name, count, clock.Elapsed.TotalMilliseconds, count / clock.Elapsed.TotalSeconds,
                stats.Loads - statsBefore.Loads, stats.Evictions - statsBefore.Evictions, stats.ResidentBytes,
                GC.GetTotalAllocatedBytes(precise: false) - allocatedBefore));
        }

        // Warm-up: compile every path once, then discard those cases.
        foreach (var level in Enum.GetValues<GridLevel>())
        {
            Measure("warm-up", FieldCatalog.Scratch.Key, level, 2_000);
            Measure("warm-up", FieldCatalog.ScratchGradient.Key, level, 2_000);
        }

        Measure("warm-up", FieldCatalog.Latitude.Key, null, 2_000);
        cases.Clear();
        Measure("latitude analytic", FieldCatalog.Latitude.Key, null, samples);
        Measure("scratch local native", FieldCatalog.Scratch.Key, GridLevel.Local, samples);
        Measure("scratch regional aggregate", FieldCatalog.Scratch.Key, GridLevel.Regional, samples);
        Measure("scratch macro aggregate", FieldCatalog.Scratch.Key, GridLevel.Macro, samples);
        Measure("scratch tactical refined", FieldCatalog.Scratch.Key, GridLevel.Tactical, samples);
        Measure("gradient local derived", FieldCatalog.ScratchGradient.Key, GridLevel.Local, samples);
        Measure("scratch local, 1 MiB cache (churn)", FieldCatalog.Scratch.Key, GridLevel.Local, samples, budget: 1024 * 1024);

        // The whole-world aggregate visits every local chunk; a poller records the managed heap's peak meanwhile, which
        // shows whether memory stays near the cache budget rather than growing with the world.
        world.Atlas.Unload();
        world.Atlas.BudgetBytes = FieldAtlas.DefaultBudgetBytes;
        var heapBeforeAggregate = GC.GetTotalMemory(forceFullCollection: true);
        var peak = heapBeforeAggregate;
        var aggregating = true;
        var poller = new Thread(() =>
        {
            while (Volatile.Read(ref aggregating))
            {
                Interlocked.Exchange(ref peak, Math.Max(Interlocked.Read(ref peak), GC.GetTotalMemory(forceFullCollection: false)));
                Thread.Sleep(1);
            }
        });
        poller.Start();
        var aggregateClock = Stopwatch.StartNew();
        var whole = world.Atlas.Aggregate(FieldCatalog.Scratch.Key, 0, 0, extent.East, extent.North);
        aggregateClock.Stop();
        Volatile.Write(ref aggregating, false);
        poller.Join();
        var heapAfter = GC.GetTotalMemory(forceFullCollection: true);
        var summary = new BenchSummary(selection.FixtureReference, world.ScenarioId.ToString(), world.Seed, extent.East, extent.North,
            Environment.ProcessorCount, System.Runtime.InteropServices.RuntimeInformation.OSDescription, cases,
            new BenchAggregate(whole.Cells, aggregateClock.Elapsed.TotalMilliseconds, whole.Mean, peak - heapBeforeAggregate),
            heapBefore, heapAfter, world.Atlas.CacheStatistics.ResidentBytes, world.Atlas.OverrideCount(FieldCatalog.Scratch.Key));
        stdout.WriteLine(JsonSerializer.Serialize(summary, SimJson.Options));
        return Ok;
    }

    private sealed record BenchCase(string Name, int Samples, double ElapsedMs, double SamplesPerSecond, long ChunkLoads, long ChunkEvictions,
        long ResidentBytes, long AllocatedBytes);

    /// <param name="PeakHeapGrowthBytes">Largest managed-heap growth observed while aggregating, sampled every millisecond.</param>
    private sealed record BenchAggregate(long Cells, double ElapsedMs, double Mean, long PeakHeapGrowthBytes);

    private sealed record BenchSummary(string? Fixture, string Scenario, ulong Seed, double ExtentEastM, double ExtentNorthM, int Processors,
        string Os, IReadOnlyList<BenchCase> Cases, BenchAggregate WholeWorldAggregate, long ManagedHeapBeforeBytes, long ManagedHeapAfterBytes,
        long CacheResidentBytes, int StoredOverrides);

    private int WriteFailureReport(World world, Exception ex, RecentLogBuffer recent, string reports)
    {
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
        stderr.WriteLine($"reproduce: sovereigns-headless replay {reportPath}{(world.BaseSaveSha256 is null ? "" : " --save <the loaded save>")}");
        return Failed;
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

        World world;
        if (report.BaseSaveSha256 is { } baseSave)
        {
            var savePath = options.Get("save") ?? throw new UsageException($"the reported session started from save {baseSave}; pass it with --save <save.json>");
            world = LoadSave(savePath, Logger.None);
            if (world.BaseSaveSha256 != baseSave)
            {
                throw new UsageException($"{savePath} has payload {world.BaseSaveSha256}, not the reported {baseSave}");
            }
        }
        else
        {
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

            // An unreadable ID only changes the world's identity, which no outcome or checksum depends on.
            world = World.Create(scenario, report.Seed, Logger.None, WorldId.TryParse(report.WorldId, out var id) ? id : default);
        }

        try
        {
            Replay.Continue(world, report.Commands, new SimTime(report.SimTimeMs));
        }
#pragma warning disable CA1031 // Any failure while rebuilding a reported world means the report no longer reproduces.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            stdout.WriteLine($"replay MISMATCH: {ex.GetType().Name}: {ex.Message}");
            return Mismatch;
        }

        stdout.WriteLine($"replayed seed {report.Seed}, {report.Scenario}, {report.Commands.Count} command(s) to {world.Now}");
        stdout.WriteLine($"reason: {report.Reason}");
        if (report.FailedDuring != FaultStage.None)
        {
            return ReproduceFault(world, report);
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

    /// <summary>Repeats the failing command or step on the rebuilt world, expecting the reported exception type.</summary>
    private int ReproduceFault(World world, FailureReport report)
    {
        try
        {
            if (report.FailedDuring == FaultStage.Command)
            {
                world.Submit(report.FailingCommand ?? throw new UsageException("the report names a failing command but does not contain it"));
            }
            else
            {
                world.Step();
            }
        }
        catch (UsageException)
        {
            throw;
        }
#pragma warning disable CA1031 // Any exception here is the reproduced failure being reported.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            // Exception.ToString() starts with the full type name, so a different failure is not mistaken for the reported one.
            if (report.Exception is { } recorded && !recorded.StartsWith(ex.GetType().FullName + ":", StringComparison.Ordinal))
            {
                stdout.WriteLine($"failure MISMATCH during the {report.FailedDuring} at {report.SimTimeMs} ms: reported {recorded.Split('\n')[0]}, replay threw {ex.GetType().FullName}: {ex.Message}");
                return Mismatch;
            }

            stdout.WriteLine($"failure reproduced during the {report.FailedDuring} at {report.SimTimeMs} ms: {ex.GetType().Name}: {ex.Message}");
            return Ok;
        }

        stdout.WriteLine($"failure NOT reproduced: the {report.FailedDuring} at {report.SimTimeMs} ms completed");
        return Mismatch;
    }

    private static World LoadSave(string path, Logger logger)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return SaveFile.Load(stream, logger);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new UsageException($"cannot read save {path}: {ex.Message}");
        }
        catch (SaveLoadException ex)
        {
            throw new UsageException($"{path}: {ex.Message}");
        }
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

    /// <summary>A fresh durable world ID from operating-system randomness; hosts may read it, the simulation may not.</summary>
    private static WorldId NewWorldId() => WorldId.FromBytes(RandomNumberGenerator.GetBytes(16));

    private static FileStream CreateFile(string path)
    {
        if (Path.GetDirectoryName(Path.GetFullPath(path)) is { } directory)
        {
            Directory.CreateDirectory(directory);
        }

        return File.Create(path);
    }

    private static StreamWriter CreateText(string path) => new(CreateFile(path)) { NewLine = "\n" };

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

    /// <summary>
    /// SHA-256 over atlas samples on a fixed 16 × 16 lattice at every level: latitude, scratch and its gradient. These are
    /// derived values computed with basic arithmetic only, so they must match across platforms (ADR-0004 §3, ADR-0009).
    /// Local solar time is excluded because it uses a transcendental function.
    /// </summary>
    private static string AtlasProbe(World world)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[8];
        foreach (var level in Enum.GetValues<GridLevel>())
        {
            for (var row = 0; row < 16; row++)
            {
                for (var column = 0; column < 16; column++)
                {
                    var point = new WorldPoint(world.Extent.East * (column + 0.37) / 16, world.Extent.North * (row + 0.61) / 16);
                    foreach (var field in new[] { FieldCatalog.Latitude.Key, FieldCatalog.Scratch.Key, FieldCatalog.ScratchGradient.Key })
                    {
                        foreach (var value in world.Atlas.Sample(field, point, level).Values ?? [])
                        {
                            System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(buffer, BitConverter.DoubleToInt64Bits(value));
                            hash.AppendData(buffer);
                        }
                    }
                }
            }
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private sealed record RunSummary(
        string? Fixture,
        string Scenario,
        string WorldId,
        string? BaseSave,
        string AtlasProbe,
        ulong Seed,
        string Start,
        string Now,
        long SimTimeMs,
        long Steps,
        int Commands,
        int Markers,
        int Events,
        int PendingDecisions,
        bool StoppedAtDecision,
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

/// <summary>Deliberate failure requested with --fail-after-ms, used to exercise failure reporting.</summary>
internal sealed class InjectedFailureException(string message) : Exception(message);
