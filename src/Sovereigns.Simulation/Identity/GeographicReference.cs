using Sovereigns.Simulation.Geometry;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Identity;

/// <summary>
/// Immutable mapping from the planar world frame to geography (SOV-P02-T01, ADR-0006). Latitude grows northward
/// from <see cref="SouthEdgeLatitudeRad"/> at y = 0 by one radian per <see cref="MetresPerRadian"/>. World standard
/// time is the mean solar time at x = <see cref="StandardMeridianX"/>; local mean solar time shifts by the
/// east–west distance converted to longitude at the world's mid latitude. Only the two authored inputs are stored;
/// every transcendental result is a derived query, never authoritative state.
/// </summary>
public sealed record GeographicReference(double SouthEdgeLatitudeRad, double StandardMeridianX)
{
    /// <summary>Earth's mean radius (IUGG), the sphere this planar frame approximates.</summary>
    public const double MetresPerRadian = 6_371_000;

    /// <summary>Largest allowed |latitude| anywhere in the world, 85°, to keep east–west scaling finite.</summary>
    public const double MaxAbsoluteLatitudeRad = 85 * Math.PI / 180;

    public static GeographicReference FromDegrees(double southEdgeLatitudeDeg, double standardMeridianX) =>
        new(southEdgeLatitudeDeg * Math.PI / 180, standardMeridianX);

    public IEnumerable<string> Validate(WorldExtent extent)
    {
        if (!double.IsFinite(SouthEdgeLatitudeRad) || !double.IsFinite(StandardMeridianX))
        {
            yield return "geographic reference values must be finite";
            yield break;
        }

        var north = NorthEdgeLatitudeRad(extent);
        if (Math.Abs(SouthEdgeLatitudeRad) > MaxAbsoluteLatitudeRad || Math.Abs(north) > MaxAbsoluteLatitudeRad)
        {
            yield return $"the world spans latitudes {SouthEdgeLatitudeRad * 180 / Math.PI:0.##}° to {north * 180 / Math.PI:0.##}°, beyond ±85°";
        }

        if (StandardMeridianX < 0 || StandardMeridianX > extent.East)
        {
            yield return "the standard meridian must lie inside the world, 0 <= x <= east extent";
        }
    }

    public double LatitudeAt(double y) => SouthEdgeLatitudeRad + y / MetresPerRadian;

    public double NorthEdgeLatitudeRad(WorldExtent extent) => LatitudeAt(extent.North);

    public Hemisphere HemisphereAt(double y) => LatitudeAt(y) >= 0 ? Hemisphere.Northern : Hemisphere.Southern;

    /// <summary>Latitude at the middle of the world, used to convert east–west distance to longitude.</summary>
    public double ScaleLatitudeRad(WorldExtent extent) => LatitudeAt(extent.North / 2);

    /// <summary>Longitude east of the standard meridian, in radians.</summary>
    public double LongitudeOffsetRad(double x, WorldExtent extent) =>
        (x - StandardMeridianX) / (MetresPerRadian * Math.Cos(ScaleLatitudeRad(extent)));

    /// <summary>Local mean solar time of day at <paramref name="x"/>, in milliseconds after local midnight.</summary>
    public long LocalSolarTimeOfDayMs(SimTime time, double x, WorldExtent extent)
    {
        var offset = (long)Math.Round(LongitudeOffsetRad(x, extent) / (2 * Math.PI) * WorldCalendar.MillisecondsPerDay);
        var local = (time.Milliseconds + offset) % WorldCalendar.MillisecondsPerDay;
        return local < 0 ? local + WorldCalendar.MillisecondsPerDay : local;
    }
}
