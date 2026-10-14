using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Identity;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Tests;

/// <summary>SOV-P02-T01: the immutable geographic coordinate contract (ADR-0006).</summary>
public sealed class GeographyTests
{
    private static readonly WorldExtent Extent = new(100_000, 100_000);
    private static readonly GeographicReference Geography = GeographicReference.FromDegrees(47.5, 50_000);

    [Fact]
    public void LatitudeGrowsNorthwardByOneRadianPerEarthRadius()
    {
        Assert.Equal(47.5 * Math.PI / 180, Geography.LatitudeAt(0), 15);
        Assert.Equal(47.5 * Math.PI / 180 + 100_000 / GeographicReference.MetresPerRadian, Geography.NorthEdgeLatitudeRad(Extent), 15);
        Assert.Equal(Hemisphere.Northern, Geography.HemisphereAt(0));
        Assert.Equal(Hemisphere.Southern, GeographicReference.FromDegrees(-30, 0).HemisphereAt(0));
    }

    [Fact]
    public void LocalSolarTimeShiftsWithEastWestDistance()
    {
        var noon = WorldCalendar.FromCalendar(1203, 6, 1, 12);
        var atMeridian = Geography.LocalSolarTimeOfDayMs(noon, 50_000, Extent);
        var east = Geography.LocalSolarTimeOfDayMs(noon, 51_000, Extent);
        var west = Geography.LocalSolarTimeOfDayMs(noon, 49_000, Extent);
        var expectedShiftMs = 1_000 / (GeographicReference.MetresPerRadian * Math.Cos(Geography.ScaleLatitudeRad(Extent))) / (2 * Math.PI) * 86_400_000;

        Assert.Equal(12 * WorldCalendar.MillisecondsPerHour, atMeridian);
        Assert.Equal(Math.Round(expectedShiftMs), east - atMeridian);
        Assert.Equal(Math.Round(expectedShiftMs), atMeridian - west);
        // About 0.0134° of longitude per km at 48°N; 240 s of solar time per degree.
        Assert.InRange(expectedShiftMs, 3_000, 3_500);
    }

    [Fact]
    public void LocalSolarTimeWrapsAroundMidnight()
    {
        var midnight = WorldCalendar.FromCalendar(1203, 6, 1);

        Assert.InRange(Geography.LocalSolarTimeOfDayMs(midnight, 0, Extent), 23 * WorldCalendar.MillisecondsPerHour, WorldCalendar.MillisecondsPerDay - 1);
    }

    [Theory]
    [InlineData(84, 50_000, true)]
    [InlineData(86, 50_000, false)]
    [InlineData(-85.5, 50_000, false)]
    [InlineData(47.5, 100_001, false)]
    [InlineData(47.5, -1, false)]
    public void PlacementMustStayWithinLatitudeLimitsAndTheWorld(double southEdgeDeg, double meridianX, bool valid) =>
        Assert.Equal(valid, !GeographicReference.FromDegrees(southEdgeDeg, meridianX).Validate(new WorldExtent(100_000, 10_000)).Any());
}
