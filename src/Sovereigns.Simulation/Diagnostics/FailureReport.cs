using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Serialization;

namespace Sovereigns.Simulation.Diagnostics;

/// <summary>Build and platform that produced a report.</summary>
public sealed record BuildInfo(string Version, string Configuration, string Runtime, string Os, string Architecture)
{
    public static BuildInfo Describe(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return new(
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown",
            assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ?? "unknown",
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.OSDescription,
            RuntimeInformation.ProcessArchitecture.ToString());
    }
}

/// <summary>A host-side input action, recorded for failure context; it is not authoritative state.</summary>
public sealed record HostInputEvent(long Frame, double RealSeconds, string Action, string? Detail = null);

/// <summary>
/// What a developer needs to reproduce a failure (SOV-P01-T09): seed, scenario, simulation time, the applied
/// command journal, recent host inputs and recent log records.
/// </summary>
/// <remarks>
/// Replaying <see cref="Commands"/> to <see cref="SimTimeMs"/>, from the scenario or from the save with
/// <see cref="BaseSaveSha256"/>, rebuilds the state before the failure. When
/// <see cref="FailedDuring"/> is set, an exception left the state partly updated, so <see cref="StateChecksum"/> is
/// null. Reproduce a <see cref="FaultStage.Command"/> failure by submitting <see cref="FailingCommand"/>, and a
/// <see cref="FaultStage.Step"/> failure by stepping once. Null members are written explicitly so that every
/// report reads back.
/// </remarks>
public sealed record FailureReport(
    int Format,
    string Host,
    string Reason,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] string? Exception,
    BuildInfo Build,
    string WorldId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] string? BaseSaveSha256,
    ulong Seed,
    string Scenario,
    long SimTimeMs,
    FaultStage FailedDuring,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] SimCommand? FailingCommand,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] string? StateChecksum,
    IReadOnlyList<JournalEntry> Commands,
    IReadOnlyList<HostInputEvent> Inputs,
    IReadOnlyList<LogRecord> RecentLog)
{
    public const int CurrentFormat = 2;

    public static FailureReport Capture(World world, string host, string reason, Exception? exception, BuildInfo build,
        RecentLogBuffer? recentLog = null, IReadOnlyList<HostInputEvent>? inputs = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        string? checksum = null;
        if (world.Fault == FaultStage.None)
        {
            try
            {
                checksum = world.ComputeChecksum();
            }
#pragma warning disable CA1031 // The report must still be written when the failed state cannot be hashed.
            catch (Exception)
#pragma warning restore CA1031
            {
                checksum = null;
            }
        }

        return new(CurrentFormat, host, reason, exception?.ToString(), build, world.Identity.Id.ToString(), world.BaseSaveSha256, world.Seed,
            world.ScenarioId.ToString(), world.Now.Milliseconds, world.Fault, world.FaultingCommand, checksum,
            [.. world.Journal.Entries], inputs ?? [], recentLog?.Snapshot() ?? []);
    }
    /// <summary>A file name that sorts by seed and simulation time.</summary>
    [JsonIgnore]
    public string FileName => $"failure-seed{Seed}-t{SimTimeMs}ms.json";

    public void WriteTo(Stream stream) => JsonSerializer.Serialize(stream, this, SimJson.Indented);

    /// <summary>
    /// Writes the report into <paramref name="directory"/> under <see cref="FileName"/>, adding <c>-2</c>, <c>-3</c>, …
    /// rather than replacing an earlier report from the same seed and time. Returns the path written.
    /// </summary>
    public string WriteToDirectory(string directory)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(this, SimJson.Indented);
        Directory.CreateDirectory(directory);
        for (var attempt = 1; ; attempt++)
        {
            var name = attempt == 1 ? FileName : $"{Path.GetFileNameWithoutExtension(FileName)}-{attempt}.json";
            var path = Path.Combine(directory, name);
            try
            {
                using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                stream.Write(bytes);
                return path;
            }
            catch (IOException) when (File.Exists(path))
            {
            }
        }
    }

    public static FailureReport ReadFrom(Stream stream) =>
        JsonSerializer.Deserialize<FailureReport>(stream, SimJson.Options) is { Format: CurrentFormat } report
            ? report
            : throw new JsonException($"not a format {CurrentFormat} failure report");
}
