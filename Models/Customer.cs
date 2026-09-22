namespace ZellburyQueueSim.Models;

/// <summary>
/// Represents a single customer flowing through the Zellbury checkout/counter
/// queue, from the moment they join the queue (arrival at the counter) to the
/// moment their transaction is completed (service end).
///
/// This mirrors the entity + timing fields defined in the Simulation
/// Environment Report: Arrival Time (join queue), Service Start Time,
/// Service Time (duration), Service End Time, Waiting Time, Time in System.
/// </summary>
public class Customer
{
    public int Id { get; set; }

    /// <summary>Time (in simulation minutes, from clock 0) between this customer's arrival and the previous one.</summary>
    public double InterArrivalTime { get; set; }

    /// <summary>Simulation-clock time at which the customer joined the checkout queue.</summary>
    public double ArrivalTime { get; set; }

    /// <summary>Simulation-clock time at which the cashier began serving this customer.</summary>
    public double ServiceStartTime { get; set; }

    /// <summary>Duration of the checkout transaction (Service End - Service Start).</summary>
    public double ServiceTime { get; set; }

    /// <summary>Simulation-clock time at which the transaction finished and the customer left the queue.</summary>
    public double ServiceEndTime { get; set; }

    /// <summary>Time spent waiting in the queue before service started (Service Start - Arrival).</summary>
    public double WaitingTime => ServiceStartTime - ArrivalTime;

    /// <summary>Total time spent in the system, queue + service (Service End - Arrival).</summary>
    public double TimeInSystem => ServiceEndTime - ArrivalTime;

    /// <summary>True once this customer's service has actually completed in the simulation.</summary>
    public bool IsCompleted { get; set; }
}
