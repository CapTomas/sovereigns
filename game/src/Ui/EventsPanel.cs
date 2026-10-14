using Sovereigns.Simulation.Events;

namespace Sovereigns.Client.Ui;

/// <summary>
/// The world's most recent events, newest first. It reads the ledger and keeps no state of its own; it redraws only
/// when the ledger has grown so that its scroll position is not reset.
/// </summary>
public partial class EventsPanel : PanelContainer
{
    private Label _title = null!;
    private RichTextLabel _body = null!;
    private int _shownCount = -1;

    public IReadOnlyList<InspectorLine> Lines { get; private set; } = [];

    public string Title => _title.Text;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(300, 120);
        SizeFlagsVertical = SizeFlags.ExpandFill;
        AddThemeStyleboxOverride("panel", UiStyle.Box());
        var column = new VBoxContainer();
        AddChild(column);

        _title = UiStyle.MakeLabel(14, UiStyle.Heading);
        column.AddChild(_title);
        _body = new RichTextLabel
        {
            ScrollActive = true,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            FocusMode = FocusModeEnum.None,
        };
        _body.AddThemeFontSizeOverride("normal_font_size", 12);
        column.AddChild(_body);
    }

    public void Show(EventLedger ledger)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        if (ledger.Count == _shownCount)
        {
            return;
        }

        _shownCount = ledger.Count;
        _title.Text = EventsContent.Title(ledger);
        Lines = EventsContent.Build(ledger);
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
