using Sovereigns.Simulation.Ids;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Events;

/// <summary>
/// The world's ordered, append-only event ledger (SOV-P02-T07). Events are in nondecreasing time order with
/// increasing IDs; an append that would go back in time is a defect and throws (SOV-P02-T14).
/// </summary>
public sealed class EventLedger
{
    private readonly List<WorldEvent> _events = [];

    public IReadOnlyList<WorldEvent> Events => _events;

    public int Count => _events.Count;

    public bool TryGet(EntityId<Kind.Event> id, out WorldEvent found)
    {
        // IDs increase with position, so a binary search finds the event.
        var index = BinarySearch(id);
        found = index >= 0 ? _events[index] : null!;
        return index >= 0;
    }

    internal void Append(WorldEvent worldEvent)
    {
        if (_events.Count > 0)
        {
            var last = _events[^1];
            if (worldEvent.At < last.At)
            {
                throw new InvalidOperationException($"{worldEvent.Id} at {worldEvent.At} would precede {last.Id} at {last.At}; the ledger is chronological");
            }

            if (worldEvent.Id.Value <= last.Id.Value)
            {
                throw new InvalidOperationException($"{worldEvent.Id} is not newer than {last.Id}");
            }
        }

        _events.Add(worldEvent);
    }

    internal void Restore(IEnumerable<WorldEvent> events)
    {
        _events.Clear();
        foreach (var worldEvent in events)
        {
            Append(worldEvent);
        }
    }

    private int BinarySearch(EntityId<Kind.Event> id)
    {
        int low = 0, high = _events.Count - 1;
        while (low <= high)
        {
            var mid = low + (high - low) / 2;
            var value = _events[mid].Id.Value;
            if (value == id.Value)
            {
                return mid;
            }

            if (value < id.Value)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return -1;
    }
}
