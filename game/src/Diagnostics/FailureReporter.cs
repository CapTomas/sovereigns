using Sovereigns.Simulation;
using Sovereigns.Simulation.Diagnostics;

namespace Sovereigns.Client.Diagnostics;

/// <summary>
/// Writes <see cref="FailureReport"/> files (SOV-P01-T09) with the client's recent log and input history.
/// Reporting must never throw: it runs while the client is already handling a failure.
/// </summary>
public sealed class FailureReporter(string directory, string? repositoryRoot, SubsystemLog log, BuildInfo build,
    RecentLogBuffer recentLog, InputHistory inputs)
{
    public string ReportsDirectory { get; } = directory;

    /// <summary>The headless command that rebuilds the reported world and compares its checksum.</summary>
    public string ReplayCommand(string reportPath) =>
        repositoryRoot is null
            ? $"sovereigns-headless replay \"{reportPath}\""
            : $"dotnet run --project \"{Path.Combine(repositoryRoot, "tools", "Sovereigns.Headless")}\" -- replay \"{reportPath}\" --repo \"{repositoryRoot}\"";

    /// <summary>Writes a report and returns its path, or null when it could not be written.</summary>
    public string? TryWrite(World world, string reason, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(world);
        try
        {
            var report = FailureReport.Capture(world, "client", reason, exception, build, recentLog, inputs.Snapshot());
            var path = report.WriteToDirectory(ReportsDirectory);

            log.Info("failure report written", data: new Dictionary<string, string> { ["path"] = path, ["reason"] = reason });
            return path;
        }
#pragma warning disable CA1031 // A failure while reporting a failure must not hide the original one.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            log.Error($"could not write the failure report to {ReportsDirectory}", ex);
            return null;
        }
    }
}
