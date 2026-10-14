using Sovereigns.Simulation.Kernel;

namespace Sovereigns.Client.Ui;

/// <summary>A strip over the map while the world waits for a decision: time is stopped until it is acknowledged.</summary>
public partial class DecisionBanner : PanelContainer
{
    private Label _label = null!;

    public string Text => _label.Text;

    /// <summary>The banner text for the oldest pending decision, naming the key that acknowledges it.</summary>
    public static string Describe(IReadOnlyList<PendingDecision> decisions, string acknowledgeKey)
    {
        ArgumentNullException.ThrowIfNull(decisions);
        var more = decisions.Count > 1 ? $" (+{decisions.Count - 1} more)" : "";
        return $"Decision pending: {decisions[0].Label} — press {acknowledgeKey} to acknowledge{more}";
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide, LayoutPresetMode.Minsize, 8);
        // Leave the debug HUD in the top-left corner and the north arrow in the top-right corner uncovered.
        OffsetLeft = 410;
        OffsetRight = -64;
        AddThemeStyleboxOverride("panel", UiStyle.Box(new Color(0.42f, 0.27f, 0.03f, 0.96f), 10));
        _label = UiStyle.MakeLabel(16, Colors.White, wrap: true);
        AddChild(_label);
        Visible = false;
    }

    public void Show(IReadOnlyList<PendingDecision> decisions, string acknowledgeKey)
    {
        ArgumentNullException.ThrowIfNull(decisions);
        if (decisions.Count == 0)
        {
            Visible = false;
            return;
        }

        _label.Text = Describe(decisions, acknowledgeKey);
        Visible = true;
    }
}
