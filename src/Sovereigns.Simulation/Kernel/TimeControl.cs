using Sovereigns.Simulation.Ids;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Kernel;

/// <summary>
/// How hosts pace the world (SOV-P02-T11): paused, speed in simulated milliseconds per real second, and requested
/// single steps. It changes only through <see cref="World.Control"/>, which does not journal. It is not world state:
/// it never alters outcomes, only how far a host advances the world per real second, so it is excluded from
/// checksums and saves.
/// </summary>
public sealed class TimeControl
{
    /// <summary>Speed presets: one simulated second, minute, hour, day and week per real second.</summary>
    public static readonly IReadOnlyList<long> Presets = [1_000, 60_000, 3_600_000, 86_400_000, 604_800_000];

    /// <summary>Fastest allowed pace, one simulated year per real second; keeps pacing arithmetic far from overflow.</summary>
    public const long MaxSimMillisecondsPerRealSecond = 31_536_000_000;

    public bool Paused { get; internal set; }

    public long SimMillisecondsPerRealSecond { get; internal set; } = Presets[0];

    public int RequestedSteps { get; internal set; }
}

/// <summary>A developer breakpoint: a decision interrupt scheduled at <see cref="At"/>.</summary>
public sealed record Breakpoint(EntityId<Kind.Breakpoint> Id, SimTime At, string Label, EntityId<Kind.Event> SetBy);

/// <summary>
/// A decision the world is waiting for. While any is pending, the kernel advances no further, whatever the speed
/// or requested target (SOV-P02-T11); acknowledging it is a journaled command.
/// </summary>
public sealed record PendingDecision(EntityId<Kind.Event> Event, SimTime At, string Source, string Label);
