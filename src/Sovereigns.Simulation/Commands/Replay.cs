using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Scenarios;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Commands;

/// <summary>Rebuilds a world from its scenario, seed and recorded command journal.</summary>
public static class Replay
{
    /// <summary>
    /// Creates a fresh world, submits each journal entry so it takes effect at its recorded time, and
    /// advances to <paramref name="until"/>. Throws when the journal cannot be applied as recorded.
    /// </summary>
    public static World Run(ScenarioDefinition scenario, ulong seed, IReadOnlyList<JournalEntry> journal, SimTime until, Logger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(journal);
        var world = World.Create(scenario, seed, logger);
        foreach (var group in journal.GroupBy(entry => entry.At))
        {
            world.AdvanceTo(group.Key);
            foreach (var entry in group)
            {
                world.Submit(entry.Command);
            }

            world.Step();
        }

        if (world.Now > until)
        {
            throw new InvalidOperationException($"journal applies commands up to {world.Now}, after the requested end {until}");
        }

        world.AdvanceTo(until);
        if (FirstDivergence(journal, world.Journal.Entries) is { } divergence)
        {
            throw new InvalidOperationException($"replay diverged from the journal: {divergence}");
        }

        return world;
    }

    /// <summary>Describes the first entry where two journals differ, or null when they match.</summary>
    public static string? FirstDivergence(IReadOnlyList<JournalEntry> expected, IReadOnlyList<JournalEntry> actual)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);
        for (var i = 0; i < Math.Min(expected.Count, actual.Count); i++)
        {
            if (expected[i] != actual[i])
            {
                return $"entry {i + 1}: expected {expected[i]}, replayed {actual[i]}";
            }
        }

        return expected.Count == actual.Count ? null : $"expected {expected.Count} entries, replayed {actual.Count}";
    }
}
