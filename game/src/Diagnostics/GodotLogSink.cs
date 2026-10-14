using Sovereigns.Simulation.Diagnostics;

namespace Sovereigns.Client.Diagnostics;

/// <summary>Sends log records to Godot's output: Print for information, PushWarning and PushError above that.</summary>
public sealed class GodotLogSink : ILogSink
{
    public void Write(LogRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var text = TextLogSink.Format(record);
        switch (record.Severity)
        {
            case LogSeverity.Warning:
                GD.PushWarning(text);
                break;
            case >= LogSeverity.Error:
                GD.PushError(text);
                break;
            default:
                GD.Print(text);
                break;
        }
    }
}
