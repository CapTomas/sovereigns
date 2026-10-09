namespace Sovereigns.Simulation.Diagnostics;

/// <summary>Receives log records. Implementations must not throw for ordinary writes.</summary>
public interface ILogSink
{
    void Write(LogRecord record);
}

/// <summary>
/// Routes structured records to sinks. Hosts create one logger, choose the sinks and inject the
/// wall clock; simulation code never reads wall time itself (ADR-0004 §3).
/// </summary>
public sealed class Logger
{
    private readonly ILogSink[] _sinks;
    private readonly TimeProvider? _wallClock;
    private readonly Lock _gate = new();

    public Logger(IEnumerable<ILogSink> sinks, LogSeverity minimum = LogSeverity.Info, TimeProvider? wallClock = null)
    {
        ArgumentNullException.ThrowIfNull(sinks);
        _sinks = [.. sinks];
        Minimum = minimum;
        _wallClock = wallClock;
    }

    /// <summary>A logger that discards everything.</summary>
    public static Logger None { get; } = new([], LogSeverity.Fatal);

    public LogSeverity Minimum { get; }

    public SubsystemLog For(string subsystem, ILogContext? context = null) => new(this, subsystem, context);

    public void Write(LogRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (record.Severity < Minimum)
        {
            return;
        }

        if (_wallClock is not null && record.WallTimeUtc is null)
        {
            record = record with { WallTimeUtc = _wallClock.GetUtcNow() };
        }

        lock (_gate)
        {
            foreach (var sink in _sinks)
            {
                sink.Write(record);
            }
        }
    }
}

/// <summary>Writes records for one subsystem, stamped with the bound world's seed and time.</summary>
public readonly struct SubsystemLog
{
    private readonly Logger _logger;
    private readonly ILogContext? _context;

    internal SubsystemLog(Logger logger, string subsystem, ILogContext? context)
    {
        _logger = logger;
        Subsystem = subsystem;
        _context = context;
    }

    public string Subsystem { get; }

    public bool IsEnabled(LogSeverity severity) => _logger is not null && severity >= _logger.Minimum;

    public void Write(LogSeverity severity, string message, IReadOnlyList<string>? objectIds = null,
        IReadOnlyDictionary<string, string>? data = null, Exception? exception = null)
    {
        if (!IsEnabled(severity))
        {
            return;
        }

        _logger.Write(new LogRecord(severity, Subsystem, message, _context?.WorldSeed, _context?.SimTimeMs,
            objectIds, data, Exception: exception?.ToString()));
    }

    public void Debug(string message, IReadOnlyList<string>? objectIds = null, IReadOnlyDictionary<string, string>? data = null) =>
        Write(LogSeverity.Debug, message, objectIds, data);

    public void Info(string message, IReadOnlyList<string>? objectIds = null, IReadOnlyDictionary<string, string>? data = null) =>
        Write(LogSeverity.Info, message, objectIds, data);

    public void Warning(string message, IReadOnlyList<string>? objectIds = null, IReadOnlyDictionary<string, string>? data = null) =>
        Write(LogSeverity.Warning, message, objectIds, data);

    public void Error(string message, Exception? exception = null, IReadOnlyList<string>? objectIds = null) =>
        Write(LogSeverity.Error, message, objectIds, exception: exception);
}
