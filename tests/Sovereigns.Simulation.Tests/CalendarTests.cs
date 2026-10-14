using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Tests;

/// <summary>SOV-P02-T02: the world calendar and chronology conventions (ADR-0006).</summary>
public sealed class CalendarTests
{
    [Fact]
    public void EpochIsTheFirstOfJanuaryOfYearOne() =>
        Assert.Equal("0001-01-01T00:00:00.000", WorldCalendar.ToCalendar(SimTime.Zero).ToString());

    [Theory]
    [InlineData(1, 1, 1, 0, 0)]
    [InlineData(1203, 3, 1, 6, 0)]
    [InlineData(1202, 2, 28, 23, 59)]
    [InlineData(1202, 12, 31, 12, 30)]
    [InlineData(0, 12, 31, 23, 59)]
    [InlineData(-500, 7, 4, 3, 2)]
    public void CalendarPositionsRoundTrip(long year, int month, int day, int hour, int minute)
    {
        var time = WorldCalendar.FromCalendar(year, month, day, hour, minute, 7, 9);
        var date = WorldCalendar.ToCalendar(time);

        Assert.Equal((year, month, day, hour, minute, 7, 9), (date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Millisecond));
    }

    [Fact]
    public void YearsHave365DaysAndNoLeapDay()
    {
        Assert.Equal(365 * WorldCalendar.MillisecondsPerDay,
            WorldCalendar.FromCalendar(1204, 1, 1).Milliseconds - WorldCalendar.FromCalendar(1203, 1, 1).Milliseconds);
        var afterFebruary28 = WorldCalendar.ToCalendar(WorldCalendar.FromCalendar(1204, 2, 28).Plus(WorldCalendar.MillisecondsPerDay));
        Assert.Equal((3, 1, 60), (afterFebruary28.Month, afterFebruary28.Day, afterFebruary28.DayOfYear));
        var newYear = WorldCalendar.ToCalendar(WorldCalendar.FromCalendar(1203, 12, 31, 23, 59, 59, 999).Plus(1));
        Assert.Equal((1204L, 1, 1, 1), (newYear.Year, newYear.Month, newYear.Day, newYear.DayOfYear));
    }

    [Fact]
    public void TimesBeforeTheEpochFallInYearZeroAndEarlier()
    {
        var justBefore = WorldCalendar.ToCalendar(new SimTime(-1));

        Assert.Equal("0000-12-31T23:59:59.999", justBefore.ToString());
        Assert.Equal(WorldCalendar.FromCalendar(0, 12, 31), WorldCalendar.StartOfDay(new SimTime(-1)));
    }

    [Theory]
    [InlineData(2, 29)]
    [InlineData(4, 31)]
    [InlineData(13, 1)]
    [InlineData(0, 1)]
    public void ImpossibleDatesAreRejected(int month, int day) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => WorldCalendar.FromCalendar(1203, month, day));

    [Fact]
    public void SeasonsMirrorBetweenHemispheresAndStartOnTheFirstOfDecemberMarchJuneAndSeptember()
    {
        Assert.Equal(Season.Winter, WorldCalendar.SeasonOf(1, Hemisphere.Northern));
        Assert.Equal(Season.Summer, WorldCalendar.SeasonOf(1, Hemisphere.Southern));
        Assert.Equal(Season.Spring, WorldCalendar.SeasonOf(3, Hemisphere.Northern));
        Assert.Equal(Season.Autumn, WorldCalendar.SeasonOf(3, Hemisphere.Southern));
        var starts = Enumerable.Range(0, 365)
            .Select(day => WorldCalendar.ToCalendar(WorldCalendar.FromCalendar(1203, 1, 1).Plus(day * WorldCalendar.MillisecondsPerDay)))
            .Where(WorldCalendar.IsSeasonStart)
            .Select(date => date.Month);
        Assert.Equal([3, 6, 9, 12], starts);
    }

    [Fact]
    public void SeasonsLastAboutThirteenSevenDayTurns()
    {
        // Spec §2.3: one turn is seven days and seasons last about 13 turns.
        var days = Enumerable.Range(0, WorldCalendar.DaysPerYear)
            .Select(day => WorldCalendar.ToCalendar(WorldCalendar.FromCalendar(1203, 1, 1).Plus(day * WorldCalendar.MillisecondsPerDay)))
            .GroupBy(date => WorldCalendar.SeasonOf(date.Month, Hemisphere.Northern))
            .ToDictionary(group => group.Key, group => group.Count());

        Assert.Equal(new Dictionary<Season, int> { [Season.Winter] = 90, [Season.Spring] = 92, [Season.Summer] = 92, [Season.Autumn] = 91 }, days);
        Assert.All(days.Values, count => Assert.InRange(count / (double)WorldCalendar.DaysPerWeek, 12.8, 13.2));
    }

    [Fact]
    public void AWorldCrossesFromNegativeYearsIntoYearOne()
    {
        var world = World.Create(TestSupport.Scenario(start: new Scenarios.CalendarStart(-1, 12, 31, 12)), seed: 1);

        world.AdvanceTo(WorldCalendar.FromCalendar(1, 1, 2));

        Assert.Equal(["0", "1"], world.Ledger.Events.Where(e => e.Type == Events.WorldEventType.YearBegan).Select(e => e.Data!["year"]));
        Assert.True(world.Identity.Start.Milliseconds < 0);
    }
}
