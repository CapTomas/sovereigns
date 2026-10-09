namespace Sovereigns.Simulation.Diagnostics;

public enum LogSeverity
{
    Trace,
    Debug,
    Info,
    Warning,
    Error,
    Fatal,
}

/// <summary>
/// One structured log event (SOV-P01-T05). World seed and simulation time come from the bound world;
/// object IDs use their typed <c>kind:value</c> form. Wall time is diagnostic only and comes from the host.
/// </summary>
public sealed record LogRecord(
    LogSeverity Severity,
    string Subsystem,
    string Message,
    ulong? WorldSeed = null,
    long? SimTimeMs = null,
    IReadOnlyList<string>? ObjectIds = null,
    IReadOnlyDictionary<string, string>? Data = null,
    DateTimeOffset? WallTimeUtc = null,
    string? Exception = null);

/// <summary>Supplies the world seed and current simulation time for records written about a world.</summary>
public interface ILogContext
{
    ulong WorldSeed { get; }

    long SimTimeMs { get; }
}
