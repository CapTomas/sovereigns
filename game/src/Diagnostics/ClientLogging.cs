using Sovereigns.Simulation.Diagnostics;

namespace Sovereigns.Client.Diagnostics;

/// <summary>
/// The run's one <see cref="Logger"/> and its sinks: Godot output, the recent-record buffer for the HUD and
/// failure reports, and a JSON Lines file. The client may read wall time, so the logger stamps records with it.
/// </summary>
public sealed class ClientLogging : IDisposable
{
    private readonly StreamWriter? _file;

    private ClientLogging(Logger logger, RecentLogBuffer recent, StreamWriter? file, string? filePath)
    {
        Logger = logger;
        Recent = recent;
        _file = file;
        FilePath = filePath;
    }

    public Logger Logger { get; }

    public RecentLogBuffer Recent { get; }

    /// <summary>The JSON Lines log file, or null when it could not be created.</summary>
    public string? FilePath { get; }

    /// <summary>A log file name that sorts by start time: <c>sovereigns-20261009T141500Z.jsonl</c>.</summary>
    public static string DefaultFileName() =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture, $"sovereigns-{DateTime.UtcNow:yyyyMMdd'T'HHmmss'Z'}.jsonl");

    public static ClientLogging Create(string logFilePath)
    {
        var recent = new RecentLogBuffer(256);
        var sinks = new List<ILogSink> { new GodotLogSink(), recent };
        StreamWriter? file = null;
        string? problem = null;
        try
        {
            if (Path.GetDirectoryName(Path.GetFullPath(logFilePath)) is { } directory)
            {
                Directory.CreateDirectory(directory);
            }

            file = new StreamWriter(logFilePath, append: true) { NewLine = "\n" };
            sinks.Add(new JsonLinesLogSink(file));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            problem = $"cannot write the log file {logFilePath}: {ex.Message}";
        }

        var logging = new ClientLogging(new Logger(sinks, LogSeverity.Info, TimeProvider.System), recent, file, file is null ? null : logFilePath);
        if (problem is not null)
        {
            logging.Logger.For("client").Warning(problem);
        }

        return logging;
    }

    public void Dispose() => _file?.Dispose();
}
