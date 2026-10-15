using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Diagnostics;
using Sovereigns.Simulation.Events;
using Sovereigns.Simulation.Fields;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Identity;
using Sovereigns.Simulation.Ids;
using Sovereigns.Simulation.Scenarios;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Tests;

public sealed class WorldTests
{
    [Fact]
    public void NewWorldStartsAtTheScenarioStartAndAdvancesInWholeSteps()
    {
        var start = new CalendarStart(1203, 3, 1, 6);
        var world = World.Create(TestSupport.Scenario(stepMs: 250, start: start), seed: 7);
        var startTime = WorldCalendar.FromCalendar(1203, 3, 1, 6);

        Assert.Equal(startTime, world.Now);
        Assert.Equal(startTime, world.Identity.Start);
        world.Step();
        world.AdvanceTo(startTime.Plus(1_000));

        Assert.Equal(startTime.Plus(1_000), world.Now);
        Assert.Equal(4, world.StepCount);
    }

    [Theory]
    [InlineData(999)]
    [InlineData(-1_000)]
    public void AdvanceToRejectsTargetsOffTheStepGridOrInThePast(long offset)
    {
        var world = World.Create(TestSupport.Scenario(stepMs: 1_000), seed: 7);

        Assert.Throws<ArgumentOutOfRangeException>(() => world.AdvanceTo(world.Now.Plus(offset)));
        Assert.Equal(SimTime.Zero, world.Now);
    }

    [Fact]
    public void CommandsApplyImmediatelyInSubmissionOrderAndAreJournaled()
    {
        var world = World.Create(TestSupport.Scenario(), seed: 7);
        world.AdvanceTo(new SimTime(3_000));

        var first = world.Submit(new PlaceMarker(new WorldPoint(10, 20), "first"));
        var second = world.Submit(new PlaceMarker(new WorldPoint(30, 40), "second"));

        Assert.Equal(["first", "second"], world.Markers.Select(marker => marker.Label));
        Assert.All(world.Markers, marker => Assert.Equal(new SimTime(3_000), marker.PlacedAt));
        Assert.Equal((1L, 2L), (first.Sequence, second.Sequence));
        Assert.All(world.Journal.Entries, entry => Assert.Equal(new SimTime(3_000), entry.At));
        var placed = world.Ledger.Events.Where(worldEvent => worldEvent.Type == WorldEventType.MarkerPlaced).ToList();
        Assert.Equal([1L, 2L], placed.Select(worldEvent => worldEvent.Cause.CommandSequence!.Value));
        Assert.Equal(["marker:1"], placed[0].Subjects);
    }

    [Fact]
    public void RejectedCommandsAreJournaledLoggedAndLeaveStateUnchanged()
    {
        var sink = new ListSink();
        var world = World.Create(TestSupport.Scenario(east: 100, north: 100), seed: 7, new Logger([sink], LogSeverity.Debug));
        var eventsBefore = world.Ledger.Count;

        world.Submit(new PlaceMarker(new WorldPoint(100, 50), "on the east edge"));
        world.Submit(new PlaceMarker(new WorldPoint(double.NaN, 50), "nan"));
        world.Submit(new PlaceMarker(new WorldPoint(5, 5), new string('x', PlaceMarker.MaxLabelLength + 1)));
        world.Submit(new RemoveMarker(new EntityId<Kind.Marker>(99)));
        world.Submit(new SetBreakpoint(world.Now, "now is not the future"));
        world.Submit(new SetBreakpoint(world.Now.Plus(1_500), "off the step grid"));
        world.Submit(new AcknowledgeDecision(new EntityId<Kind.Event>(1)));

        Assert.Empty(world.Markers);
        Assert.Equal(7, world.Journal.Entries.Count);
        Assert.All(world.Journal.Entries, entry => Assert.NotNull(entry.Rejection));
        Assert.Contains("outside the world", world.Journal.Entries[0].Rejection, StringComparison.Ordinal);
        Assert.Contains("marker:99 does not exist", world.Journal.Entries[3].Rejection, StringComparison.Ordinal);
        Assert.Equal(7, sink.Records.Count(record => record.Severity == LogSeverity.Warning));
        Assert.Equal(eventsBefore, world.Ledger.Count);
        Assert.Empty(world.Breakpoints);
        Assert.Empty(world.PendingDecisions);
    }

    [Fact]
    public void MarkerIdsAreMonotonicAndNeverReused()
    {
        var world = World.Create(TestSupport.Scenario(), seed: 7);
        world.Submit(new PlaceMarker(new WorldPoint(1, 1), "a"));
        world.Submit(new PlaceMarker(new WorldPoint(2, 2), "b"));
        world.Submit(new RemoveMarker(new EntityId<Kind.Marker>(2)));
        world.Submit(new PlaceMarker(new WorldPoint(3, 3), "c"));
        var again = world.Submit(new RemoveMarker(new EntityId<Kind.Marker>(2)));

        Assert.Equal([1UL, 3UL], world.Markers.Select(marker => marker.Id.Value));
        Assert.Contains("already removed", again.Rejection, StringComparison.Ordinal);
    }

    [Fact]
    public void InspectionReportsEveryFieldFamilyWithProvenanceAndFabricatesNoPhysicalValues()
    {
        var world = World.Create(TestSupport.Scenario(), seed: 42);
        world.Submit(new PlaceMarker(new WorldPoint(500, 500), "near"));
        world.Submit(new PlaceMarker(new WorldPoint(900, 500), "far"));
        world.AdvanceTo(new SimTime(WorldCalendar.MillisecondsPerHour * 13));

        var inspection = world.Inspect(new WorldPoint(510, 500), markerRadiusM: 50);

        Assert.True(inspection.InsideWorld);
        Assert.Equal((world.Now, 42UL, world.Extent), (inspection.At, inspection.WorldSeed, inspection.WorldExtent));
        Assert.Equal(FieldFamilies.All.Append(Atlas.FieldCatalog.Developer).Select(family => family.Key), inspection.Families.Select(family => family.Family.Key));
        var unavailable = inspection.Families.Where(family => family.Availability == FieldAvailability.Unavailable).ToList();
        Assert.Equal(15, unavailable.Count);
        Assert.All(unavailable.SelectMany(family => family.Readings), reading =>
        {
            Assert.Equal(ReadingBasis.Unavailable, reading.Basis);
            Assert.Null(reading.Value);
            Assert.Matches("implements .+ in Phase \\d\\d", reading.UnavailableReason!);
        });
        Assert.Equal(FieldAvailability.Available, inspection.Families[0].Availability);

        var readings = inspection.Families[0].Readings.ToDictionary(reading => reading.Quantity);
        Assert.Equal((510.0, "m", ReadingBasis.Query), (readings["easting x"].Value, readings["easting x"].Unit, readings["easting x"].Basis));
        var latitude = readings["latitude"];
        Assert.Equal(ReadingBasis.Derived, latitude.Basis);
        Assert.Equal("rad", latitude.Unit);
        Assert.Equal(45 * Math.PI / 180 + 500 / GeographicReference.MetresPerRadian, latitude.Value!.Value, 12);
        Assert.Equal("World foundation", latitude.Owner);
        Assert.NotNull(latitude.Derivation);
        Assert.Equal(world.Now, readings["local mean solar time"].UpdatedAt);
        var elevation = inspection.Families.Single(family => family.Family.Key == "bedrock_landform").Readings[0];
        Assert.Equal(("elevation above datum", ReadingBasis.Unavailable), (elevation.Quantity, elevation.Basis));
        Assert.Equal("near", Assert.Single(inspection.NearbyMarkers).Label);
    }

    [Theory]
    [InlineData(-0.001, 10)]
    [InlineData(10, 8_000)]
    [InlineData(10_000, 10)]
    public void InspectionOutsideTheFrameDerivesNothing(double x, double y)
    {
        var world = World.Create(TestSupport.Scenario(east: 10_000, north: 8_000), seed: 1);

        var inspection = world.Inspect(new WorldPoint(x, y));

        Assert.False(inspection.InsideWorld);
        Assert.All(inspection.Families[0].Readings.Where(reading => reading.Basis != ReadingBasis.Query),
            reading => Assert.Equal(ReadingBasis.Unavailable, reading.Basis));
    }

    [Fact]
    public void ChecksumDependsOnSeedTimeAndCommandsButNotWorldId()
    {
        static World Run(ulong seed, long until, bool marker, ulong id = 1)
        {
            var world = World.Create(TestSupport.Scenario(), seed, id: new WorldId(id));
            if (marker)
            {
                world.Submit(new PlaceMarker(new WorldPoint(1, 2), "m"));
            }

            world.AdvanceTo(new SimTime(until));
            return world;
        }

        var baseline = Run(1, 5_000, marker: true).ComputeChecksum();

        Assert.Equal(baseline, Run(1, 5_000, marker: true).ComputeChecksum());
        Assert.Equal(baseline, Run(1, 5_000, marker: true, id: 2).ComputeChecksum());
        Assert.NotEqual(baseline, Run(2, 5_000, marker: true).ComputeChecksum());
        Assert.NotEqual(baseline, Run(1, 6_000, marker: true).ComputeChecksum());
        Assert.NotEqual(baseline, Run(1, 5_000, marker: false).ComputeChecksum());
        Assert.Matches("^[0-9a-f]{64}$", baseline);
    }

    [Fact]
    public void ControlCommandsChangePacingButNeitherStateNorJournal()
    {
        var plain = World.Create(TestSupport.Scenario(), seed: 3);
        var controlled = World.Create(TestSupport.Scenario(), seed: 3);
        Assert.Null(controlled.Control(new Pause()));
        Assert.Null(controlled.Control(new SetSpeed(3_600_000)));
        Assert.Null(controlled.Control(new StepOnce()));
        Assert.Null(controlled.Control(new Resume()));
        Assert.NotNull(controlled.Control(new SetSpeed(0)));
        plain.Submit(new PlaceMarker(new WorldPoint(1, 1), "m"));
        controlled.Submit(new PlaceMarker(new WorldPoint(1, 1), "m"));

        Assert.Equal((false, 3_600_000L, 1), (controlled.TimeControl.Paused, controlled.TimeControl.SimMillisecondsPerRealSecond, controlled.TimeControl.RequestedSteps));
        Assert.Equal(plain.Journal.Entries, controlled.Journal.Entries);
        Assert.Equal(plain.ComputeChecksum(), controlled.ComputeChecksum());
        Assert.Throws<ArgumentException>(() => controlled.Submit(new Pause()));
    }

    [Fact]
    public void ARejectedCommandStillCountsAsAppliedInput()
    {
        // A rejected command takes a sequence number, so later causes differ; the checksum must see it (ADR-0007).
        var plain = World.Create(TestSupport.Scenario(), seed: 3);
        var withRejection = World.Create(TestSupport.Scenario(), seed: 3);
        withRejection.Submit(new RemoveMarker(new EntityId<Kind.Marker>(1)));

        Assert.NotEqual(plain.ComputeChecksum(), withRejection.ComputeChecksum());
    }

    [Fact]
    public void SpeedIsCappedAtOneYearPerSecond()
    {
        var world = World.Create(TestSupport.Scenario(), seed: 3);

        Assert.Null(world.Control(new SetSpeed(Kernel.TimeControl.MaxSimMillisecondsPerRealSecond)));
        Assert.NotNull(world.Control(new SetSpeed(Kernel.TimeControl.MaxSimMillisecondsPerRealSecond + 1)));
    }

    [Fact]
    public void CreationRejectsAnInvalidScenario()
    {
        Assert.Throws<ArgumentException>(() => World.Create(TestSupport.Scenario(stepMs: 0), seed: 1));
        Assert.Throws<ArgumentException>(() => World.Create(TestSupport.Scenario(stepMs: 7_000), seed: 1));
        Assert.Throws<ArgumentException>(() => World.Create(TestSupport.Scenario(southLatitudeDeg: 89), seed: 1));
    }
}
