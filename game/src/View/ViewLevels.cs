using Sovereigns.Simulation.Atlas;

namespace Sovereigns.Client.View;

/// <summary>
/// Which atlas level the map samples at a given zoom, and how levels read on screen (SOV-P03-T14). The level is a
/// display decision only: every level answers from the same authoritative data (ADR-0009).
/// </summary>
public static class ViewLevels
{
    /// <summary>Screen pixels between scalar overlay samples.</summary>
    public const double SamplePixels = 6;

    /// <summary>Screen pixels between vector arrows.</summary>
    public const double ArrowPixels = 40;

    /// <summary>
    /// The level whose cell size is closest to, and not finer than, <paramref name="spacingM"/>: the finest level
    /// that still has cells at least as large as the sample spacing, so a sample never reads more detail than it
    /// can show. Spacings finer than 1 m stay at tactical, the finest level; coarser than 4096 m stay at macro.
    /// </summary>
    public static GridLevel ForSpacing(double spacingM)
    {
        for (var level = GridLevel.Tactical; level > GridLevel.Macro; level--)
        {
            if (Grid.CellSize(level) >= spacingM)
            {
                return level;
            }
        }

        return GridLevel.Macro;
    }

    public static GridLevel ForZoom(double pixelsPerMetre) => ForSpacing(SamplePixels / pixelsPerMetre);

    /// <summary>The level and its cell size, for example <c>local (16 m)</c>.</summary>
    public static string Describe(GridLevel level) => $"{Grid.Name(level)} ({MapScale.FormatDistance(Grid.CellSize(level))})";

    /// <summary>The level one step finer (+1) or coarser (-1) than <paramref name="from"/>; null past either end, which means automatic.</summary>
    public static GridLevel? Step(GridLevel from, int direction)
    {
        var next = (int)from + direction;
        return next is < (int)GridLevel.Macro or > (int)GridLevel.Tactical ? null : (GridLevel)next;
    }

    /// <summary>Finds a level by name (<c>macro</c>, <c>regional</c>, <c>local</c>, <c>tactical</c>).</summary>
    public static bool TryParse(string text, out GridLevel level) =>
        Enum.TryParse(text, ignoreCase: true, out level) && Enum.IsDefined(level) && !int.TryParse(text, out _);
}
