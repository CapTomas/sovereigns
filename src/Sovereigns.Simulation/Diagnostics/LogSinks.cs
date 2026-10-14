using System.Globalization;
using System.Text;
using System.Text.Json;
using Sovereigns.Simulation.Serialization;

namespace Sovereigns.Simulation.Diagnostics;

/// <summary>Machine-readable log: one JSON object per line.</summary>
public sealed class JsonLinesLogSink(TextWriter writer) : ILogSink
{
    public void Write(LogRecord record)
    {
        writer.Write(JsonSerializer.Serialize(record, SimJson.Options));
        writer.Write('\n');
        writer.Flush();
    }
}

/// <summary>Human-readable single-line log for consoles.</summary>
public sealed class TextLogSink(TextWriter writer) : ILogSink
{
    public void Write(LogRecord record)
    {
        writer.WriteLine(Format(record));
        writer.Flush();
    }

    public static string Format(LogRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var text = new StringBuilder();
        text.Append(CultureInfo.InvariantCulture, $"[{record.Severity.ToString().ToUpperInvariant()}] {record.Subsystem}");
        if (record.WorldSeed is { } seed)
        {
            text.Append(CultureInfo.InvariantCulture, $" seed={seed}");
        }

        if (record.SimTimeMs is { } time)
        {
            text.Append(CultureInfo.InvariantCulture, $" t={time}ms");
        }

        text.Append(": ").Append(record.Message);
        if (record.ObjectIds is { Count: > 0 } ids)
        {
            text.Append(" [").AppendJoin(", ", ids).Append(']');
        }

        if (record.Data is { Count: > 0 } data)
        {
            text.Append(" {").AppendJoin(", ", data.Select(pair => $"{pair.Key}={pair.Value}")).Append('}');
        }

        if (record.Exception is not null)
        {
            text.AppendLine().Append(record.Exception);
        }

        return text.ToString();
    }
}

/// <summary>Keeps the most recent records for failure reports and the debug HUD.</summary>
public sealed class RecentLogBuffer(int capacity) : ILogSink
{
    private readonly Queue<LogRecord> _records = new(capacity);
    private readonly Lock _gate = new();

    public int WarningCount { get; private set; }

    public int ErrorCount { get; private set; }

    public LogRecord? LastWarningOrError { get; private set; }

    public void Write(LogRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        lock (_gate)
        {
            if (_records.Count == capacity)
            {
                _records.Dequeue();
            }

            _records.Enqueue(record);
            if (record.Severity == LogSeverity.Warning)
            {
                WarningCount++;
            }
            else if (record.Severity >= LogSeverity.Error)
            {
                ErrorCount++;
            }

            if (record.Severity >= LogSeverity.Warning)
            {
                LastWarningOrError = record;
            }
        }
    }

    public IReadOnlyList<LogRecord> Snapshot()
    {
        lock (_gate)
        {
            return [.. _records];
        }
    }
}
