using Sovereigns.Client.View;

namespace Sovereigns.Client.Ui;

/// <summary>
/// The layer list and the overlay toggles. Fields are grouped under their family; unavailable ones stay visible but
/// greyed, marked with their owner and phase. Survey lines and the chunk-residency overlay are toggles drawn over
/// whichever layer is selected.
/// </summary>
public partial class LayerPanel : PanelContainer
{
    private readonly List<int> _itemOfLayer = [];
    private readonly Dictionary<int, int> _layerOfItem = [];
    private ItemList _list = null!;
    private CheckBox _survey = null!;
    private CheckBox _residency = null!;

    /// <summary>The layers to list; set before the panel enters the tree.</summary>
    public IReadOnlyList<MapLayer> Layers { get; set; } = [];

    /// <summary>Text of each list row in order, family headings included.</summary>
    public IReadOnlyList<string> Rows => [.. Enumerable.Range(0, _list.ItemCount).Select(index => _list.GetItemText(index))];

    public event Action<int>? LayerChosen;

    public event Action<bool>? SurveyLinesToggled;

    public event Action<bool>? ResidencyToggled;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(420, 0);
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
            CustomMinimumSize = new Vector2(0, 360),
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        };
        _list.AddThemeFontSizeOverride("font_size", 12);
        string? family = null;
        for (var layerIndex = 0; layerIndex < Layers.Count; layerIndex++)
        {
            var layer = Layers[layerIndex];
            if (layer.Family?.Name is { } name && name != family)
            {
                family = name;
                var heading = _list.AddItem(name.ToUpperInvariant(), selectable: false);
                _list.SetItemCustomFgColor(heading, UiStyle.Heading);
            }

            var indent = layer.Family is null ? "" : "  ";
            var item = _list.AddItem(indent + layer.ListText);
            _itemOfLayer.Add(item);
            _layerOfItem[item] = layerIndex;
            _list.SetItemTooltip(item, layer.Legend);
            _list.SetItemCustomFgColor(item, layer.IsAvailable ? UiStyle.Normal : UiStyle.Dim);
        }

        _list.ItemSelected += item => LayerChosen?.Invoke(_layerOfItem[(int)item]);
        column.AddChild(_list);

        _survey = MakeToggle("Survey lines", "Draw survey lines over the selected layer");
        _survey.Toggled += on => SurveyLinesToggled?.Invoke(on);
        column.AddChild(_survey);
        _residency = MakeToggle("Chunk residency", "Outline the chunks the atlas cache holds, coloured by content");
        _residency.Toggled += on => ResidencyToggled?.Invoke(on);
        column.AddChild(_residency);
    }

    /// <summary>Highlights a layer without raising <see cref="LayerChosen"/>.</summary>
    public void Highlight(int layerIndex)
    {
        _list.Select(_itemOfLayer[layerIndex]);
        _list.EnsureCurrentIsVisible();
    }

    /// <summary>Shows the overlay toggles without raising their events.</summary>
    public void ShowOverlays(bool surveyLines, bool residency)
    {
        _survey.SetPressedNoSignal(surveyLines);
        _residency.SetPressedNoSignal(residency);
    }

    private static CheckBox MakeToggle(string text, string tooltip)
    {
        var box = new CheckBox { Text = text, TooltipText = tooltip, FocusMode = FocusModeEnum.None };
        box.AddThemeFontSizeOverride("font_size", 13);
        return box;
    }
}
