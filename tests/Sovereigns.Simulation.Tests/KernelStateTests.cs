using System.Text.Json;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Events;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Ids;
using Sovereigns.Simulation.Serialization;
using Sovereigns.Simulation.State;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Tests;

/// <summary>SOV-P02-T05, T07, T08, T09, T10, T14: IDs, ledger, ownership, caches and side-effect-free queries.</summary>
public sealed class KernelStateTests
{
    [Fact]
    public void EachKindCountsSeparatelyAndNeverReusesIds()
    {
        var ids = new IdAllocator();

        Assert.Equal(1UL, ids.Next<Kind.Settlement>().Value);
        Assert.Equal(1UL, ids.Next<Kind.Army>().Value);
        Assert.Equal(2UL, ids.Next<Kind.Settlement>().Value);
        Assert.Equal("settlement:2", new EntityId<Kind.Settlement>(2).ToString());
        Assert.True(ids.WasIssued(new EntityId<Kind.Settlement>(2)));
        Assert.False(ids.WasIssued(new EntityId<Kind.Settlement>(3)));
        Assert.False(ids.WasIssued(default(EntityId<Kind.Settlement>)));
        Assert.Equal(["army", "settlement"], ids.Counters.Keys);
    }

    [Fact]
    public void IdsSerializeAsTheirNumber() =>
        Assert.Equal("7", JsonSerializer.Serialize(new EntityId<Kind.Polity>(7), SimJson.Options));

    [Fact]
    public void LedgerRefusesToGoBackInTime()
    {
        var ledger = new EventLedger();
        ledger.Append(new WorldEvent(new EntityId<Kind.Event>(1), new SimTime(5_000), WorldEventType.WorldCreated, EventCause.Origin, []));

        var backwards = Assert.Throws<InvalidOperationException>(() =>
            ledger.Append(new WorldEvent(new EntityId<Kind.Event>(2), new SimTime(4_000), WorldEventType.YearBegan, EventCause.Origin, [])));
        Assert.Contains("chronological", backwards.Message, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() =>
            ledger.Append(new WorldEvent(new EntityId<Kind.Event>(1), new SimTime(6_000), WorldEventType.YearBegan, EventCause.Origin, [])));
        Assert.True(ledger.TryGet(new EntityId<Kind.Event>(1), out var found));
        Assert.Equal(WorldEventType.WorldCreated, found.Type);
        Assert.False(ledger.TryGet(new EntityId<Kind.Event>(9), out _));
    }

    [Fact]
    public void LedgerRecordsEveryStateChangeWithItsCause()
    {
        var world = World.Create(TestSupport.Scenario(), seed: 1);
        world.Submit(new PlaceMarker(new WorldPoint(1, 1), "a"));
        world.Submit(new RemoveMarker(new EntityId<Kind.Marker>(1)));
        world.AdvanceTo(new SimTime(WorldCalendar.MillisecondsPerDay));

        Assert.Equal([WorldEventType.WorldCreated, WorldEventType.MarkerPlaced, WorldEventType.MarkerRemoved, WorldEventType.YearBegan],
            world.Ledger.Events.Select(e => e.Type));
        Assert.Equal(["origin", "command #1", "command #2", "system calendar"], world.Ledger.Events.Select(e => e.Cause.ToString()));
        Assert.All(world.Ledger.Events.Zip(world.Ledger.Events.Skip(1)), pair => Assert.True(pair.First.Id.Value < pair.Second.Id.Value));
    }

    [Fact]
    public void DerivedCacheRecomputesOnlyAfterItsSourcesChange()
    {
        var source = new StateVersion("markers");
        var computations = 0;
        var cache = new DerivedCache<int>(() => ++computations, source);

        Assert.Equal(1, cache.Value);
        Assert.Equal(1, cache.Value);
        source.Bump();
        Assert.Equal(2, cache.Value);
        Assert.Equal(["markers"], cache.Sources);
        Assert.Throws<ArgumentException>(() => new DerivedCache<int>(() => 0));
    }

    [Fact]
    public void MarkerQueriesReuseTheIndexUntilMarkersChange()
    {
        var world = World.Create(TestSupport.Scenario(), seed: 1);
        world.Submit(new PlaceMarker(new WorldPoint(100, 100), "a"));
        for (var i = 0; i < 50; i++)
        {
            world.Inspect(new WorldPoint(100 + i, 100), markerRadiusM: 10);
        }

        Assert.Equal(1, world.MarkerIndexComputations);
        world.Submit(new PlaceMarker(new WorldPoint(5_000, 5_000), "b"));
        Assert.Equal("b", Assert.Single(world.Inspect(new WorldPoint(5_000, 5_005), markerRadiusM: 10).NearbyMarkers).Label);
        Assert.Equal(2, world.MarkerIndexComputations);
        Assert.Equal(2, world.Inspect(new WorldPoint(2_500, 2_500), markerRadiusM: 1e9).NearbyMarkers.Count);
    }

    [Fact]
    public void QueriesNeverChangeStateWhateverTheView()
    {
        // ADR-0007 query semantics: inspection is read-only and residency-independent, so any pattern of views,
        // inside or outside the world, leaves the simulation exactly as it was.
        static World Run(bool inspect)
        {
            var world = World.Create(TestSupport.Scenario(), seed: 4);
            world.Submit(new PlaceMarker(new WorldPoint(1_000, 1_000), "m"));
            for (var day = 1; day <= 30; day++)
            {
                world.AdvanceTo(new SimTime(day * WorldCalendar.MillisecondsPerDay));
                if (inspect)
                {
                    foreach (var point in new[] { new WorldPoint(day * 100, 50), new WorldPoint(-1e9, 1e300), new WorldPoint(double.NaN, 0) })
                    {
                        world.Inspect(point, markerRadiusM: day * 1_000);
                    }
                }
            }

            return world;
        }

        Assert.Equal(Run(inspect: false).ComputeChecksum(), Run(inspect: true).ComputeChecksum());
    }
}
