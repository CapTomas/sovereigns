using System.Globalization;
using Sovereigns.Simulation.Events;

namespace Sovereigns.Client.Ui;

/// <summary>Turns the newest events of the world's ledger into display lines (SOV-P02-T07): time, type, subjects, data and cause.</summary>
public static class EventsContent
{
    /// <summary>How many of the most recent events the panel lists.</summary>
    public const int RecentCount = 12;

    public static IReadOnlyList<InspectorLine> Build(EventLedger ledger, int count = RecentCount)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        var lines = new List<InspectorLine>();
        for (var i = ledger.Count - 1; i >= Math.Max(0, ledger.Count - count); i--)
        {
            var worldEvent = ledger.Events[i];
            lines.Add(new($"{ClockText.Date(worldEvent.At)}  {worldEvent.Type}", LineStyle.Normal));
            var detail = new List<string>();
            if (worldEvent.Subjects.Count > 0)
            {
                detail.Add($"[{string.Join(", ", worldEvent.Subjects)}]");
            }

            if (worldEvent.Data is { Count: > 0 } data)
            {
                detail.Add($"{{{string.Join(", ", data.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key}={pair.Value}"))}}}");
            }

            detail.Add($"cause: {worldEvent.Cause}");
            lines.Add(new($"    {string.Join(' ', detail)}", LineStyle.Dim));
        }

        if (ledger.Count == 0)
        {
            lines.Add(new("No events yet.", LineStyle.Dim));
        }

        return lines;
    }

    public static string Title(EventLedger ledger)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        return string.Create(CultureInfo.InvariantCulture, $"Recent events ({Math.Min(ledger.Count, RecentCount)} of {ledger.Count})");
    }
}
