using ZellburyQueueSim.Models;

namespace ZellburyQueueSim.Engine;

/// <summary>
/// Turns the REAL recorded observations into a <see cref="SimulationResult"/> (the
/// "Real observed" column on the Simulator page) without re-simulating anything: every customer keeps the exact Arrival / Service Start / Exit
/// that was written down, so waiting time, service time and time in system are the
/// observed ones. (Re-running the recorded inter-arrival and service durations through
/// a FIFO engine would silently change the observed start times wherever a customer was
/// served out of arrival order.)
///
/// Queue length and number in system are rebuilt by sweeping the three timestamps:
///   waiting  = arrived and not yet started       (+1 at Arrival, -1 at Service Start)
///   in system = arrived and not yet exited       (+1 at Arrival, -1 at Exit)
///   in service = in system - waiting
/// Averages are time-weighted over [0, horizon] where the horizon is supplied by the caller
/// (the stated 60-minute window, extended when records run past it).
/// </summary>
public static class RecordedTraceBuilder
{
    public static SimulationResult Build(IReadOnlyList<Customer> recorded, double horizon)
    {
        var customers = recorded.Select(c => new Customer
        {
            Id = c.Id,
            InterArrivalTime = c.InterArrivalTime,
            ArrivalTime = c.ArrivalTime,
            ServiceStartTime = c.ServiceStartTime,
            ServiceTime = c.ServiceTime,
            ServiceEndTime = c.ServiceEndTime,
            IsCompleted = true
        }).ToList();

        // Net change in (waiting, system) at each distinct timestamp. Applying all changes that
        // share a timestamp together means a customer who is served the instant they arrive
        // never shows up as "waiting".
        var changes = new SortedDictionary<double, (int Waiting, int InSystem)>();
        void Add(double time, int dWaiting, int dSystem)
        {
            changes.TryGetValue(time, out var cur);
            changes[time] = (cur.Waiting + dWaiting, cur.InSystem + dSystem);
        }
        foreach (var c in customers)
        {
            Add(c.ArrivalTime, +1, +1);
            Add(c.ServiceStartTime, -1, 0);
            Add(c.ServiceEndTime, 0, -1);
        }

        var queueTrace = new List<QueueLengthSample> { new(0, 0) };
        double last = 0, areaQueue = 0, areaBusy = 0;
        int waiting = 0, inSystem = 0, maxQueue = 0;

        foreach (var kv in changes)
        {
            double dt = kv.Key - last;
            areaQueue += waiting * dt;
            if (inSystem - waiting > 0) areaBusy += dt;     // someone is being served
            last = kv.Key;
            waiting += kv.Value.Waiting;
            inSystem += kv.Value.InSystem;
            maxQueue = Math.Max(maxQueue, waiting);
            queueTrace.Add(new QueueLengthSample(kv.Key, waiting));
        }

        double end = Math.Max(horizon, last);
        if (end > last)
        {
            double dt = end - last;
            areaQueue += waiting * dt;
            if (inSystem - waiting > 0) areaBusy += dt;
        }
        double span = Math.Max(end, 0.0001);

        var result = new SimulationResult
        {
            Customers = customers,
            QueueLengthTrace = queueTrace,
            CustomersServed = customers.Count,
            SimulationEndTime = end,
            RandomSeed = 0,
            MaximumQueueLength = maxQueue,
            ServerUtilization = areaBusy / span,
            AverageQueueLength = areaQueue / span
        };

        if (customers.Count > 0)
        {
            result.AverageWaitingTime = customers.Average(c => c.WaitingTime);
            result.AverageServiceTime = customers.Average(c => c.ServiceTime);
            result.AverageTimeInSystem = customers.Average(c => c.TimeInSystem);
        }
        return result;
    }
}
