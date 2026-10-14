using Sovereigns.Simulation.Events;
using Sovereigns.Simulation.Random;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Kernel;

/// <summary>
/// A scheduled subsystem (SOV-P02-T04, ADR-0007). It runs at times t with (t − <see cref="PhaseMs"/>) divisible by
/// <see cref="CadenceMs"/>; systems due at the same time run in ascending <see cref="Priority"/>, then by
/// <see cref="Id"/>. It owns its <see cref="OwnedComponents"/> and writes nothing else.
/// </summary>
public interface ISimulationSystem
{
    /// <summary>Stable lowercase name, used in causes, ownership and logs.</summary>
    string Id { get; }

    /// <summary>Position in the dependency order of ADR-0007; lower runs first.</summary>
    int Priority { get; }

    long CadenceMs { get; }

    long PhaseMs { get; }

    IReadOnlyList<string> OwnedComponents { get; }

    void Update(SystemContext context);
}

/// <summary>What a system may do during its update: read the world, record events it caused, draw keyed randomness.</summary>
public sealed class SystemContext
{
    private readonly Action<WorldEventType, IReadOnlyList<string>, IReadOnlyDictionary<string, string>?> _record;

    internal SystemContext(World world, ISimulationSystem system,
        Action<WorldEventType, IReadOnlyList<string>, IReadOnlyDictionary<string, string>?> record)
    {
        World = world;
        System = system;
        _record = record;
    }

    public World World { get; }

    public ISimulationSystem System { get; }

    public SimTime Now => World.Now;

    /// <summary>
    /// Start of the interval this update covers: the previous due time, or the world's start when the world began
    /// between two due times (ADR-0007). Integrate over [<see cref="Since"/>, <see cref="Now"/>).
    /// </summary>
    public SimTime Since
    {
        get
        {
            var previous = Now.Plus(-System.CadenceMs);
            return previous < World.Identity.Start ? World.Identity.Start : previous;
        }
    }

    public void Record(WorldEventType type, IReadOnlyList<string> subjects, IReadOnlyDictionary<string, string>? data = null) =>
        _record(type, subjects, data);

    /// <summary>A random stream for this system, keyed by purpose and place, entity or event values.</summary>
    public RandomStream Stream(string purpose, params ReadOnlySpan<ulong> keys) =>
        RandomStream.For(World.Identity.Seed, $"{System.Id}.{purpose}", keys);
}
