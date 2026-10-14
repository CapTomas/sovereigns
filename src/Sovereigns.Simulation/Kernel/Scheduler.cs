using Sovereigns.Simulation.State;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Kernel;

/// <summary>Registered systems, their cadences and dependency order (SOV-P02-T04).</summary>
internal sealed class Scheduler
{
    private readonly List<ISimulationSystem> _systems = [];

    public IReadOnlyList<ISimulationSystem> Systems => _systems;

    public void Register(ISimulationSystem system, long stepMs, OwnershipRegistry ownership)
    {
        ArgumentNullException.ThrowIfNull(system);
        if (_systems.Any(existing => existing.Id == system.Id))
        {
            throw new DuplicateOwnerException($"system '{system.Id}' is already registered");
        }

        if (system.CadenceMs <= 0 || system.CadenceMs % stepMs != 0)
        {
            throw new ArgumentException($"system '{system.Id}' cadence {system.CadenceMs} ms must be a positive multiple of the {stepMs} ms step");
        }

        if (system.PhaseMs < 0 || system.PhaseMs >= system.CadenceMs || system.PhaseMs % stepMs != 0)
        {
            throw new ArgumentException($"system '{system.Id}' phase {system.PhaseMs} ms must be a multiple of the step within its cadence");
        }

        foreach (var component in system.OwnedComponents)
        {
            ownership.Register(component, system.Id);
        }

        _systems.Add(system);
        _systems.Sort((a, b) => a.Priority != b.Priority ? a.Priority.CompareTo(b.Priority) : string.CompareOrdinal(a.Id, b.Id));
    }

    public IEnumerable<ISimulationSystem> DueAt(SimTime time) =>
        _systems.Where(system => Mod(time.Milliseconds - system.PhaseMs, system.CadenceMs) == 0);

    /// <summary>The earliest system update strictly after <paramref name="time"/>, or null when nothing is registered.</summary>
    public SimTime? NextDueAfter(SimTime time)
    {
        SimTime? next = null;
        foreach (var system in _systems)
        {
            var due = time.Plus(system.CadenceMs - Mod(time.Milliseconds - system.PhaseMs, system.CadenceMs));
            if (next is null || due < next.Value)
            {
                next = due;
            }
        }

        return next;
    }

    private static long Mod(long value, long divisor)
    {
        var remainder = value % divisor;
        return remainder < 0 ? remainder + divisor : remainder;
    }
}
