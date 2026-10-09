namespace Sovereigns.Client.Ui;

/// <summary>Bottom bar: a transient status message above the help line listing the current bindings.</summary>
public partial class StatusBar : PanelContainer
{
    private const ulong StatusMilliseconds = 8000;

    private Label _status = null!;
    private Label _help = null!;
    private ulong _clearAt;

    public string HelpText => _help.Text;

    public override void _Ready()
    {
        AddThemeStyleboxOverride("panel", UiStyle.Box(padding: 5));
        var column = new VBoxContainer();
        AddChild(column);
        _status = UiStyle.MakeLabel(wrap: true);
        column.AddChild(_status);
        _help = UiStyle.MakeLabel(12, UiStyle.Dim, wrap: true);
        column.AddChild(_help);
    }

    public override void _Process(double delta)
    {
        if (_clearAt != 0 && Time.GetTicksMsec() >= _clearAt)
        {
            _status.Text = "";
            _clearAt = 0;
        }
    }

    public void SetHelp(string text) => _help.Text = text;

    public void ShowStatus(string text, LineStyle style = LineStyle.Normal)
    {
        _status.Text = text;
        _status.AddThemeColorOverride("font_color", UiStyle.Of(style));
        _clearAt = Time.GetTicksMsec() + StatusMilliseconds;
    }
}
