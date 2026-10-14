using Sovereigns.Client.View;

namespace Sovereigns.Client.Ui;

/// <summary>Explains the active layer; for an unavailable layer it states "No data" with the owner and phase.</summary>
public partial class LayerLegend : PanelContainer
{
    private Label _label = null!;

    public string Text => _label.Text;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide, LayoutPresetMode.Minsize, 12);
        OffsetLeft = 250;
        OffsetBottom = -12;
        GrowVertical = GrowDirection.Begin;
        _label = UiStyle.MakeLabel(14, wrap: true);
        AddChild(_label);
    }

    public void Show(MapLayer layer, int markerCount)
    {
        var detail = layer.Key == MapLayer.DeveloperMarkersKey ? $" {markerCount} marker(s) in the world." : "";
        _label.Text = $"{layer.Name}: {layer.Legend}{detail}";
        AddThemeStyleboxOverride("panel", layer.IsAvailable
            ? UiStyle.Box(new Color(0.05f, 0.06f, 0.08f, 0.82f), 6)
            : UiStyle.Box(new Color(0.3f, 0.18f, 0.04f, 0.93f), 8));
        _label.AddThemeColorOverride("font_color", layer.IsAvailable ? UiStyle.Normal : UiStyle.Warning);
    }
}
