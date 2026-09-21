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
/// service), and a trace-driven replay of the real Zellbury data, without
/// duplicating the event-scheduling logic.
/// </summary>
public class SimulationEngine
{
    private readonly Func<double> _nextInterArrivalTime;
    private readonly Func<double> _nextServiceTime;
    private readonly int _numberOfCustomersToGenerate;
    private readonly long _seed;

    public SimulationEngine(
        Func<double> nextInterArrivalTime,
        Func<double> nextServiceTime,
        int numberOfCustomersToGenerate,
        long seed)
    {
        _nextInterArrivalTime = nextInterArrivalTime;
        _nextServiceTime = nextServiceTime;
        _numberOfCustomersToGenerate = numberOfCustomersToGenerate;
        _seed = seed;
    }

    public SimulationResult Run()
    {
        var fel = new FutureEventList();
        var customers = new Dictionary<int, Customer>();
        var waitingQueue = new Queue<int>(); // holds customer IDs, FIFO
        var queueTrace = new List<QueueLengthSample> { new(0, 0) };

        bool serverBusy = false;
        int? inServiceCustomerId = null;

        double clock = 0;
        double lastEventTime = 0;
        double areaQueueLength = 0;   // integral of Lq(t) dt  -> time-weighted queue length
        double areaSystemBusy = 0;    // integral of server-busy indicator dt -> utilization
        int maxQueueLength = 0;

        int nextCustomerId = 1;
        int arrivalsScheduled = 0;
        int arrivalsCompleted = 0;
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
                    inServiceCustomerId = customer.Id;
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

                arrivalsCompleted++;

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
                    inServiceCustomerId = nextId;
                    fel.Schedule(nextCustomer.ServiceEndTime, EventType.Departure, nextId);
                }
                else
                {
                    serverBusy = false;
                    inServiceCustomerId = null;
                }
                queueTrace.Add(new QueueLengthSample(clock, waitingQueue.Count));
            }
        }

        var completed = customers.Values.Where(c => c.IsCompleted).OrderBy(c => c.Id).ToList();
        int remaining = customers.Count - completed.Count;

        double totalTime = Math.Max(clock, 0.0001);

        var result = new SimulationResult
        {
            Customers = customers.Values.OrderBy(c => c.Id).ToList(),
            QueueLengthTrace = queueTrace,
            CustomersServed = completed.Count,
            CustomersRemaining = remaining,
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

        var interArrivals = customers.Values.Select(c => c.InterArrivalTime).Where(v => v > 0).ToList();
        if (interArrivals.Count > 0)
            result.AverageInterArrivalTime = interArrivals.Average();

        return result;
    }
}
