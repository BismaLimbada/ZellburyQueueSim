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

    public double AverageWaitingTime { get; set; }
    public double AverageServiceTime { get; set; }
    public double AverageTimeInSystem { get; set; }
    public double AverageQueueLength { get; set; }
    public int MaximumQueueLength { get; set; }

    /// <summary>
    /// Measured busy fraction: time the server was serving / the time span the run (or the
    /// recording) is averaged over. This is NOT the model's rho = lambda / mu; see the
    /// utilization note on the Simulator page.
    /// </summary>
    public double ServerUtilization { get; set; }

    public double SimulationEndTime { get; set; }
    public long RandomSeed { get; set; }

    /// <summary>
    /// Average number of customers in the whole system (queue + the one in
    /// service), decomposed as L = Lq + (fraction of time the server is
    /// busy). This is the simulated / observed counterpart of the theoretical L.
    /// </summary>
    public double AverageNumberInSystem => AverageQueueLength + ServerUtilization;
}
