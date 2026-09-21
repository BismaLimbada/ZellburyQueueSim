using ZellburyQueueSim.Models;

namespace ZellburyQueueSim.Engine;

/// <summary>
/// The Future Event List (FEL) for the discrete-event simulation.
///
/// It is a priority queue ordered by event time (and, for events scheduled
/// at the exact same instant, by insertion order) so that
/// <see cref="PopNext"/> always returns the chronologically next event to
/// process - the core mechanism that drives the simulation clock forward.
///
/// Internally this wraps .NET's built-in PriorityQueue&lt;TElement,TPriority&gt;
/// (a binary min-heap), giving O(log n) scheduling and O(log n) removal of
/// the next event instead of a linear scan over a plain list.
/// </summary>
public class FutureEventList
{
    private readonly PriorityQueue<SimulationEvent, (double Time, long Sequence)> _heap = new();
    private long _sequenceCounter;

    public int Count => _heap.Count;
    public bool IsEmpty => _heap.Count == 0;

    /// <summary>Schedules a new event onto the FEL.</summary>
    public void Schedule(double time, EventType type, int customerId)
    {
        var evt = new SimulationEvent(time, type, customerId, _sequenceCounter);
        _heap.Enqueue(evt, (time, _sequenceCounter));
        _sequenceCounter++;
    }

    /// <summary>Removes and returns the event with the smallest (earliest) time.</summary>
    public SimulationEvent PopNext()
    {
        if (_heap.Count == 0)
            throw new InvalidOperationException("Future Event List is empty - no more events to process.");

        _heap.TryDequeue(out var evt, out _);
        return evt!;
    }

    /// <summary>Looks at the next event without removing it.</summary>
    public bool TryPeekNext(out SimulationEvent? next)
    {
        if (_heap.TryPeek(out var evt, out _))
        {
            next = evt;
            return true;
        }
        next = null;
        return false;
    }

    public void Clear() => _heap.Clear();
}
