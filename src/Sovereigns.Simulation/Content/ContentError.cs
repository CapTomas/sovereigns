using System.Globalization;
using System.Text;

namespace Sovereigns.Simulation.Content;

/// <summary>
/// An actionable content problem: the file, the location inside it, what is wrong and how to fix it.
/// </summary>
public sealed record ContentError(string File, string Message, string? JsonPath = null, long? Line = null, string? Hint = null)
{
    public override string ToString()
    {
        var text = new StringBuilder(File);
        if (Line is { } line)
        {
            text.Append(CultureInfo.InvariantCulture, $":{line}");
        }

        text.Append(": ");
        if (JsonPath is not null)
        {
            text.Append(JsonPath).Append(": ");
        }

        text.Append(Message);
        if (Hint is not null)
        {
            text.Append(" Fix: ").Append(Hint);
        }

        return text.ToString();
    }
}
