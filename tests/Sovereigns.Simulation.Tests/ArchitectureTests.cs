namespace Sovereigns.Simulation.Tests;

public sealed class ArchitectureTests
{
    /// <summary>ADR-0001 and the system map: the simulation references only the .NET base library.</summary>
    [Fact]
    public void SimulationReferencesNoEngineOrUiAssemblies()
    {
        var references = typeof(World).Assembly.GetReferencedAssemblies().Select(name => name.Name!).ToList();

        Assert.NotEmpty(references);
        Assert.All(references, name => Assert.True(
            name.StartsWith("System", StringComparison.Ordinal) || name is "netstandard" or "mscorlib",
            $"Sovereigns.Simulation must not reference {name}"));
    }
}
