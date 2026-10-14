using System.Globalization;
using Sovereigns.Client.View;
using Sovereigns.Simulation.Fields;

namespace Sovereigns.Client.Ui;

public enum LineStyle
{
    Normal,
    Heading,
    Dim,
    Warning,
}

public sealed record InspectorLine(string Text, LineStyle Style);

/// <summary>
/// Turns one <see cref="LocationInspection"/> into display lines (SOV-P02-T13). Readings are SI numbers; this class
/// converts them to display units (degrees, hh:mm:ss) and lists each reading's provenance: basis, owner, source,
/// derivation and when its inputs last changed. An unavailable reading shows its owner-and-phase reason in a
/// distinct style and never a value.
/// </summary>
public static class InspectorContent
{
    // The simulation reports local mean solar time in seconds after local midnight; the unit "s" alone is not enough to tell it from a duration.
    private const string SolarTimeQuantity = "local mean solar time";

    public const string NoSelection = "Click the map to select a location. Its values appear here, re-queried from the simulation.";

    public static IReadOnlyList<InspectorLine> Build(LocationInspection inspection, double markerRadiusM)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        var lines = new List<InspectorLine>
        {
            new($"Location {inspection.Point}", LineStyle.Heading),
            inspection.InsideWorld
                ? new("Inside the world frame.", LineStyle.Normal)
                : new("Outside the world frame: the simulation has no data here.", LineStyle.Warning),
            new("", LineStyle.Normal),
        };

        foreach (var family in inspection.Families)
        {
            var availability = family.Availability switch
            {
                FieldAvailability.Available => "available",
                FieldAvailability.Partial => "partly available",
                _ => "unavailable",
            };
            var unavailable = family.Availability == FieldAvailability.Unavailable;
            lines.Add(new($"{family.Family.Name} ({availability})", unavailable ? LineStyle.Dim : LineStyle.Heading));
            lines.Add(new($"  owner: {family.Family.Owner}; first implemented: {family.Family.FirstImplementedBy}", LineStyle.Dim));
            if (family.Family == FieldFamilies.GeographicReference)
            {
                lines.Add(new($"  world seed: {inspection.WorldSeed.ToString(CultureInfo.InvariantCulture)}", LineStyle.Normal));
                lines.Add(new(string.Create(CultureInfo.InvariantCulture,
                    $"  world extent: {inspection.WorldExtent.East:0.###} x {inspection.WorldExtent.North:0.###} m"), LineStyle.Normal));
            }

            foreach (var reading in family.Readings)
            {
                AddReading(lines, reading);
            }

            lines.Add(new("", LineStyle.Normal));
        }

        lines.Add(new($"Markers within {MapScale.FormatDistance(markerRadiusM)}", LineStyle.Heading));
        if (inspection.NearbyMarkers.Count == 0)
        {
            lines.Add(new("  none", LineStyle.Dim));
        }

        lines.AddRange(inspection.NearbyMarkers.Select(marker =>
            new InspectorLine($"  {marker.Id} \"{marker.Label}\" at {marker.Position}, placed {ClockText.Date(marker.PlacedAt)}", LineStyle.Normal)));
        return lines;
    }

    /// <summary>A reading's value in display units: degrees with four decimals for angles, hh:mm:ss for solar time, else the SI value with its unit.</summary>
    public static string FormatValue(FieldReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        if (reading.Value is not { } value)
        {
            return reading.UnavailableReason ?? "unavailable";
        }

        if (reading.Unit == "rad")
        {
            return string.Create(CultureInfo.InvariantCulture, $"{value * 180 / Math.PI:0.0000}°");
        }

        if (reading.Unit == "s" && reading.Quantity == SolarTimeQuantity)
        {
            var seconds = (long)Math.Floor(value);
            return string.Create(CultureInfo.InvariantCulture, $"{seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}");
        }

        return string.Create(CultureInfo.InvariantCulture, $"{value:0.###}{(reading.Unit is null ? "" : " " + reading.Unit)}");
    }

    private static void AddReading(List<InspectorLine> lines, FieldReading reading)
    {
        if (reading.Value is null)
        {
            lines.Add(new($"  {reading.Quantity}: {FormatValue(reading)}", LineStyle.Dim));
            return;
        }

        lines.Add(new($"  {reading.Quantity}: {FormatValue(reading)}", LineStyle.Normal));
        lines.Add(new($"      basis: {reading.Basis.ToString().ToLowerInvariant()}", LineStyle.Dim));
        AddProvenance(lines, "owner", reading.Owner);
        AddProvenance(lines, "source", reading.Source);
        AddProvenance(lines, "derivation", reading.Derivation);
        AddProvenance(lines, "updated", reading.UpdatedAt is { } updated ? ClockText.Date(updated) : null);
    }

    private static void AddProvenance(List<InspectorLine> lines, string label, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            lines.Add(new($"      {label}: {value}", LineStyle.Dim));
        }
    }
}
