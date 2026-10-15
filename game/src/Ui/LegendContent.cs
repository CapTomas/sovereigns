using System.Globalization;
using Sovereigns.Client.View;
using Sovereigns.Simulation.Atlas;

namespace Sovereigns.Client.Ui;

/// <summary>What the map legend shows (SOV-P03-T14): the explanation, and for a field overlay the colour bar or arrow scale.</summary>
/// <param name="Text">The explanatory lines: field, unit, level, range and no-data meaning.</param>
/// <param name="HasBar">True for a scalar overlay: a colour bar from <paramref name="MinText"/> to <paramref name="MaxText"/>.</param>
/// <param name="ArrowText">For a vector overlay, what a full-length arrow stands for; empty when there are no arrows.</param>
/// <param name="ShowsNoData">True when the legend includes the no-data swatch.</param>
public sealed record LegendModel(string Text, bool HasBar, string MinText, string MaxText, string ArrowText, bool ShowsNoData, FieldOverlay? Overlay);

public static class LegendContent
{
    public static LegendModel Build(MapLayer layer, int markerCount, FieldOverlay? overlay, GridLevel level, bool levelFixed)
    {
        ArgumentNullException.ThrowIfNull(layer);
        if (!layer.IsFieldOverlay)
        {
            var detail = layer.Key == MapLayer.DeveloperMarkersKey ? $" {markerCount} marker(s) in the world." : "";
            return new LegendModel($"{layer.Name}: {layer.Legend}{detail}", false, "", "", "", false, null);
        }

        var field = layer.Field!;
        string F(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
        var lines = new List<string>
        {
            F($"{field.Name} [{field.Unit}] — {field.Family.Name}; {field.Class.ToString().ToLowerInvariant()}, native level {ViewLevels.Describe(field.NativeLevel)}"),
        };

        var how = levelFixed ? "fixed with the level keys" : "chosen from the zoom";
        var spacing = overlay is null ? "" : F($"; one sample every {MapScale.FormatDistance(Math.Round(overlay.SpacingM, 1))}");
        lines.Add($"level: {ViewLevels.Describe(level)} ({how}){spacing}");

        var min = "";
        var max = "";
        var arrow = "";
        if (overlay is null)
        {
            lines.Add("no samples: the world is out of view");
        }
        else if (overlay.IsVector)
        {
            arrow = overlay.ArrowReference > 0 ? $"{FieldFormat.Compact(overlay.ArrowReference)} {field.Unit}" : "";
            lines.Add(overlay.ArrowReference > 0
                ? $"arrows point toward the vector's direction; a full-length arrow is {arrow}; largest in view {FieldFormat.Compact(overlay.DataMax)} {field.Unit}"
                : $"flat: every vector in view is 0 {field.Unit}, so there are no arrows");
        }
        else if (overlay.HasData)
        {
            (min, max) = (FieldFormat.Value(field, overlay.RangeMin), FieldFormat.Value(field, overlay.RangeMax));
            lines.Add(overlay.UsesDataRange
                ? $"grey ramp spans the data range in view, not the field range ({FieldFormat.Value(field, field.Min)} to {FieldFormat.Value(field, field.Max)}): dark {min}, light {max}"
                : $"grey ramp spans the field range: dark {min}, light {max}");
        }
        else
        {
            lines.Add("no data in view");
        }

        lines.Add("no data: hatched outside the world; transparent where a sample has no value");
        return new LegendModel(string.Join('\n', lines), overlay is { IsVector: false, HasData: true }, min, max, arrow, true, overlay);
    }
}
