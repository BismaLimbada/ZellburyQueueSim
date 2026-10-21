namespace ZellburyQueueSim.Models;

/// <summary>
/// The two event types relevant to a single-server checkout queue:
/// a customer joining the queue (Arrival) and a customer finishing their
/// transaction and leaving (Departure / service completion).
/// </summary>
public enum EventType
{
    Arrival,
    Departure
}

/// <summary>
/// A single scheduled occurrence on the simulation clock, held in the
/// Future Event List (FEL) and processed in non-decreasing time order.
/// </summary>
public class SimulationEvent
{
    public double Time { get; init; }
    public EventType Type { get; init; }
    public int CustomerId { get; init; }

    /// <summary>
    /// Insertion sequence, used only to break ties when two events share the
    /// exact same clock time, so the FEL behaves deterministically (FIFO
    /// among same-time events) rather than arbitrarily.
    /// </summary>
    public long Sequence { get; init; }

    public SimulationEvent(double time, EventType type, int customerId, long sequence)
    {
        Time = time;
        Type = type;
        CustomerId = customerId;
        Sequence = sequence;
    }
}
