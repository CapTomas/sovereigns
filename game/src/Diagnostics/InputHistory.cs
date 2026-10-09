using Sovereigns.Simulation.Diagnostics;

namespace Sovereigns.Client.Diagnostics;

/// <summary>Ring buffer of the most recent handled input actions, attached to failure reports.</summary>
public sealed class InputHistory(int capacity)
{
    private readonly Queue<HostInputEvent> _events = new(capacity);
    private readonly Lock _gate = new();

    public void Record(long frame, double realSeconds, string action, string? detail = null)
    {
        lock (_gate)
        {
            if (_events.Count == capacity)
            {
                _events.Dequeue();
            }

            _events.Enqueue(new HostInputEvent(frame, realSeconds, action, detail));
        }
    }

    public IReadOnlyList<HostInputEvent> Snapshot()
    {
        lock (_gate)
        {
            return [.. _events];
        }
    }
}
