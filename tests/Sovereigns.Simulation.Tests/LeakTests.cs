using System.Runtime.CompilerServices;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Geometry;

namespace Sovereigns.Simulation.Tests;

/// <summary>Development leak detection (SOV-P01-T15): nothing static may keep a discarded world alive.</summary>
public sealed class LeakTests
{
    [Fact]
    public void DiscardedWorldIsCollected()
    {
        var reference = RunAndDiscard();

        for (var attempt = 0; attempt < 5 && reference.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.False(reference.IsAlive, "a World is still reachable after its host released it");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RunAndDiscard()
    {
        var world = World.Create(TestSupport.Scenario(), seed: 1, new Logger([new RecentLogBuffer(8)], LogSeverity.Debug));
        world.Submit(new PlaceMarker(new WorldPoint(1, 1), "m"));
        world.AdvanceTo(new Time.SimTime(10_000));
        _ = world.Inspect(new WorldPoint(1, 1), 5);
        _ = world.ComputeChecksum();
        return new WeakReference(world);
    }
}
