using Sovereigns.Client.Ui;
using Sovereigns.Simulation.Commands;
using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Kernel;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Client.Tests;

/// <summary>What the HUD, inspector and events panel say about the world, without a scene (SOV-P02-T11, T13).</summary>
public static class PanelTests
{
    public static IEnumerable<TestCase> Cases() =>
    [
        new("panels: clock text gives standard date and time, season, hemisphere and speed labels", ClockTextFormats),
        new("inspector: latitude and solar time show display units and provenance lines; unavailable readings stay grey", InspectorShowsProvenance),
        new("events: the panel lists the 12 most recent ledger events, newest first, with time, type, subjects and cause", EventsListRecentLedger),
    ];

    private static Task ClockTextFormats()
    {
        Check.Equal("1 Mar 1203, 06:00:00", ClockText.Date(WorldCalendar.FromCalendar(1203, 3, 1, 6)), "standard date and time");
        Check.Equal("31 Dec 1202, 23:59:59.500", ClockText.Date(WorldCalendar.FromCalendar(1202, 12, 31, 23, 59, 59, 500)), "milliseconds appear only when not zero");
        Check.Equal("Spring (northern hemisphere)", ClockText.Season(WorldCalendar.FromCalendar(1203, 3, 1), Hemisphere.Northern), "March in the north");
        Check.Equal("Autumn (southern hemisphere)", ClockText.Season(WorldCalendar.FromCalendar(1203, 3, 1), Hemisphere.Southern), "March in the south");
        var labels = TimeControl.Presets.Select(ClockText.Speed).ToList();
        Check.Equal("1 s/s,1 min/s,1 h/s,1 day/s,1 week/s", string.Join(",", labels), "preset labels");
        Check.Equal("2.5 s/s", ClockText.Speed(2500), "a speed outside the presets is still labelled");
        return Task.CompletedTask;
    }

    private static Task InspectorShowsProvenance()
    {
        var world = TestWorlds.LaunchFixture().World!;
        world.AdvanceTo(world.Now.Plus(3_600_000));
        var lines = InspectorContent.Build(world.Inspect(new WorldPoint(50_000, 50_000)), 100);
        var text = lines.Select(line => line.Text).ToList();

        // F-EMPTY starts 1 Mar 1203 06:00; the point is on the standard meridian, so local solar time equals standard time.
        var latitude = text.IndexOf("  latitude: 47.9497°");
        Check.True(latitude >= 0, $"latitude shows degrees with four decimals:\n{string.Join('\n', text)}");
        Check.Equal("      level: regional (256 m)", text[latitude + 1], "latitude level");
        Check.Contains(text[latitude + 2], "classification: analytic: φ = φ₀ + y", "latitude classification");
        Check.Equal("      basis: derived", text[latitude + 3], "latitude basis");
        Check.Equal("      owner: World foundation", text[latitude + 4], "latitude owner");
        Check.Equal("      source: geographic reference: south-edge latitude", text[latitude + 5], "latitude source");
        Check.Equal("      updated: 1 Mar 1203, 06:00:00", text[latitude + 6], "latitude was last derived from the start");
        Check.Equal("      inputs: none (computed from position and time)", text[latitude + 7], "an analytic reading has no grid inputs");

        var solar = text.IndexOf("  local mean solar time: 07:00:00");
        Check.True(solar >= 0, "solar time shows hh:mm:ss");
        Check.Equal("      level: regional (256 m)", text[solar + 1], "solar time level");
        Check.Contains(text[solar + 2], "classification: analytic: standard time", "solar time classification");
        Check.Equal("      basis: derived", text[solar + 3], "solar time basis");
        Check.Equal("      owner: Simulation scheduler", text[solar + 4], "solar time owner");
        Check.Equal("      source: world clock; geographic reference: standard meridian", text[solar + 5], "solar time source");
        Check.Equal("      updated: 1 Mar 1203, 07:00:00", text[solar + 6], "solar time updated at the queried time");

        var easting = text.IndexOf("  easting x: 50000 m");
        Check.True(easting >= 0 && text[easting + 1] == "      basis: query" && text[easting + 2] == "      source: inspected position", "a queried coordinate states its basis and source");
        Check.True(!text[easting + 3].Contains("owner", StringComparison.Ordinal), "a reading without an owner shows no owner line");
        Check.True(text.Contains("  world seed: 20261009") && text.Any(line => line.StartsWith("  world extent: 100000 x 100000 m", StringComparison.Ordinal)), "seed and extent come from the inspection");

        Check.True(text.IndexOf("Developer tools (available)") is >= 0 and var developer && developer < text.IndexOf("Bedrock and landform (unavailable)"), "families with data come before unavailable ones, even the last in the catalogue");
        var elevation = lines.Single(line => line.Text.StartsWith("  elevation above datum:", StringComparison.Ordinal));
        Check.True(elevation.Style == LineStyle.Dim && elevation.Text.Contains("Phase 04", StringComparison.Ordinal), "an unavailable reading is grey and names its phase");

        var west = InspectorContent.Build(world.Inspect(new WorldPoint(0, 50_000)), 100).Select(line => line.Text).ToList();
        Check.True(west.Contains("  local mean solar time: 06:57:18"), "solar time west of the standard meridian is earlier (07:00:00 - 161.125 s)");

        var outside = InspectorContent.Build(world.Inspect(new WorldPoint(-5, 10)), 100);
        Check.True(outside.Any(line => line.Style == LineStyle.Dim && line.Text.StartsWith("  latitude: unavailable", StringComparison.Ordinal)), "outside the frame latitude is unavailable and grey");
        return Task.CompletedTask;
    }

    private static Task EventsListRecentLedger()
    {
        var world = TestWorlds.LaunchFixture(fixture: "F-KERNEL").World!;
        var created = EventsContent.Build(world.Ledger);
        Check.Equal(2, created.Count, "a new world has one event, shown on two lines");
        Check.Equal("15 Nov 1202, 06:00:00  WorldCreated", created[0].Text, "time and type");
        Check.Contains(created[1].Text, "cause: origin", "cause");

        world.AdvanceTo(world.Now.Plus(20 * WorldCalendar.MillisecondsPerDay));
        var season = EventsContent.Build(world.Ledger);
        Check.Equal("1 Dec 1202, 00:00:00  SeasonBegan", season[0].Text, "the newest event is the start of winter");
        Check.Contains(season[1].Text, "season=Winter", "event data");
        Check.Contains(season[1].Text, "cause: system calendar", "event cause");

        for (var i = 0; i < 14; i++)
        {
            world.Submit(new PlaceMarker(new WorldPoint(1000 + i, 2000), $"E{i}"));
        }

        var recent = EventsContent.Build(world.Ledger);
        Check.Equal(EventsContent.RecentCount * 2, recent.Count, "twelve events on two lines each");
        Check.Contains(recent[0].Text, "MarkerPlaced", "newest event first");
        Check.Contains(recent[1].Text, $"[{world.Markers[^1].Id}]", "subjects");
        Check.Contains(recent[1].Text, $"cause: command #{world.Journal.Entries[^1].Sequence}", "the command that caused it");
        Check.True(recent.All(line => !line.Text.Contains("WorldCreated", StringComparison.Ordinal) && !line.Text.Contains("SeasonBegan", StringComparison.Ordinal)), "older events are not listed");
        Check.Equal("Recent events (12 of 16)", EventsContent.Title(world.Ledger), "title counts the ledger");
        return Task.CompletedTask;
    }
}
