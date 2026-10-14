using Sovereigns.Simulation.Scenarios;

namespace Sovereigns.Simulation.Content;

/// <summary>The definition type registered for each content kind directory.</summary>
public static class ContentKinds
{
    public static IReadOnlyDictionary<string, Type> Default { get; } = new SortedDictionary<string, Type>(StringComparer.Ordinal)
    {
        ["scenario"] = typeof(ScenarioDefinition),
    };
}
