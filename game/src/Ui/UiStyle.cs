namespace Sovereigns.Client.Ui;

/// <summary>Colours and small construction helpers shared by the inspection panels (nonfinal debug styling).</summary>
public static class UiStyle
{
    public const int SmallFont = 13;

    public static readonly Color Normal = new(0.88f, 0.9f, 0.94f);
    public static readonly Color Dim = new(0.55f, 0.58f, 0.64f);
    public static readonly Color Heading = new(0.45f, 0.8f, 1f);
    public static readonly Color Warning = new(1f, 0.75f, 0.3f);
    public static readonly Color Error = new(1f, 0.5f, 0.45f);

    private static readonly Color PanelColour = new(0.085f, 0.095f, 0.125f, 0.94f);

    public static Color Of(LineStyle style) => style switch
    {
        LineStyle.Heading => Heading,
        LineStyle.Dim => Dim,
        LineStyle.Warning => Warning,
        _ => Normal,
    };

    /// <summary>A panel background with padding; <paramref name="tint"/> overrides the default dark fill.</summary>
    public static StyleBoxFlat Box(Color? tint = null, int padding = 8)
    {
        var box = new StyleBoxFlat { BgColor = tint ?? PanelColour, BorderColor = new Color(1, 1, 1, 0.12f) };
        box.SetBorderWidthAll(1);
        box.SetContentMarginAll(padding);
        return box;
    }

    public static Label MakeLabel(int fontSize = SmallFont, Color? colour = null, bool wrap = false)
    {
        var label = new Label { MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", colour ?? Normal);
        if (wrap)
        {
            label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        }

        return label;
    }
}
