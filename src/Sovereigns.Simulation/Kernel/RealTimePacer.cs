using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Kernel;

/// <summary>
/// Turns a host's real elapsed time into whole simulation steps at the world's <see cref="TimeControl"/> pace
/// (SOV-P02-T11). It decides only how far the world advances per real second, never what happens: outcomes at a
/// given simulation time are the same for any frame rate, pause pattern or speed (SOV-P02-T16). It is host state
/// and is not saved.
/// </summary>
public sealed class RealTimePacer
{
    /// <summary>Most real time a single call may catch up on; a longer frame hitch drops the excess.</summary>
    public const double MaxBacklogRealSeconds = 0.25;

    private long _owedMicroseconds;

    /// <summary>How many calls dropped backlog after a hitch; hosts may warn once.</summary>
    public int DroppedBacklogCount { get; private set; }

    /// <summary>
    /// Advances <paramref name="world"/> for <paramref name="realSeconds"/> of host time and returns the time reached.
    /// It never passes <paramref name="limit"/>, such as the time of the next scripted command a host must submit.
    /// </summary>
    public SimTime Advance(World world, double realSeconds, SimTime? limit = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (world.PendingDecisions.Count > 0)
        {
            _owedMicroseconds = 0;
            return world.Now;
        }

        if (limit is { } stop && stop <= world.Now)
        {
            return world.Now;
        }

        if (world.TryTakeRequestedStep())
        {
            _owedMicroseconds = 0;
            return world.Step();
        }

        if (world.TimeControl.Paused || !double.IsFinite(realSeconds) || realSeconds <= 0)
        {
            if (world.TimeControl.Paused)
            {
                _owedMicroseconds = 0;
            }

            return world.Now;
        }

        var speed = world.TimeControl.SimMillisecondsPerRealSecond;
        var stepMicroseconds = world.StepMs * 1000;
        _owedMicroseconds += (long)Math.Round(realSeconds * speed * 1000);
        var cap = Math.Max(stepMicroseconds, (long)(speed * 1000 * MaxBacklogRealSeconds));
        if (_owedMicroseconds > cap)
        {
            _owedMicroseconds = cap;
            DroppedBacklogCount++;
        }

        var steps = _owedMicroseconds / stepMicroseconds;
        if (steps == 0)
        {
            return world.Now;
        }

        var before = world.Now;
        var target = before.Plus(steps * world.StepMs);
        if (limit is { } scripted && scripted < target)
        {
            target = scripted;
        }

        var reached = world.AdvanceTo(target);
        _owedMicroseconds -= (reached.Milliseconds - before.Milliseconds) * 1000;
        if (world.PendingDecisions.Count > 0)
        {
            _owedMicroseconds = 0;
        }

        return reached;
    }
}
