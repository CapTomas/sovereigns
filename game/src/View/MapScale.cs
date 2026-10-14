using System.Globalization;

namespace Sovereigns.Client.View;

/// <summary>Display-only scale decisions: which grid spacing to draw, how long a scale bar is, and how distances read.</summary>
public static class MapScale
{
    /// <summary>Metres between grid lines, finest first.</summary>
    public static readonly IReadOnlyList<double> GridSpacingsM = [1, 10, 100, 1000, 10_000];

    private const double MinGridSpacingPixels = 64;

    /// <summary>The finest grid spacing whose lines stay at least 64 px apart, or the coarsest when none does.</summary>
    public static double ChooseGridSpacing(double pixelsPerMetre) =>
        GridSpacingsM.FirstOrDefault(spacing => spacing * pixelsPerMetre >= MinGridSpacingPixels, GridSpacingsM[^1]);

    /// <summary>The longest 1-2-5 length in metres whose bar fits within <paramref name="maxPixels"/>.</summary>
    public static double ChooseScaleBarLength(double pixelsPerMetre, double maxPixels)
    {
        var best = 1.0;
        for (var exponent = 0; exponent <= 7; exponent++)
        {
            foreach (var mantissa in new[] { 1.0, 2.0, 5.0 })
            {
                var length = mantissa * Math.Pow(10, exponent);
                if (length * pixelsPerMetre > maxPixels)
                {
                    return best;
                }

                best = length;
            }
        }

        return best;
    }

    /// <summary>Metres below one kilometre, kilometres above: <c>250 m</c>, <c>12.5 km</c>.</summary>
    public static string FormatDistance(double metres) =>
        Math.Abs(metres) >= 1000
            ? string.Create(CultureInfo.InvariantCulture, $"{metres / 1000:0.###} km")
            : string.Create(CultureInfo.InvariantCulture, $"{metres:0.###} m");
}
