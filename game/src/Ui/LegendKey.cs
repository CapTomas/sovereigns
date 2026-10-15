using Sovereigns.Client.View;

namespace Sovereigns.Client.Ui;

/// <summary>The legend's graphic row: grey colour bar with its end values or an arrow with its scale, then the no-data swatch.</summary>
public partial class LegendKey : Control
{
    private const int FontSize = 12;
    private const float BarWidth = 180;
    private const float BarHeight = 14;
    private static readonly Color Ink = new(0.85f, 0.9f, 0.95f);

    public LegendModel? Model { get; set; }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        CustomMinimumSize = new Vector2(0, 24);
    }

    public override void _Draw()
    {
        if (Model is null)
        {
            return;
        }

        var font = ThemeDB.FallbackFont;
        var baseline = new Vector2(0, (BarHeight / 2) + 5);
        var x = 0f;
        if (Model.HasBar && Model.Overlay is { } overlay)
        {
            x += DrawText(font, x, Model.MinText) + 6;
            const int strips = 90;
            for (var i = 0; i < strips; i++)
            {
                var value = overlay.RangeMin + ((overlay.RangeMax - overlay.RangeMin) * i / (strips - 1));
                DrawRect(new Rect2(x + (BarWidth * i / strips), 2, (BarWidth / strips) + 1, BarHeight), FieldRamp.ToColour(value, overlay.RangeMin, overlay.RangeMax));
            }

            DrawRect(new Rect2(x, 2, BarWidth, BarHeight), Ink with { A = 0.6f }, filled: false);
            x += BarWidth + 6;
            x += DrawText(font, x, Model.MaxText) + 28;
        }
        else if (Model.ArrowText.Length > 0)
        {
            x += DrawArrowSample(x) + 6;
            x += DrawText(font, x, Model.ArrowText) + 28;
        }

        DrawNoDataSwatch(x);
        DrawString(font, new Vector2(x + 30, baseline.Y + 2), "no data", HorizontalAlignment.Left, -1, FontSize, Ink);
    }

    private float DrawText(Font font, float x, string text)
    {
        DrawString(font, new Vector2(x, (BarHeight / 2) + 7), text, HorizontalAlignment.Left, -1, FontSize, Ink);
        return font.GetStringSize(text, HorizontalAlignment.Left, -1, FontSize).X;
    }

    private float DrawArrowSample(float x)
    {
        const float length = 40;
        var y = (BarHeight / 2) + 2;
        DrawLine(new Vector2(x, y), new Vector2(x + length, y), Ink, 2);
        DrawColoredPolygon([new Vector2(x + length + 4, y), new Vector2(x + length - 6, y - 5), new Vector2(x + length - 6, y + 5)], Ink);
        return length + 4;
    }

    private void DrawNoDataSwatch(float x)
    {
        var box = new Rect2(x, 2, 24, BarHeight);
        DrawRect(box, new Color(0.45f, 0.3f, 0.12f, 0.6f));
        const float bottom = 2 + BarHeight;
        for (var offset = -BarHeight; offset < 24; offset += 6)
        {
            // A "/" stroke from (offset, bottom) up and right, clipped to the swatch's left and right sides.
            var startX = Math.Max(0, offset);
            var endX = Math.Min(24, offset + BarHeight);
            if (endX > startX)
            {
                DrawLine(new Vector2(x + startX, bottom - (startX - offset)), new Vector2(x + endX, bottom - (endX - offset)), new Color(0.95f, 0.65f, 0.25f, 0.9f));
            }
        }

        DrawRect(box, Ink with { A = 0.6f }, filled: false);
    }
}
