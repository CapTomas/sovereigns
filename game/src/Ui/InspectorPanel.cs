using Sovereigns.Simulation.Fields;

namespace Sovereigns.Client.Ui;

/// <summary>
/// The location inspector. It shows the most recent <see cref="LocationInspection"/> it was given and keeps no
/// physical state of its own; the shell re-queries the world on every refresh.
/// </summary>
public partial class InspectorPanel : PanelContainer
{
    private Label _time = null!;
    private RichTextLabel _body = null!;
    private string _signature = "";

    public LocationInspection? Current { get; private set; }

    public IReadOnlyList<InspectorLine> Lines { get; private set; } = [];

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(430, 0);
        AddThemeStyleboxOverride("panel", UiStyle.Box());
        var column = new VBoxContainer();
        AddChild(column);

        var title = UiStyle.MakeLabel(14, UiStyle.Heading);
        title.Text = "Location inspector";
        column.AddChild(title);
        _time = UiStyle.MakeLabel(colour: UiStyle.Dim);
        column.AddChild(_time);

        _body = new RichTextLabel
        {
            ScrollActive = true,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SelectionEnabled = true,
        };
        _body.AddThemeFontSizeOverride("normal_font_size", UiStyle.SmallFont);
        column.AddChild(_body);
        ShowNothing();
    }

    public void ShowNothing()
    {
        Current = null;
        Lines = [new(InspectorContent.NoSelection, LineStyle.Dim)];
        _time.Text = "";
        Render();
    }

    public void Show(LocationInspection inspection, double markerRadiusM)
    {
        Current = inspection;
        Lines = InspectorContent.Build(inspection, markerRadiusM);
        _time.Text = $"Queried at simulation time {inspection.At.ToElapsedString()} ({inspection.At.Milliseconds} ms)";
        Render();
    }

    private void Render()
    {
        // Rebuilding resets the scroll position, so only do it when the displayed content changed.
        var signature = string.Join('\n', Lines.Select(line => $"{(int)line.Style}{line.Text}"));
        if (signature == _signature)
        {
            return;
        }

        _signature = signature;
        _body.Clear();
        foreach (var line in Lines)
        {
            _body.PushColor(UiStyle.Of(line.Style));
            _body.AddText(line.Text);
            _body.Pop();
            _body.Newline();
        }
    }
}
