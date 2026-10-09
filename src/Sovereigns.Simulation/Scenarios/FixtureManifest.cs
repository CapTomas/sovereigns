using System.Text.Json;
using System.Text.Json.Serialization;
using Sovereigns.Simulation.Serialization;

namespace Sovereigns.Simulation.Scenarios;

/// <summary>
/// A versioned reference fixture from <c>tests/fixtures/</c>. A defined fixture pins a seed and the
/// repository path of its scenario; evidence cites it as <see cref="Reference"/>.
/// </summary>
public sealed record FixtureManifest(string Id, int Version, string Status, string Purpose, ulong? Seed, string? Scenario)
{
    [JsonIgnore]
    public string Reference => $"{Id}@{Version}";

    [JsonIgnore]
    public bool IsDefined => Status == "defined" && Seed is not null && Scenario is not null;

    public static FixtureManifest Load(string path)
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<FixtureManifest>(stream, SimJson.Options)
            ?? throw new JsonException($"{path}: fixture manifest is null");
    }
}
