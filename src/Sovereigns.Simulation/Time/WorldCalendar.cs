using System.Globalization;

namespace Sovereigns.Simulation.Time;

public enum Season
{
    Winter,
    Spring,
    Summer,
    Autumn,
}

public enum Hemisphere
{
    Northern,
    Southern,
}

/// <summary>A calendar position in world standard time. Years use astronomical numbering: the year before 1 is 0.</summary>
public readonly record struct CalendarDateTime(long Year, int Month, int Day, int DayOfYear, int Hour, int Minute, int Second, int Millisecond)
{
    /// <summary>Canonical, culture-invariant text for logs and files, for example <c>1203-04-01T06:00:00.000</c>; not player display.</summary>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Year:0000}-{Month:00}-{Day:00}T{Hour:00}:{Minute:00}:{Second:00}.{Millisecond:000}");
}

/// <summary>
/// The world calendar (SOV-P02-T02, ADR-0006): 365-day years of twelve Gregorian common-year months, no leap days,
/// 24-hour days of 86 400 000 ms. The epoch, <see cref="SimTime.Zero"/>, is 1 January of year 1 at 00:00 world
/// standard time, the mean solar time of the world's standard meridian. Earlier times are negative.
/// </summary>
public static class WorldCalendar
{
    public const long MillisecondsPerSecond = 1_000;
    public const long MillisecondsPerMinute = 60_000;
    public const long MillisecondsPerHour = 3_600_000;
    public const long MillisecondsPerDay = 86_400_000;
    public const int DaysPerYear = 365;
    public const int DaysPerWeek = 7;
    public const long MillisecondsPerYear = MillisecondsPerDay * DaysPerYear;

    private static readonly int[] MonthLengths = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
    private static readonly int[] MonthStarts = BuildMonthStarts();

    public static CalendarDateTime ToCalendar(SimTime time)
    {
        var yearIndex = FloorDiv(time.Milliseconds, MillisecondsPerYear);
        var inYear = time.Milliseconds - yearIndex * MillisecondsPerYear;
        var dayOfYear = (int)(inYear / MillisecondsPerDay);
        var inDay = inYear % MillisecondsPerDay;
        var month = Array.FindLastIndex(MonthStarts, start => start <= dayOfYear);
        return new CalendarDateTime(
            yearIndex + 1,
            month + 1,
            dayOfYear - MonthStarts[month] + 1,
            dayOfYear + 1,
            (int)(inDay / MillisecondsPerHour),
            (int)(inDay / MillisecondsPerMinute % 60),
            (int)(inDay / MillisecondsPerSecond % 60),
            (int)(inDay % MillisecondsPerSecond));
    }

    /// <summary>The time of a calendar position; rejects positions that do not exist, such as 30 February.</summary>
    public static SimTime FromCalendar(long year, int month, int day, int hour = 0, int minute = 0, int second = 0, int millisecond = 0)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), month, "month must be 1 to 12");
        }

        if (day < 1 || day > MonthLengths[month - 1])
        {
            throw new ArgumentOutOfRangeException(nameof(day), day, $"month {month} has days 1 to {MonthLengths[month - 1]}");
        }

        if (hour is < 0 or > 23 || minute is < 0 or > 59 || second is < 0 or > 59 || millisecond is < 0 or > 999)
        {
            throw new ArgumentOutOfRangeException(nameof(hour), "time of day must be 00:00:00.000 to 23:59:59.999");
        }

        var days = checked((year - 1) * DaysPerYear + MonthStarts[month - 1] + day - 1);
        return new SimTime(checked(days * MillisecondsPerDay + hour * MillisecondsPerHour + minute * MillisecondsPerMinute +
                                   second * MillisecondsPerSecond + millisecond));
    }

    public static int DaysInMonth(int month) =>
        month is >= 1 and <= 12 ? MonthLengths[month - 1] : throw new ArgumentOutOfRangeException(nameof(month));

    /// <summary>Start of the standard-time day containing <paramref name="time"/>.</summary>
    public static SimTime StartOfDay(SimTime time) => new(FloorDiv(time.Milliseconds, MillisecondsPerDay) * MillisecondsPerDay);

    /// <summary>
    /// Meteorological season of a month (December–February is northern winter), mirrored in the southern
    /// hemisphere. Seasons are calendar conventions for scheduling and display; physical forcing comes from solar
    /// geometry (Phase 06).
    /// </summary>
    public static Season SeasonOf(int month, Hemisphere hemisphere)
    {
        var northern = month switch
        {
            12 or 1 or 2 => Season.Winter,
            3 or 4 or 5 => Season.Spring,
            6 or 7 or 8 => Season.Summer,
            9 or 10 or 11 => Season.Autumn,
            _ => throw new ArgumentOutOfRangeException(nameof(month)),
        };
        return hemisphere == Hemisphere.Northern ? northern : (Season)(((int)northern + 2) % 4);
    }

    /// <summary>True on the first day of December, March, June and September.</summary>
    public static bool IsSeasonStart(CalendarDateTime date) => date.Day == 1 && date.Month % 3 == 0;

    private static long FloorDiv(long value, long divisor) =>
        value >= 0 ? value / divisor : -((-(value + 1) / divisor) + 1);

    private static int[] BuildMonthStarts()
    {
        var starts = new int[12];
        for (var i = 1; i < 12; i++)
        {
            starts[i] = starts[i - 1] + MonthLengths[i - 1];
        }

        return starts;
    }
}
