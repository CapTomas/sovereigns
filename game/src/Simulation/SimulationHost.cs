using System.Diagnostics;
using Sovereigns.Client.Diagnostics;
using Sovereigns.Simulation;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Kernel;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Client.Simulation;

/// <summary>Why the host stopped advancing and where the evidence is.</summary>
public sealed record HostFailure(string Reason, string? ReportPath, string ReplayCommand);

/// <summary>Deliberate failure requested with <c>--fail-after-ms</c>, used to exercise failure reporting.</summary>
public sealed class InjectedFailureException(string message) : Exception(message);

/// <summary>
/// Owns the one authoritative <see cref="World"/> in the client and paces it with the kernel's
/// <see cref="RealTimePacer"/> at the world's <see cref="TimeControl"/> speed (SOV-P02-T11). The rendering clock
/// only decides how far the world advances per frame; the world never sees frame deltas, so any frame pacing,
/// pause pattern or speed reaches the same state at the same simulation time (SOV-P02-T16). A fixture's command
/// stream is submitted when the world's time reaches each entry, as the headless runner does. This class has no
/// Godot types so it can be exercised with scripted frame deltas.
/// </summary>
public sealed class SimulationHost
{
    private readonly RealTimePacer _pacer = new();
    private readonly SubsystemLog _log;
    private readonly FailureReporter _reporter;
    private readonly SimTime? _failAt;
    private readonly IReadOnlyList<JournalEntry> _script;
    private int _nextScripted;
    private bool _backlogWarned;

    /// <param name="failAfterMs">Milliseconds after the world's current time at which to inject a failure, rounded up to the step grid.</param>
    /// <param name="script">Commands to submit when the world's time reaches each entry's time, in time order.</param>
    public SimulationHost(World world, Logger logger, FailureReporter reporter, long? failAfterMs = null, IReadOnlyList<JournalEntry>? script = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(reporter);
        World = world;
        _log = logger.For("client.sim", world);
        _reporter = reporter;
        _failAt = failAfterMs is { } after ? world.Now.Plus((Math.Max(0, after) + world.StepMs - 1) / world.StepMs * world.StepMs) : null;
        _script = script ?? [];
    }

    public World World { get; }

    /// <summary>Set when advancing or command submission threw; the host then stops advancing.</summary>
    public HostFailure? Failure { get; private set; }

    /// <summary>Wall-clock cost, in milliseconds, of the most recent <see cref="Advance"/> that moved the clock.</summary>
    public double LastAdvanceCostMs { get; private set; }

    /// <summary>Steps taken by the most recent <see cref="Advance"/> that moved the clock.</summary>
    public long LastAdvanceSteps { get; private set; }

    /// <summary>Fixture commands not yet submitted because the world's time has not reached them.</summary>
    public int ScriptedCommandsRemaining => _script.Count - _nextScripted;

    /// <summary>
    /// Accepts real elapsed seconds and advances the world as its time control allows. The world stops at the next
    /// scripted command, at a pending decision, and never passes <paramref name="limit"/> (for example a fixed end time).
    /// </summary>
    public void Advance(double realDeltaSeconds, SimTime? limit = null)
    {
        if (Failure is not null)
        {
            return;
        }

        var started = Stopwatch.GetTimestamp();
        var before = World.Now;
        try
        {
            SubmitDueScript();
            _pacer.Advance(World, realDeltaSeconds, Earliest(limit, _failAt, _nextScripted < _script.Count ? _script[_nextScripted].At : null));
            SubmitDueScript();
            if (_failAt is { } failAt && World.Now >= failAt)
            {
                throw new InjectedFailureException($"failure injected by --fail-after-ms at {World.Now}");
            }
        }
#pragma warning disable CA1031 // Any advancing failure becomes a failure report and an on-screen banner.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Fail("simulation step failed", ex);
            return;
        }

        var steps = (World.Now.Milliseconds - before.Milliseconds) / World.StepMs;
        if (steps > 0)
        {
            LastAdvanceSteps = steps;
            LastAdvanceCostMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        }

        if (_pacer.DroppedBacklogCount > 0 && !_backlogWarned)
        {
            _backlogWarned = true;
            _log.Warning($"frame hitch: dropped simulation backlog (the kernel catches up on at most {RealTimePacer.MaxBacklogRealSeconds} s of real time per frame); further drops are not logged");
        }
    }

    /// <summary>Applies a world command now and returns its journal entry; null when the host has already failed or submission threw.</summary>
    public JournalEntry? TrySubmit(SimCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (Failure is not null)
        {
            return null;
        }

        try
        {
            return World.Submit(command);
        }
#pragma warning disable CA1031 // Any submission failure becomes a failure report and an on-screen banner.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Fail("command submission failed", ex);
            return null;
        }
    }

    /// <summary>Applies a pacing command to the world's time control; null when applied, else why it was refused.</summary>
    public string? TryControl(ControlCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Failure is null ? World.Control(command) : "the simulation has stopped";
    }

    /// <summary>Writes a report of the current state without stopping the host; null when it could not be written.</summary>
    public string? WriteReport(string reason, Exception? exception = null) => _reporter.TryWrite(World, reason, exception);

    private static SimTime? Earliest(params SimTime?[] times)
    {
        SimTime? earliest = null;
        foreach (var time in times)
        {
            if (time is { } candidate && (earliest is null || candidate < earliest.Value))
            {
                earliest = candidate;
            }
        }

        return earliest;
    }

    private void SubmitDueScript()
    {
        while (_nextScripted < _script.Count && _script[_nextScripted].At <= World.Now)
        {
            var command = _script[_nextScripted++].Command;
            if (command is ControlCommand control)
            {
                World.Control(control);
            }
            else
            {
                World.Submit(command);
            }
        }
    }

    private void Fail(string reason, Exception exception)
    {
        _log.Error(reason, exception);
        var path = _reporter.TryWrite(World, $"{reason}: {exception.Message}", exception);
        Failure = new HostFailure($"{reason}: {exception.Message}", path, _reporter.ReplayCommand(path ?? _reporter.ReportsDirectory));
    }
}
