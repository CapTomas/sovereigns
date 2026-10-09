using System.Text.Json;
using Sovereigns.Simulation.Serialization;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Commands;

/// <summary>
/// One applied command: its global order, the simulation time at which it took effect and,
/// when the world refused it, the reason. Replaying the entries in order reproduces the state.
/// </summary>
public sealed record JournalEntry(long Sequence, SimTime At, SimCommand Command, string? Rejection = null);

/// <summary>Ordered record of every command a world applied (SOV-P01-T12).</summary>
public sealed class CommandJournal
{
    private readonly List<JournalEntry> _entries = [];

    public IReadOnlyList<JournalEntry> Entries => _entries;

    internal JournalEntry Append(SimTime at, SimCommand command, string? rejection)
    {
        var entry = new JournalEntry(_entries.Count + 1, at, command, rejection);
        _entries.Add(entry);
        return entry;
    }

    /// <summary>Writes one JSON object per line.</summary>
    public static void WriteJsonLines(IEnumerable<JournalEntry> entries, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(writer);
        foreach (var entry in entries)
        {
            writer.Write(JsonSerializer.Serialize(entry, SimJson.Options));
            writer.Write('\n');
        }
    }

    /// <summary>Reads a journal written by <see cref="WriteJsonLines"/>, checking sequence and time order.</summary>
    /// <exception cref="FormatException">A line is malformed or out of order; the message names the line.</exception>
    public static IReadOnlyList<JournalEntry> ReadJsonLines(TextReader reader, string source)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var entries = new List<JournalEntry>();
        var lineNumber = 0;
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            if (line.Length == 0)
            {
                continue;
            }

            JournalEntry? entry;
            try
            {
                entry = JsonSerializer.Deserialize<JournalEntry>(line, SimJson.Options);
            }
            catch (JsonException ex)
            {
                throw new FormatException($"{source}:{lineNumber}: invalid journal entry: {ex.Message}", ex);
            }

            if (entry is null)
            {
                throw new FormatException($"{source}:{lineNumber}: journal entry is null");
            }

            if (entry.Sequence != entries.Count + 1)
            {
                throw new FormatException($"{source}:{lineNumber}: expected sequence {entries.Count + 1}, found {entry.Sequence}");
            }

            if (entries.Count > 0 && entry.At < entries[^1].At)
            {
                throw new FormatException($"{source}:{lineNumber}: time {entry.At} is earlier than the previous entry {entries[^1].At}");
            }

            entries.Add(entry);
        }

        return entries;
    }
}
