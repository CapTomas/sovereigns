using System.Diagnostics;
using Sovereigns.Client.Diagnostics;
using Sovereigns.Simulation;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Diagnostics;

namespace Sovereigns.Client.Simulation;

/// <summary>Why the host stopped advancing and where the evidence is.</summary>
public sealed record HostFailure(string Reason, string? ReportPath, string ReplayCommand);

/// <summary>Deliberate failure requested with <c>--fail-at-ms</c>, used to exercise failure reporting.</summary>
public sealed class InjectedFailureException(string message) : Exception(message);

/// <summary>
/// Owns the one authoritative <see cref="World"/> in the client and feeds it fixed steps at real-time pace
/// (one simulated second per real second). The rendering clock only decides how many whole steps are due;
/// the world never sees frame deltas, so any frame pacing reaches the same state at the same simulation time.
/// This class has no Godot types so it can be exercised with scripted frame deltas.
/// </summary>
public sealed class SimulationHost
{
    public const int MaxStepsPerFrame = 10;

    private const long MicrosecondsPerSecond = 1_000_000;

    private readonly SubsystemLog _log;
    private readonly FailureReporter _reporter;
    private readonly long? _failAtMs;
    private readonly long _stepMicroseconds;
    private long _owedMicroseconds;
    private bool _backlogWarned;

    public SimulationHost(World world, Logger logger, FailureReporter reporter, long? failAtMs = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(reporter);
        World = world;
        _log = logger.For("client.sim", world);
        _reporter = reporter;
        _failAtMs = failAtMs;
        _stepMicroseconds = world.StepMs * 1000;
    }

    public World World { get; }

    /// <summary>Set when stepping or command submission threw; the host then stops advancing.</summary>
    public HostFailure? Failure { get; private set; }

    /// <summary>Wall-clock cost of the most recent <see cref="World.Step"/>, in milliseconds.</summary>
    public double LastStepCostMs { get; private set; }

    /// <summary>Accepts real elapsed seconds and runs every whole simulation step that is now due, up to <see cref="MaxStepsPerFrame"/>.</summary>
    public void Advance(double realDeltaSeconds)
    {
        if (Failure is not null || !double.IsFinite(realDeltaSeconds) || realDeltaSeconds <= 0)
        {
            return;
        }

        // Whole microseconds keep the pacing exact; summing binary fractions of a second would drift.
        _owedMicroseconds += (long)Math.Round(realDeltaSeconds * MicrosecondsPerSecond);
        var steps = 0;
        try
        {
            while (_owedMicroseconds >= _stepMicroseconds && steps < MaxStepsPerFrame)
            {
                StepOnce();
                _owedMicroseconds -= _stepMicroseconds;
                steps++;
            }
        }
#pragma warning disable CA1031 // Any stepping failure becomes a failure report and an on-screen banner.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Fail("simulation step failed", ex);
            return;
        }

        if (_owedMicroseconds >= _stepMicroseconds)
        {
            var droppedSeconds = _owedMicroseconds / _stepMicroseconds * World.StepMs / 1000.0;
            _owedMicroseconds %= _stepMicroseconds;
            if (!_backlogWarned)
            {
                _backlogWarned = true;
                _log.Warning($"frame hitch: dropped {droppedSeconds:0.###} s of simulation backlog (limit {MaxStepsPerFrame} steps per frame); further drops are not logged");
            }
        }
    }

    /// <summary>Queues a command with the world; false when the host has already failed or submission threw.</summary>
    public bool TrySubmit(SimCommand command)
    {
        if (Failure is not null)
        {
            return false;
        }

        try
        {
            World.Submit(command);
            return true;
        }
#pragma warning disable CA1031 // Any submission failure becomes a failure report and an on-screen banner.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Fail("command submission failed", ex);
            return false;
        }
    }

    /// <summary>Writes a report of the current state without stopping the host; null when it could not be written.</summary>
    public string? WriteReport(string reason, Exception? exception = null) => _reporter.TryWrite(World, reason, exception);

    private void StepOnce()
    {
        if (_failAtMs is { } failAt && World.Now.Milliseconds >= failAt)
        {
            throw new InjectedFailureException($"failure injected by --fail-at-ms at {World.Now}");
        }

        var started = Stopwatch.GetTimestamp();
        World.Step();
        LastStepCostMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
    }

    private void Fail(string reason, Exception exception)
    {
        _log.Error(reason, exception);
        var path = _reporter.TryWrite(World, $"{reason}: {exception.Message}", exception);
        Failure = new HostFailure($"{reason}: {exception.Message}", path, _reporter.ReplayCommand(path ?? _reporter.ReportsDirectory));
    }
}
