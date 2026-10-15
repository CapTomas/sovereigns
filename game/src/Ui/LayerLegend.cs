using Sovereigns.Client.View;
using Sovereigns.Simulation.Atlas;

namespace Sovereigns.Client.Ui;

/// <summary>
/// Explains the active layer. A field overlay gets its name, unit, level, value range, a colour bar or arrow scale and
/// the no-data swatch; an unavailable layer states "No data" with the owner and phase.
/// </summary>
public partial class LayerLegend : PanelContainer
{
    private Label _label = null!;
    private LegendKey _key = null!;

    public string Text => _label.Text;

    /// <summary>What the legend currently shows, for inspection.</summary>
    public LegendModel? Model { get; private set; }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide, LayoutPresetMode.Minsize, 12);
        OffsetLeft = 250;
        OffsetBottom = -12;
        GrowVertical = GrowDirection.Begin;
        var column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        AddChild(column);
        _label = UiStyle.MakeLabel(13, wrap: true);
        column.AddChild(_label);
        _key = new LegendKey { Visible = false };
        column.AddChild(_key);
    }

    public void Show(MapLayer layer, int markerCount, FieldOverlay? overlay = null, GridLevel level = GridLevel.Local, bool levelFixed = false)
    {
        ArgumentNullException.ThrowIfNull(layer);
        var model = LegendContent.Build(layer, markerCount, overlay?.Field == layer.Field ? overlay : null, level, levelFixed);
        Model = model;
        _label.Text = model.Text;
        _key.Visible = layer.IsFieldOverlay;
        _key.Model = model;
        _key.QueueRedraw();
        AddThemeStyleboxOverride("panel", layer.IsAvailable
            ? UiStyle.Box(new Color(0.05f, 0.06f, 0.08f, 0.82f), 6)
            : UiStyle.Box(new Color(0.3f, 0.18f, 0.04f, 0.93f), 8));
        _label.AddThemeColorOverride("font_color", layer.IsAvailable ? UiStyle.Normal : UiStyle.Warning);
    }
}
