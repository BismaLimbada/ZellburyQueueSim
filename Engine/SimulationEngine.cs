using ZellburyQueueSim.Models;

namespace ZellburyQueueSim.Engine;

/// <summary>
/// Discrete-event simulation of the Zellbury checkout/counter queue as a
/// single-server FIFO system:
///
///   customer reaches checkout -> joins queue -> waits if necessary
///   -> service begins -> service completes -> customer leaves
///
/// The engine is deliberately generator-agnostic: it is handed a function
/// that produces the next inter-arrival time and a function that produces
/// the next service time. This lets the same engine drive M/M/1
/// (exponential/exponential), M/G/1 (exponential arrivals + empirical
/// service), G/G/1 (resampled gaps and service times) and the "Simulate recorded
/// data" mode (the real recorded gaps and service times fed in order), without
/// duplicating the event-scheduling logic.
/// </summary>
public class SimulationEngine
{
    private readonly Func<double> _nextInterArrivalTime;
    private readonly Func<double> _nextServiceTime;
    private readonly int _numberOfCustomersToGenerate;
    private readonly long _seed;
    private readonly double _minimumHorizon;

    /// <param name="minimumHorizon">
    /// Time-averaged statistics (utilization, average queue length) are divided by
    /// max(last event time, minimumHorizon). Pass the observation horizon of the recorded
    /// data when simulating it, so the run is measured over the same window as the real
    /// observation. Leave at 0 for a free-standing random run (divide by the last event time).
    /// The system is empty after the last departure, so extending the window adds no area.
    /// </param>
    public SimulationEngine(
        Func<double> nextInterArrivalTime,
        Func<double> nextServiceTime,
        int numberOfCustomersToGenerate,
        long seed,
        double minimumHorizon = 0)
    {
        _nextInterArrivalTime = nextInterArrivalTime;
        _nextServiceTime = nextServiceTime;
        _numberOfCustomersToGenerate = numberOfCustomersToGenerate;
        _seed = seed;
        _minimumHorizon = minimumHorizon;
    }

    public SimulationResult Run()
    {
        var fel = new FutureEventList();
        var customers = new Dictionary<int, Customer>();
        var waitingQueue = new Queue<int>(); // holds customer IDs, FIFO
        var queueTrace = new List<QueueLengthSample> { new(0, 0) };

        bool serverBusy = false;

        double clock = 0;
        double lastEventTime = 0;
        double areaQueueLength = 0;   // integral of Lq(t) dt  -> time-weighted queue length
        double areaSystemBusy = 0;    // integral of server-busy indicator dt -> utilization
        int maxQueueLength = 0;

        int nextCustomerId = 1;
        int arrivalsScheduled = 0;
        double lastArrivalTime = 0;

        // Prime the FEL with the first arrival.
        if (_numberOfCustomersToGenerate > 0)
        {
            double firstInterArrival = _nextInterArrivalTime();
            double firstArrivalTime = firstInterArrival;
            fel.Schedule(firstArrivalTime, EventType.Arrival, nextCustomerId);
            customers[nextCustomerId] = new Customer
            {
                Id = nextCustomerId,
                InterArrivalTime = firstInterArrival,
                ArrivalTime = firstArrivalTime
            };
            lastArrivalTime = firstArrivalTime;
            nextCustomerId++;
            arrivalsScheduled++;
        }

        while (!fel.IsEmpty)
        {
            var evt = fel.PopNext();
            clock = evt.Time;

            // Time-weighted integration since the previous event.
            double delta = clock - lastEventTime;
            areaQueueLength += waitingQueue.Count * delta;
            areaSystemBusy += (serverBusy ? 1.0 : 0.0) * delta;
            lastEventTime = clock;

            if (evt.Type == EventType.Arrival)
            {
                var customer = customers[evt.CustomerId];

                if (!serverBusy)
                {
                    // Server is idle: service starts immediately.
                    serverBusy = true;
                    customer.ServiceStartTime = clock;
                    customer.ServiceTime = _nextServiceTime();
                    customer.ServiceEndTime = customer.ServiceStartTime + customer.ServiceTime;
                    fel.Schedule(customer.ServiceEndTime, EventType.Departure, customer.Id);
                }
                else
                {
                    // Server busy: customer joins the FIFO waiting line.
                    waitingQueue.Enqueue(customer.Id);
                    maxQueueLength = Math.Max(maxQueueLength, waitingQueue.Count);
                }
                queueTrace.Add(new QueueLengthSample(clock, waitingQueue.Count));

                // Schedule the next arrival, if we still need more customers.
                if (arrivalsScheduled < _numberOfCustomersToGenerate)
                {
                    double interArrival = _nextInterArrivalTime();
                    double nextArrivalTime = lastArrivalTime + interArrival;
                    lastArrivalTime = nextArrivalTime;

                    fel.Schedule(nextArrivalTime, EventType.Arrival, nextCustomerId);
                    customers[nextCustomerId] = new Customer
                    {
                        Id = nextCustomerId,
                        InterArrivalTime = interArrival,
                        ArrivalTime = nextArrivalTime
                    };
                    nextCustomerId++;
                    arrivalsScheduled++;
                }
            }
            else // Departure - a service completion
            {
                var finishing = customers[evt.CustomerId];
                finishing.IsCompleted = true;

                if (waitingQueue.Count > 0)
                {
                    // Immediately pull the next customer from the FIFO queue into service.
                    int nextId = waitingQueue.Dequeue();
                    var nextCustomer = customers[nextId];
                    nextCustomer.ServiceStartTime = clock;
                    nextCustomer.ServiceTime = _nextServiceTime();
                    nextCustomer.ServiceEndTime = nextCustomer.ServiceStartTime + nextCustomer.ServiceTime;
                    fel.Schedule(nextCustomer.ServiceEndTime, EventType.Departure, nextId);
                }
                else
                {
                    serverBusy = false;
                }
                queueTrace.Add(new QueueLengthSample(clock, waitingQueue.Count));
            }
        }

        var completed = customers.Values.Where(c => c.IsCompleted).OrderBy(c => c.Id).ToList();

        double totalTime = Math.Max(Math.Max(clock, _minimumHorizon), 0.0001);

        var result = new SimulationResult
        {
            Customers = customers.Values.OrderBy(c => c.Id).ToList(),
            QueueLengthTrace = queueTrace,
            CustomersServed = completed.Count,
            SimulationEndTime = clock,
            RandomSeed = _seed,
            MaximumQueueLength = maxQueueLength,
            ServerUtilization = areaSystemBusy / totalTime,
            AverageQueueLength = areaQueueLength / totalTime,
        };

        if (completed.Count > 0)
        {
            result.AverageWaitingTime = completed.Average(c => c.WaitingTime);
            result.AverageServiceTime = completed.Average(c => c.ServiceTime);
            result.AverageTimeInSystem = completed.Average(c => c.TimeInSystem);
        }

        return result;
    }
}
