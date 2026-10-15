using System.Globalization;
using Sovereigns.Client.View;
using Sovereigns.Simulation.Atlas;
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
/// Turns one <see cref="LocationInspection"/> into display lines (SOV-P02-T13, SOV-P03-T13). Readings are SI numbers;
/// this class converts them to display units (degrees, hh:mm:ss) and lists each reading's provenance: the grid level
/// it was sampled at, its classification, basis, owner, source and when its inputs last changed, then the vector
/// components and the grid cells with their weights that produced the value. Markers and features near the location
/// come before the families so that they are not lost below the long list of readings. An unavailable reading shows
/// its owner-and-phase reason in a distinct style and never a value.
/// </summary>
public static class InspectorContent
{
    // The simulation reports local mean solar time in seconds after local midnight; the unit "s" alone is not enough to tell it from a duration.
    private const string SolarTimeQuantity = "local mean solar time";

    public const string NoSelection = "Click the map to select a location. Its values appear here, re-queried from the simulation.";

    /// <param name="levelNote">Says which grid level the readings were sampled at and why; shown under the location.</param>
    /// <param name="focus">The family of the selected layer; it is listed first.</param>
    public static IReadOnlyList<InspectorLine> Build(LocationInspection inspection, double markerRadiusM, string? levelNote = null, FieldFamily? focus = null)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        var lines = new List<InspectorLine>
        {
            new($"Location {inspection.Point}", LineStyle.Heading),
            inspection.InsideWorld
                ? new("Inside the world frame.", LineStyle.Normal)
                : new("Outside the world frame: the simulation has no data here.", LineStyle.Warning),
        };
        if (levelNote is not null)
        {
            lines.Add(new(levelNote, LineStyle.Dim));
        }

        lines.Add(new("", LineStyle.Normal));

        lines.Add(inspection.NearbyMarkers.Count == 0
            ? new($"Markers within {MapScale.FormatDistance(markerRadiusM)}: none", LineStyle.Dim)
            : new($"Markers within {MapScale.FormatDistance(markerRadiusM)}", LineStyle.Heading));
        lines.AddRange(inspection.NearbyMarkers.Select(marker =>
            new InspectorLine($"  {marker.Id} \"{marker.Label}\" at {marker.Position}, placed {ClockText.Date(marker.PlacedAt)}", LineStyle.Normal)));

        lines.Add(inspection.NearbyFeatures.Count == 0
            ? new($"Nearby features within {MapScale.FormatDistance(markerRadiusM)}: none", LineStyle.Dim)
            : new($"Nearby features within {MapScale.FormatDistance(markerRadiusM)}", LineStyle.Heading));
        lines.AddRange(inspection.NearbyFeatures.Select(hit => new InspectorLine(
            $"  {hit.Feature.Id} \"{hit.Feature.Label}\": {FieldFormat.Words(hit.Feature.Kind)}, {MapScale.FormatDistance(Math.Round(hit.DistanceM, 1))} away, drawn {ClockText.Date(hit.Feature.CreatedAt)}",
            LineStyle.Normal)));

        lines.Add(new("", LineStyle.Normal));

        // The selected layer's family comes first, then those with data, so the usable readings are at the top; the stable sort keeps catalogue order.
        foreach (var family in inspection.Families.OrderBy(family => family.Family == focus ? 0 : family.Availability == FieldAvailability.Unavailable ? 2 : 1))
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

        return lines;
    }

    /// <summary>A reading's value in display units: degrees with four decimals for angles, hh:mm:ss for solar time, else the SI value with its unit.</summary>
    public static string FormatValue(FieldReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        return reading.Value is not { } value
            ? reading.UnavailableReason ?? "unavailable"
            : FieldFormat.Value(value, reading.Unit, reading.Quantity == SolarTimeQuantity);
    }

    private static void AddReading(List<InspectorLine> lines, FieldReading reading)
    {
        if (reading.Value is null)
        {
            lines.Add(new($"  {reading.Quantity}: {FormatValue(reading)}", LineStyle.Dim));
            return;
        }

        var vector = reading.Components is { Count: 2 };
        lines.Add(new($"  {reading.Quantity}: {FormatValue(reading)}{(vector ? " (magnitude)" : "")}", LineStyle.Normal));
        if (reading.Level is { } level)
        {
            lines.Add(new($"      level: {ViewLevels.Describe(level)}", LineStyle.Dim));
        }

        // Atlas readings carry their classification (how the value was obtained) in the derivation slot.
        AddProvenance(lines, reading.Level is null ? "derivation" : "classification", reading.Derivation);
        lines.Add(new($"      basis: {reading.Basis.ToString().ToLowerInvariant()}", LineStyle.Dim));
        AddProvenance(lines, "owner", reading.Owner);
        AddProvenance(lines, "source", reading.Source);
        AddProvenance(lines, "updated", reading.UpdatedAt is { } updated ? ClockText.Date(updated) : null);
        if (reading.Components is [var east, var north])
        {
            var unit = FieldFormat.UnitSuffix(reading.Unit);
            lines.Add(new($"      east: {FieldFormat.Compact(east)}{unit}", LineStyle.Dim));
            lines.Add(new($"      north: {FieldFormat.Compact(north)}{unit}", LineStyle.Dim));
        }

        if (reading.Level is not null)
        {
            AddInputs(lines, reading);
        }
    }

    private static void AddInputs(List<InspectorLine> lines, FieldReading reading)
    {
        if (reading.Inputs.Count == 0)
        {
            lines.Add(new("      inputs: none (computed from position and time)", LineStyle.Dim));
            return;
        }

        lines.Add(new($"      inputs ({reading.Inputs.Count} grid cell(s) and weights):", LineStyle.Dim));
        var classification = reading.Derivation ?? "";
        foreach (var input in reading.Inputs)
        {
            var kind = input.Stored ? "stored"
                : classification.StartsWith("aggregate", StringComparison.Ordinal) ? "aggregate"
                : classification.StartsWith("derived", StringComparison.Ordinal) ? "derived"
                : "base";
            lines.Add(new($"        cell {input.Cell} w={InputNumber(input.Weight)} v={InputValues(input.Values)} ({kind})", LineStyle.Dim));
        }
    }

    /// <summary>A grid value or weight with three decimals, or two significant digits when it is tiny, so small gradients stay readable.</summary>
    public static string InputNumber(double value) =>
        value == 0 || Math.Abs(value) >= 0.001
            ? value.ToString("0.000", CultureInfo.InvariantCulture)
            : value.ToString("0.0e+0", CultureInfo.InvariantCulture);

    private static string InputValues(IReadOnlyList<double> values) =>
        values.Count == 1 ? InputNumber(values[0]) : $"({string.Join(", ", values.Select(InputNumber))})";

    private static void AddProvenance(List<InspectorLine> lines, string label, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            lines.Add(new($"      {label}: {value}", LineStyle.Dim));
        }
    }
}
