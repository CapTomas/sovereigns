using Sovereigns.Simulation.Atlas;

namespace Sovereigns.Client.View;

/// <summary>
/// The greyscale colour mapping of scalar overlays (SOV-P03-T14): the range minimum is dark grey, the maximum near
/// white, no data is fully transparent. Never pure black, so a low value still reads against the dark map.
/// </summary>
public static class FieldRamp
{
    public const double Dark = 0.14;
    public const double Light = 0.96;

    /// <summary>A field narrower than this fraction of its declared range is mapped over the data range instead.</summary>
    public const double NarrowFraction = 0.1;

    /// <summary>RGBA bytes of <paramref name="value"/> between <paramref name="min"/> and <paramref name="max"/>; NaN is transparent.</summary>
    public static (byte R, byte G, byte B, byte A) Map(double value, double min, double max)
    {
        if (double.IsNaN(value))
        {
            return (0, 0, 0, 0);
        }

        var t = max > min ? Math.Clamp((value - min) / (max - min), 0, 1) : 0.5;
        var grey = (byte)Math.Round(255 * (Dark + (Light - Dark) * t));
        return (grey, grey, grey, 255);
    }

    public static Color ToColour(double value, double min, double max)
    {
        var (r, g, b, a) = Map(value, min, max);
        return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
    }

    /// <summary>
    /// The value range the colours span. A field far narrower than its declared range over the view (latitude over
    /// a 100 km world, say) would show as one flat grey, so the observed range is used and the legend says so.
    /// </summary>
    public static (double Min, double Max, bool IsDataRange) ChooseRange(FieldDefinition field, double observedMin, double observedMax)
    {
        ArgumentNullException.ThrowIfNull(field);
        var span = observedMax - observedMin;
        if (double.IsFinite(span) && span > 0 && span < NarrowFraction * (field.Max - field.Min))
        {
            return (observedMin, observedMax, true);
        }

        return (field.Min, field.Max, false);
    }
}
