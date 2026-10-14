using Sovereigns.Client.View;

namespace Sovereigns.Client.Ui;

/// <summary>The layer list. Unavailable layers stay visible but are greyed and carry their owner and phase as a tooltip.</summary>
public partial class LayerPanel : PanelContainer
{
    private ItemList _list = null!;

    public event Action<int>? LayerChosen;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(300, 0);
        AddThemeStyleboxOverride("panel", UiStyle.Box());
        var column = new VBoxContainer();
        AddChild(column);

        var title = UiStyle.MakeLabel(14, UiStyle.Heading);
        title.Text = "Layers";
        column.AddChild(title);

        _list = new ItemList
        {
            FocusMode = FocusModeEnum.None,
            SelectMode = ItemList.SelectModeEnum.Single,
            AutoHeight = true,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        };
        _list.AddThemeFontSizeOverride("font_size", UiStyle.SmallFont);
        foreach (var layer in MapLayers.All)
        {
            var index = _list.AddItem(layer.ListText);
            _list.SetItemTooltip(index, layer.Legend);
            _list.SetItemCustomFgColor(index, layer.IsAvailable ? UiStyle.Normal : UiStyle.Dim);
        }

        _list.ItemSelected += index => LayerChosen?.Invoke((int)index);
        column.AddChild(_list);
    }

    /// <summary>Highlights a layer without raising <see cref="LayerChosen"/>.</summary>
    public void Highlight(int index) => _list.Select(index);
}
