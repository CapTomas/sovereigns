using System.Globalization;
using Sovereigns.Simulation.Events;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Kernel;

/// <summary>
/// Records calendar boundaries in the event ledger at each standard-time midnight: a new year, and a new season in
/// the world's mid-latitude hemisphere. It owns no state; it is the first scheduled system and the reference
/// daily cadence.
/// </summary>
internal sealed class CalendarSystem : ISimulationSystem
{
    public string Id => "calendar";

    public int Priority => SystemPriorities.Calendar;

    public long CadenceMs => WorldCalendar.MillisecondsPerDay;

    public long PhaseMs => 0;

    public IReadOnlyList<string> OwnedComponents => [];

    public void Update(SystemContext context)
    {
        var date = WorldCalendar.ToCalendar(context.Now);
        if (date.DayOfYear == 1)
        {
            context.Record(WorldEventType.YearBegan, [], new Dictionary<string, string>
            {
                ["year"] = date.Year.ToString(CultureInfo.InvariantCulture),
            });
        }

        if (WorldCalendar.IsSeasonStart(date))
        {
            var world = context.World;
            var hemisphere = world.Identity.Geography.HemisphereAt(world.Identity.Extent.North / 2);
            context.Record(WorldEventType.SeasonBegan, [], new Dictionary<string, string>
            {
                ["season"] = WorldCalendar.SeasonOf(date.Month, hemisphere).ToString(),
                ["hemisphere"] = hemisphere.ToString(),
            });
        }
    }
}

/// <summary>
/// Order of systems due at the same instant (ADR-0007): physical causes before their consumers, following spec
/// §29.9. Gaps leave room for systems added by later phases.
/// </summary>
public static class SystemPriorities
{
    public const int Calendar = 0;
    public const int Atmosphere = 100;
    public const int Radiation = 150;
    public const int SurfaceWater = 200;
    public const int Hydrology = 300;
    public const int Ecology = 400;
    public const int Agriculture = 500;
    public const int Economy = 600;
    public const int Politics = 700;
    public const int Military = 800;
}
