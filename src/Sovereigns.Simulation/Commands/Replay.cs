using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Identity;
using Sovereigns.Simulation.Scenarios;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Commands;

/// <summary>Rebuilds a world from its scenario, seed and recorded command journal.</summary>
public static class Replay
{
    /// <summary>
    /// Creates a fresh world, advances to each entry's time, submits it, and finally advances to
    /// <paramref name="until"/>. Throws when the journal cannot be applied exactly as recorded: a decision stops
    /// time before an entry, or an entry's outcome differs.
    /// </summary>
    public static World Run(ScenarioDefinition scenario, ulong seed, IReadOnlyList<JournalEntry> journal, SimTime until,
        Logger? logger = null, WorldId id = default)
    {
        ArgumentNullException.ThrowIfNull(journal);
        return Continue(World.Create(scenario, seed, logger, id), journal, until);
    }

    /// <summary>Applies a recorded journal to an existing world, such as one loaded from a save, then advances to <paramref name="until"/>.</summary>
    public static World Continue(World world, IReadOnlyList<JournalEntry> journal, SimTime until)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(journal);
        foreach (var entry in journal)
        {
            if (world.AdvanceTo(entry.At) != entry.At)
            {
                throw new InvalidOperationException($"replay stopped at a pending decision at {world.Now}, before journal entry {entry.Sequence} at {entry.At}");
            }

            if (entry.Command is ControlCommand)
            {
                throw new InvalidOperationException($"journal entry {entry.Sequence} is a control command; journals hold world commands only, and hosts apply scripted pacing with World.Control");
            }

            var replayed = world.Submit(entry.Command);
            if (replayed != entry)
            {
                throw new InvalidOperationException($"replay diverged from the journal: entry {entry.Sequence}: expected {entry}, replayed {replayed}");
            }
        }

        if (world.Now > until)
        {
            throw new InvalidOperationException($"journal applies commands up to {world.Now}, after the requested end {until}");
        }

        if (world.AdvanceTo(until) != until)
        {
            throw new InvalidOperationException($"replay stopped at a pending decision at {world.Now}, before the requested end {until}");
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
