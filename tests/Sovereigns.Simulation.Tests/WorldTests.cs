using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Fields;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Markers;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Tests;

public sealed class WorldTests
{
    [Fact]
    public void NewWorldStartsAtEpochAndAdvancesInWholeSteps()
    {
        var world = World.Create(TestSupport.Scenario(stepMs: 250), seed: 7);

        Assert.Equal(SimTime.Zero, world.Now);
        world.Step();
        world.AdvanceTo(new SimTime(1_000));

        Assert.Equal(new SimTime(1_000), world.Now);
        Assert.Equal(4, world.StepCount);
    }

    [Theory]
    [InlineData(999)]
    [InlineData(-1_000)]
    public void AdvanceToRejectsTargetsOffTheStepGrid(long target)
    {
        var world = World.Create(TestSupport.Scenario(stepMs: 1_000), seed: 7);

        Assert.Throws<ArgumentOutOfRangeException>(() => world.AdvanceTo(new SimTime(target)));
        Assert.Equal(SimTime.Zero, world.Now);
    }

    [Fact]
    public void CommandsTakeEffectAtTheNextStepBoundaryInSubmissionOrder()
    {
        var world = World.Create(TestSupport.Scenario(), seed: 7);
        world.AdvanceTo(new SimTime(3_000));

        world.Submit(new PlaceMarker(new WorldPoint(10, 20), "first"));
        world.Submit(new PlaceMarker(new WorldPoint(30, 40), "second"));
        Assert.Empty(world.Markers);
        Assert.Equal(2, world.PendingCommandCount);

        world.Step();

        Assert.Equal(["first", "second"], world.Markers.Select(marker => marker.Label));
        Assert.All(world.Markers, marker => Assert.Equal(new SimTime(3_000), marker.PlacedAt));
        Assert.Equal([1L, 2L], world.Journal.Entries.Select(entry => entry.Sequence));
        Assert.All(world.Journal.Entries, entry => Assert.Equal(new SimTime(3_000), entry.At));
        Assert.Equal(0, world.PendingCommandCount);
    }

    [Fact]
    public void RejectedCommandsAreJournaledLoggedAndLeaveStateUnchanged()
    {
        var sink = new ListSink();
        var world = World.Create(TestSupport.Scenario(east: 100, north: 100), seed: 7, new Logger([sink], LogSeverity.Debug));
        var before = world.ComputeChecksum();

        world.Submit(new PlaceMarker(new WorldPoint(100, 50), "on the east edge"));
        world.Submit(new PlaceMarker(new WorldPoint(double.NaN, 50), "nan"));
        world.Submit(new PlaceMarker(new WorldPoint(5, 5), new string('x', PlaceMarker.MaxLabelLength + 1)));
        world.Submit(new RemoveMarker(new MarkerId(99)));
        world.Step();

        Assert.Empty(world.Markers);
        Assert.Equal(4, world.Journal.Entries.Count);
        Assert.All(world.Journal.Entries, entry => Assert.NotNull(entry.Rejection));
        Assert.Contains("outside the world", world.Journal.Entries[0].Rejection, StringComparison.Ordinal);
        Assert.Contains("marker:99 does not exist", world.Journal.Entries[3].Rejection, StringComparison.Ordinal);
        Assert.Equal(4, sink.Records.Count(record => record.Severity == LogSeverity.Warning));

        // Only time moved; no rejected command changed state.
        var reference = World.Create(TestSupport.Scenario(east: 100, north: 100), seed: 7);
        reference.Step();
        Assert.NotEqual(before, world.ComputeChecksum());
        Assert.Equal(reference.ComputeChecksum(), world.ComputeChecksum());
    }

    [Fact]
    public void MarkerIdsAreMonotonicAndNeverReused()
    {
        var world = World.Create(TestSupport.Scenario(), seed: 7);
        world.Submit(new PlaceMarker(new WorldPoint(1, 1), "a"));
        world.Submit(new PlaceMarker(new WorldPoint(2, 2), "b"));
        world.Step();
        world.Submit(new RemoveMarker(new MarkerId(2)));
        world.Submit(new PlaceMarker(new WorldPoint(3, 3), "c"));
        world.Step();

        Assert.Equal([new MarkerId(1), new MarkerId(3)], world.Markers.Select(marker => marker.Id));
    }

    [Fact]
    public void InspectionReportsEveryFieldFamilyAndFabricatesNoPhysicalValues()
    {
        var world = World.Create(TestSupport.Scenario(), seed: 42);
        world.Submit(new PlaceMarker(new WorldPoint(500, 500), "near"));
        world.Submit(new PlaceMarker(new WorldPoint(900, 500), "far"));
        world.Step();

        var inspection = world.Inspect(new WorldPoint(510, 500), markerRadiusM: 50);

        Assert.True(inspection.InsideWorld);
        Assert.Equal(world.Now, inspection.At);
        Assert.Equal(FieldFamilies.All.Select(family => family.Key), inspection.Families.Select(family => family.Family.Key));
        Assert.Equal(16, inspection.Families.Count);
        var unavailable = inspection.Families.Where(family => family.Availability == FieldAvailability.Unavailable).ToList();
        Assert.Equal(15, unavailable.Count);
        Assert.All(unavailable, family =>
        {
            var reading = Assert.Single(family.Readings);
            Assert.False(reading.IsAvailable);
            Assert.Contains(family.Family.Owner, reading.UnavailableReason, StringComparison.Ordinal);
            Assert.Contains(family.Family.FirstImplementedBy, reading.UnavailableReason, StringComparison.Ordinal);
        });

        var geographic = Assert.Single(inspection.Families, family => family.Availability == FieldAvailability.Partial);
        Assert.Equal((42UL, world.Extent), (inspection.WorldSeed, inspection.WorldExtent));
        Assert.Equal(510.0, geographic.Readings.Single(reading => reading.Quantity == "easting x").Value);
        Assert.Equal(500.0, geographic.Readings.Single(reading => reading.Quantity == "northing y").Value);
        Assert.Equal("m", geographic.Readings.Single(reading => reading.Quantity == "easting x").Unit);
        Assert.All(geographic.Readings.Where(reading => !reading.IsAvailable), reading => Assert.NotNull(reading.UnavailableReason));
        Assert.Equal("near", Assert.Single(inspection.NearbyMarkers).Label);
    }

    [Theory]
    [InlineData(-0.001, 10)]
    [InlineData(10, 8_000)]
    [InlineData(10_000, 10)]
    public void InspectionOutsideTheFrameSaysSo(double x, double y)
    {
        var world = World.Create(TestSupport.Scenario(east: 10_000, north: 8_000), seed: 1);

        var inspection = world.Inspect(new WorldPoint(x, y));

        Assert.False(inspection.InsideWorld);
    }

    [Fact]
    public void ChecksumDependsOnSeedTimeAndCommandsOnly()
    {
        static World Run(ulong seed, long until, bool marker)
        {
            var world = World.Create(TestSupport.Scenario(), seed);
            if (marker)
            {
                world.Submit(new PlaceMarker(new WorldPoint(1, 2), "m"));
            }

            world.AdvanceTo(new SimTime(until));
            return world;
        }

        var baseline = Run(1, 5_000, marker: true).ComputeChecksum();

        Assert.Equal(baseline, Run(1, 5_000, marker: true).ComputeChecksum());
        Assert.NotEqual(baseline, Run(2, 5_000, marker: true).ComputeChecksum());
        Assert.NotEqual(baseline, Run(1, 6_000, marker: true).ComputeChecksum());
        Assert.NotEqual(baseline, Run(1, 5_000, marker: false).ComputeChecksum());
        Assert.Matches("^[0-9a-f]{64}$", baseline);
    }

    [Fact]
    public void CreationRejectsAnInvalidScenario() =>
        Assert.Throws<ArgumentException>(() => World.Create(TestSupport.Scenario(stepMs: 0), seed: 1));
}
