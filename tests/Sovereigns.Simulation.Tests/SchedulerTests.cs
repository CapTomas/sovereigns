using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Events;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Kernel;
using Sovereigns.Simulation.Scenarios;
using Sovereigns.Simulation.State;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Tests;

/// <summary>SOV-P02-T03, T04, T14: one clock, cadence and priority order, time-step ownership (ADR-0007).</summary>
public sealed class SchedulerTests
{
    private const long Day = WorldCalendar.MillisecondsPerDay;

    [Fact]
    public void DueSystemsRunInPriorityThenNameOrder()
    {
        var scheduler = new Scheduler();
        var ownership = new OwnershipRegistry();
        scheduler.Register(new TestSystem("water", 300, 2_000), 1_000, ownership);
        scheduler.Register(new TestSystem("air", 100, 3_000), 1_000, ownership);
        scheduler.Register(new TestSystem("aaa", 300, 6_000), 1_000, ownership);

        Assert.Equal(["air", "aaa", "water"], scheduler.DueAt(new SimTime(6_000)).Select(system => system.Id));
        Assert.Equal(["water"], scheduler.DueAt(new SimTime(4_000)).Select(system => system.Id));
        Assert.Empty(scheduler.DueAt(new SimTime(1_000)));
        Assert.Equal(new SimTime(2_000), scheduler.NextDueAfter(SimTime.Zero));
        Assert.Equal(new SimTime(8_000), scheduler.NextDueAfter(new SimTime(6_000)));
        Assert.Equal(new SimTime(-4_000), scheduler.NextDueAfter(new SimTime(-5_000)));
    }

    [Fact]
    public void PhaseOffsetsTheCadence()
    {
        var scheduler = new Scheduler();
        scheduler.Register(new TestSystem("noon", 0, Day, 12 * WorldCalendar.MillisecondsPerHour), 1_000, new OwnershipRegistry());

        Assert.Equal(new SimTime(12 * WorldCalendar.MillisecondsPerHour), scheduler.NextDueAfter(SimTime.Zero));
        Assert.Single(scheduler.DueAt(new SimTime(Day + 12 * WorldCalendar.MillisecondsPerHour)));
    }

    [Theory]
    [InlineData(1_500, 0, "cadence")]
    [InlineData(0, 0, "cadence")]
    [InlineData(2_000, 2_000, "phase")]
    [InlineData(2_000, 500, "phase")]
    public void InvalidCadencesAreRejected(long cadence, long phase, string expected)
    {
        var error = Assert.Throws<ArgumentException>(() =>
            new Scheduler().Register(new TestSystem("bad", 0, cadence, phase), 1_000, new OwnershipRegistry()));

        Assert.Contains(expected, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TwoOwnersForOneComponentAreRejected()
    {
        var scheduler = new Scheduler();
        var ownership = new OwnershipRegistry();
        scheduler.Register(new TestSystem("rivers", 300, 1_000, owns: "surface_water"), 1_000, ownership);

        var duplicate = Assert.Throws<DuplicateOwnerException>(() =>
            scheduler.Register(new TestSystem("floods", 310, 1_000, owns: "surface_water"), 1_000, ownership));
        Assert.Contains("'surface_water' already has owner 'rivers'", duplicate.Message, StringComparison.Ordinal);
        Assert.Throws<DuplicateOwnerException>(() => scheduler.Register(new TestSystem("rivers", 1, 1_000), 1_000, ownership));
    }

    [Fact]
    public void WorldComponentsHaveExactlyOneOwnerEach()
    {
        var world = World.Create(TestSupport.Scenario(), seed: 1);

        Assert.Equal(World.KernelOwner, world.Ownership.OwnerOf("clock"));
        Assert.Equal(World.DeveloperOwner, world.Ownership.OwnerOf("markers"));
        Assert.Throws<DuplicateOwnerException>(() => world.Ownership.Register("ledger", "intruder"));
    }

    [Fact]
    public void DailyCalendarUpdatesRunOncePerDayHoweverTimeIsAdvanced()
    {
        // Spec §30.2: daily systems neither skip nor double-run, whatever the advancement steps.
        var start = new CalendarStart(1202, 11, 15, 6);
        var oneCall = World.Create(TestSupport.Scenario(start: start), seed: 2);
        var daily = World.Create(TestSupport.Scenario(start: start), seed: 2);
        var irregular = World.Create(TestSupport.Scenario(start: start), seed: 2);
        var end = oneCall.Now.Plus(400 * Day);

        oneCall.AdvanceTo(end);
        while (daily.Now < end)
        {
            daily.AdvanceTo(daily.Now.Plus(Day));
        }

        var chunk = 1L;
        while (irregular.Now < end)
        {
            irregular.AdvanceTo(irregular.Now.Plus(Math.Min(chunk, (end.Milliseconds - irregular.Now.Milliseconds) / 1_000) * 1_000));
            chunk = chunk * 7 % 100_003 + 1;
        }

        var seasons = oneCall.Ledger.Events.Where(e => e.Type == WorldEventType.SeasonBegan).Select(e => e.Data!["season"]).ToList();
        Assert.Equal(["Winter", "Spring", "Summer", "Autumn", "Winter"], seasons);
        Assert.Single(oneCall.Ledger.Events, e => e.Type == WorldEventType.YearBegan && e.Data!["year"] == "1203");
        Assert.Equal(oneCall.ComputeChecksum(), daily.ComputeChecksum());
        Assert.Equal(oneCall.ComputeChecksum(), irregular.ComputeChecksum());
    }

    [Fact]
    public void AnUpdateCoversTheIntervalSinceItsPreviousDueTimeOrTheStart()
    {
        // ADR-0007: a daily system in a world that starts at 06:00 first integrates [06:00, midnight).
        var world = World.Create(TestSupport.Scenario(start: new CalendarStart(1203, 3, 1, 6)), seed: 1);
        var daily = new TestSystem("daily", 0, Day);
        var firstMidnight = WorldCalendar.FromCalendar(1203, 3, 2);

        world.AdvanceTo(firstMidnight);
        var first = new SystemContext(world, daily, (_, _, _) => { }).Since;
        world.AdvanceTo(firstMidnight.Plus(Day));
        var second = new SystemContext(world, daily, (_, _, _) => { }).Since;

        Assert.Equal(world.Identity.Start, first);
        Assert.Equal(firstMidnight, second);
    }

    [Fact]
    public void CommandsAtAnInstantPrecedeThatInstantsUpdates()
    {
        var world = World.Create(TestSupport.Scenario(), seed: 1);

        world.Submit(new PlaceMarker(new WorldPoint(1, 1), "before the new-year update"));
        world.Step();

        Assert.Equal([WorldEventType.WorldCreated, WorldEventType.MarkerPlaced, WorldEventType.YearBegan],
            world.Ledger.Events.Select(e => e.Type));
        Assert.Equal(CauseKind.System, world.Ledger.Events[2].Cause.Kind);
        Assert.Equal("calendar", world.Ledger.Events[2].Cause.System);
    }

    private sealed class TestSystem(string id, int priority, long cadenceMs, long phaseMs = 0, string? owns = null) : ISimulationSystem
    {
        public string Id => id;

        public int Priority => priority;

        public long CadenceMs => cadenceMs;

        public long PhaseMs => phaseMs;

        public IReadOnlyList<string> OwnedComponents => owns is null ? [] : [owns];

        public void Update(SystemContext context)
        {
        }
    }
}
