using System.Globalization;
using Sovereigns.Simulation.Kernel;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Client.Ui;

/// <summary>
/// Player-facing text for simulation time. The simulation reports numbers and calendar fields only; month names,
/// season names and speed labels are display data and belong to the client (ADR-0004 §4).
/// </summary>
public static class ClockText
{
    private static readonly string[] MonthNames = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

    private static readonly string[] PresetLabels = ["1 s/s", "1 min/s", "1 h/s", "1 day/s", "1 week/s"];

    /// <summary>World standard date and time, for example <c>1 Mar 1203, 06:00:00</c>; milliseconds appear only when not zero.</summary>
    public static string Date(SimTime time)
    {
        var date = WorldCalendar.ToCalendar(time);
        var text = string.Create(CultureInfo.InvariantCulture,
            $"{date.Day} {MonthNames[date.Month - 1]} {date.Year}, {date.Hour:00}:{date.Minute:00}:{date.Second:00}");
        return date.Millisecond == 0 ? text : string.Create(CultureInfo.InvariantCulture, $"{text}.{date.Millisecond:000}");
    }

    /// <summary>The pace as simulated time per real second, for example <c>1 day/s</c>.</summary>
    public static string Speed(long simMillisecondsPerRealSecond)
    {
        var preset = TimeControl.Presets.ToList().IndexOf(simMillisecondsPerRealSecond);
        return preset >= 0 && preset < PresetLabels.Length
            ? PresetLabels[preset]
            : string.Create(CultureInfo.InvariantCulture, $"{simMillisecondsPerRealSecond / 1000.0:0.###} s/s");
    }

    /// <summary>The season of the date's month in the given hemisphere, for example <c>Spring (northern hemisphere)</c>.</summary>
    public static string Season(SimTime time, Hemisphere hemisphere)
    {
        var season = WorldCalendar.SeasonOf(WorldCalendar.ToCalendar(time).Month, hemisphere);
        return string.Create(CultureInfo.InvariantCulture, $"{season} ({hemisphere.ToString().ToLowerInvariant()} hemisphere)");
    }
}
