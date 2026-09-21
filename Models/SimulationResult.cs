namespace ZellburyQueueSim.Models;

/// <summary>
/// A single (time, queueLength) sample recorded whenever the queue length
/// changes, used to draw the queue-length-over-time chart and to compute the
/// time-weighted average / maximum queue length.
/// </summary>
public readonly record struct QueueLengthSample(double Time, int QueueLength);

/// <summary>
/// Full output of one discrete-event simulation run: per-customer records
/// plus the aggregated statistics required by the report (waiting time,
/// service time, time in system, queue length, utilization, etc.).
/// </summary>
public class SimulationResult
{
    public List<Customer> Customers { get; set; } = new();
    public List<QueueLengthSample> QueueLengthTrace { get; set; } = new();

    public int CustomersServed { get; set; }
    public int CustomersRemaining { get; set; }

    public double AverageInterArrivalTime { get; set; }
    public double AverageWaitingTime { get; set; }
    public double AverageServiceTime { get; set; }
    public double AverageTimeInSystem { get; set; }
    public double AverageQueueLength { get; set; }
    public int MaximumQueueLength { get; set; }
    public double ServerUtilization { get; set; }

    public double SimulationEndTime { get; set; }
    public long RandomSeed { get; set; }

    /// <summary>Simulated arrival rate lambda, estimated as 1 / average inter-arrival time.</summary>
    public double EstimatedLambda => AverageInterArrivalTime > 0 ? 1.0 / AverageInterArrivalTime : 0;

    /// <summary>Simulated service rate mu, estimated as 1 / average service time.</summary>
    public double EstimatedMu => AverageServiceTime > 0 ? 1.0 / AverageServiceTime : 0;

    /// <summary>Little's Law check: L should approximately equal lambda * W (server + queue).</summary>
    public double LittlesLawL => EstimatedLambda * AverageTimeInSystem;

    /// <summary>
    /// Average number of customers in the whole system (queue + the one in
    /// service), decomposed as L = Lq + (fraction of time the server is
    /// busy). This is the simulated counterpart of the theoretical L.
    /// </summary>
    public double AverageNumberInSystem => AverageQueueLength + ServerUtilization;
}
