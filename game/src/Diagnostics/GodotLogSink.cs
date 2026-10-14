using Sovereigns.Simulation.Diagnostics;

namespace Sovereigns.Client.Diagnostics;

/// <summary>
/// Sends log records to Godot's output. Information and warnings print as plain lines; the text already says
/// "[WARNING]", and a C# backtrace adds nothing to an expected warning such as a rejected command. Errors use
/// PushError, so they keep their backtrace and appear in the editor's debugger.
/// </summary>
public sealed class GodotLogSink : ILogSink
{
    public void Write(LogRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var text = TextLogSink.Format(record);
        switch (record.Severity)
        {
            case LogSeverity.Warning:
                GD.PrintErr(text);
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
