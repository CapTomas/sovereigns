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
/// Turns one <see cref="LocationInspection"/> into display lines. Values and units come from the readings as
/// reported; an unavailable reading shows its owner-and-phase reason in a distinct style and never a value.
/// </summary>
public static class InspectorContent
{
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

            lines.AddRange(family.Readings.Select(reading => reading.Value is { } value
                ? new InspectorLine(string.Create(CultureInfo.InvariantCulture,
                    $"  {reading.Quantity}: {value:0.###}{(reading.Unit is null ? "" : " " + reading.Unit)}"), LineStyle.Normal)
                : new InspectorLine($"  {reading.Quantity}: {reading.UnavailableReason}", LineStyle.Dim)));
            lines.Add(new("", LineStyle.Normal));
        }

        lines.Add(new($"Markers within {MapScale.FormatDistance(markerRadiusM)}", LineStyle.Heading));
        if (inspection.NearbyMarkers.Count == 0)
        {
            lines.Add(new("  none", LineStyle.Dim));
        }

        lines.AddRange(inspection.NearbyMarkers.Select(marker =>
            new InspectorLine($"  {marker.Id} \"{marker.Label}\" at {marker.Position}, placed {marker.PlacedAt.ToElapsedString()}", LineStyle.Normal)));
        return lines;
    }
}
