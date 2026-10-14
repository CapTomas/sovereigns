namespace Sovereigns.Client.Ui;

/// <summary>A prominent message across the top of the window: a launch problem or a stopped simulation.</summary>
public partial class ErrorBanner : PanelContainer
{
    private Label _label = null!;

    public string Text => _label.Text;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide, LayoutPresetMode.Minsize, 12);
        AddThemeStyleboxOverride("panel", UiStyle.Box(new Color(0.35f, 0.07f, 0.07f, 0.96f), 12));
        _label = UiStyle.MakeLabel(15, wrap: true);
        AddChild(_label);
        Visible = false;
    }

    public void ShowMessage(IEnumerable<string> lines)
    {
        _label.Text = string.Join('\n', lines);
        Visible = true;
    }
}
