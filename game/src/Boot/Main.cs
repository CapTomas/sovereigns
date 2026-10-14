using System.Globalization;
using System.Reflection;
using Sovereigns.Client.Controls;
using Sovereigns.Client.Diagnostics;
using Sovereigns.Client.Simulation;
using Sovereigns.Client.Ui;
using Sovereigns.Simulation;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Scenarios;
using InputEvent = Godot.InputEvent;

namespace Sovereigns.Client.Boot;

/// <summary>
/// Root of the client. It parses launch options, sets up logging, creates the one authoritative world, runs it at
/// real-time pace through <see cref="SimulationHost"/>, hosts the <see cref="InspectionShell"/>, and closes cleanly:
/// log the shutdown checksum, write the recorded command journal, flush logs and quit with exit code 0.
/// </summary>
public partial class Main : Node
{
    private ClientLogging? _logging;
    private SubsystemLog _log;
    private LaunchArguments _arguments = new();
    private ClientPaths? _paths;
    private UnhandledExceptionEventHandler? _unhandledHandler;
    private int _framesSinceReady;
    private bool _captured;
    private bool _shutDown;

    public SimulationHost? Host { get; private set; }

    public InspectionShell? Shell { get; private set; }

    public InputBindings Bindings { get; private set; } = InputBindings.Defaults();

    public InputHistory Inputs { get; } = new(256);

    public LaunchResult? Launch { get; private set; }

    /// <summary>True once the client has logged its shutdown and written the recorded journal.</summary>
    public bool IsShutDown => _shutDown;

    public override void _Ready()
    {
        GetTree().AutoAcceptQuit = false;
        GetWindow().Title = "Sovereigns - inspection shell (development)";
        try
        {
            _arguments = LaunchArguments.FromEnvironment();
        }
        catch (LaunchException ex)
        {
            Start(new LaunchArguments(), [ex.Message]);
            return;
        }

        Start(_arguments, []);
    }

    public override void _Process(double delta)
    {
        if (_shutDown)
        {
            // The log writer is closed; the world must not advance or log after shutdown.
            return;
        }

        Host?.Advance(delta);
        if (++_framesSinceReady == _arguments.CaptureFrame && _arguments.CapturePath is { } path && !_captured)
        {
            _captured = true;
            SaveCapture(path);
        }
    }

    public override void _Input(InputEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        if (@event.IsActionPressed(InputActions.Quit, exactMatch: true))
        {
            GetViewport().SetInputAsHandled();
            Quit();
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            Quit();
        }
    }

    public override void _ExitTree() => Shutdown();

    /// <summary>Shuts the client down and ends the process with exit code 0.</summary>
    public void Quit()
    {
        Shutdown();
        GetTree().Quit();
    }

    private void Start(LaunchArguments arguments, IReadOnlyList<string> argumentProblems)
    {
        _paths = ClientPaths.Resolve(arguments);
        _logging = ClientLogging.Create(arguments.LogPath ?? Path.Combine(_paths.LogsDirectory, ClientLogging.DefaultFileName()));
        _log = _logging.Logger.For("client");
        _log.Info($"client starting; logs in {_paths.LogsDirectory}, failure reports in {_paths.ReportsDirectory}",
            data: new Dictionary<string, string> { ["log_file"] = _logging.FilePath ?? "none", ["bindings_file"] = _paths.BindingsFile });

        Bindings = InputBindings.Load(_paths.BindingsFile, message => _logging.Logger.For("client.input").Warning(message));
        foreach (var conflict in Bindings.FindConflicts())
        {
            _logging.Logger.For("client.input").Warning($"binding {conflict.Binding} is used by {string.Join(", ", conflict.Actions)}");
        }

        InputMapRegistrar.Apply(Bindings);
        _unhandledHandler = OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += _unhandledHandler;

        Launch = argumentProblems.Count > 0
            ? new LaunchResult(null, null, argumentProblems)
            : WorldLauncher.Launch(arguments, _paths, _logging.Logger);
        if (!Launch.Succeeded)
        {
            ShowLaunchProblems(Launch.Problems);
            return;
        }

        var world = Launch.World!;
        var build = BuildInfo.Describe(Assembly.GetExecutingAssembly());
        var worldLog = _logging.Logger.For("client", world);
        var reporter = new FailureReporter(_paths.ReportsDirectory, _paths.RepositoryRoot, worldLog, build, _logging.Recent, Inputs);
        Host = new SimulationHost(world, _logging.Logger, reporter, arguments.FailAtMs);
        worldLog.Info($"running {world.ScenarioId} with seed {world.Seed}{(Launch.Selection!.FixtureReference is { } fixture ? $" (fixture {fixture})" : "")}");

        Shell = new InspectionShell(new ShellContext(Host, Launch.Selection, arguments, Bindings, Inputs, _logging.Recent, worldLog, build));
        AddChild(Shell);
    }

    private void ShowLaunchProblems(IReadOnlyList<string> problems)
    {
        foreach (var problem in problems)
        {
            _log.Error(problem);
        }

        var banner = new ErrorBanner();
        AddChild(banner);
        banner.ShowMessage(["The client could not start a world:", .. problems, "", $"Close the window or press {string.Join(" / ", Bindings.Get(InputActions.Quit).Select(b => b.DisplayName))} to quit. {LaunchArguments.Usage}"]);
    }

    private void SaveCapture(string path)
    {
        if (DisplayServer.GetName() == "headless")
        {
            _log.Warning($"--capture {path}: a headless run renders nothing; run with a window to capture the viewport");
            return;
        }

        var image = GetViewport().GetTexture().GetImage();
        if (image is null || image.IsEmpty())
        {
            _log.Warning($"--capture {path}: the viewport has no image yet");
            return;
        }

        if (Path.GetDirectoryName(Path.GetFullPath(path)) is { } directory)
        {
            Directory.CreateDirectory(directory);
        }

        var error = image.SavePng(path);
        if (error == Error.Ok)
        {
            _log.Info($"viewport captured to {path} ({image.GetWidth()}x{image.GetHeight()})");
        }
        else
        {
            _log.Warning($"--capture {path}: saving the PNG failed with {error}");
        }
    }

    private void Shutdown()
    {
        if (_shutDown)
        {
            return;
        }

        _shutDown = true;
        if (_unhandledHandler is not null)
        {
            AppDomain.CurrentDomain.UnhandledException -= _unhandledHandler;
        }

        try
        {
            if (Host is { } host && _logging is { } logging)
            {
                var world = host.World;
                logging.Logger.For("client", world).Info("shutdown", data: new Dictionary<string, string>
                {
                    ["sim_time_ms"] = world.Now.Milliseconds.ToString(CultureInfo.InvariantCulture),
                    ["steps"] = world.StepCount.ToString(CultureInfo.InvariantCulture),
                    ["journal_entries"] = world.Journal.Entries.Count.ToString(CultureInfo.InvariantCulture),
                    ["checksum"] = world.ComputeChecksum(),
                });
                WriteRecordedJournal(world);
            }
            else
            {
                _logging?.Logger.For("client").Info("shutdown");
            }
        }
        finally
        {
            _logging?.Dispose();
        }
    }

    private void WriteRecordedJournal(World world)
    {
        if (_arguments.RecordPath is not { } path)
        {
            return;
        }

        try
        {
            if (Path.GetDirectoryName(Path.GetFullPath(path)) is { } directory)
            {
                Directory.CreateDirectory(directory);
            }

            using var writer = new StreamWriter(path) { NewLine = "\n" };
            CommandJournal.WriteJsonLines(world.Journal.Entries, writer);
            _log.Info($"command journal written to {path}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _log.Error($"cannot write the command journal to {path}", ex);
        }
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        var exception = args.ExceptionObject as Exception;
        _logging?.Logger.Write(new LogRecord(LogSeverity.Fatal, "client", $"unhandled exception: {exception?.Message ?? args.ExceptionObject}",
            Host?.World.Seed, Host?.World.Now.Milliseconds, Exception: exception?.ToString()));
        Host?.WriteReport($"unhandled exception: {exception?.Message ?? "unknown"}", exception);
    }
}
