using ZellburyQueueSim.Models;

namespace ZellburyQueueSim.Services;

/// <summary>
/// Hard-coded reference data collected by Group 11 at the Zellbury outlet,
/// Lucky One Mall, Gulberg Town, Karachi, on the observation window
/// 16:20-17:20 (see Simulation Environment Report, Section 6 and the
/// attached workbook "Group11-Zellbury_formatted.xlsx").
///
/// Two checkout counters were timed independently - women's ("Female
/// Counter") and men's ("Male Counter") - each recording, per customer:
/// Arrival Time (joins the queue), Service Time (service starts) and Exit
/// Time (service ends / customer leaves the queue). This service treats
/// that data as the ONLY source of truth for the scenario: nothing here is
/// invented.
///
/// All clock values are normalized to minutes elapsed since the
/// observation window opened at 16:20, so ObservationStart = 0.
/// </summary>
public class ZellburyDataService
{
    private static readonly TimeOnly ObservationStart = new(16, 20);

    // Raw (Arrival, ServiceStart, Exit) triples exactly as recorded in the workbook.
    private static readonly (TimeOnly Arrival, TimeOnly ServiceStart, TimeOnly Exit)[] FemaleRaw =
    {
        (new(16,22), new(16,30), new(16,31)),
        (new(16,27), new(16,32), new(16,36)),
        (new(16,26), new(16,37), new(16,38)),
        (new(16,36), new(16,38), new(16,39)),
        (new(16,25), new(16,38), new(16,44)),
        (new(16,37), new(16,39), new(16,44)),
        (new(16,30), new(16,42), new(16,45)),
        (new(16,35), new(16,43), new(16,46)),
        (new(16,37), new(16,46), new(16,54)),
        (new(16,32), new(16,46), new(16,49)),
        (new(16,49), new(16,50), new(16,52)),
        (new(16,47), new(16,55), new(16,57)),
        (new(16,53), new(16,57), new(16,59)),
        (new(16,59), new(16,59), new(17, 1)),
        (new(16,50), new(17, 2), new(17, 6)),
        (new(16,30), new(17, 3), new(17, 8)),
        (new(16,50), new(17, 4), new(17, 8)),
        (new(16,54), new(17, 6), new(17, 9)),
        (new(16,50), new(17,10), new(17,13)),
        (new(16,56), new(17,12), new(17,15)),
        (new(17, 0), new(17,13), new(17,15)),
        (new(17, 0), new(17,14), new(17,16)),
    };

    private static readonly (TimeOnly Arrival, TimeOnly ServiceStart, TimeOnly Exit)[] MaleRaw =
    {
        (new(16,21), new(16,30), new(16,31)),
        (new(16,22), new(16,27), new(16,28)),
        (new(16,30), new(16,40), new(16,42)),
        (new(16,33), new(16,45), new(16,49)),
        (new(16,34), new(16,45), new(16,47)),
        (new(16,35), new(16,42), new(16,45)),
        (new(16,36), new(16,41), new(16,43)),
        (new(16,38), new(16,50), new(16,53)),
        (new(16,40), new(16,54), new(16,55)),
        (new(16,41), new(16,58), new(16,59)),
        (new(16,48), new(16,55), new(16,59)),
        (new(16,48), new(16,50), new(16,56)),
        (new(16,49), new(17, 1), new(17, 3)),
        (new(16,50), new(17, 2), new(17, 5)),
        (new(17, 6), new(17, 9), new(17,11)),
        (new(17, 9), new(17,19), new(17,22)),
        (new(17, 9), new(17,19), new(17,21)),
        (new(17,10), new(17,15), new(17,16)),
        (new(17,12), new(17,21), new(17,23)),
        (new(17,14), new(17,19), new(17,19)),
    };

    private static double ToMinutes(TimeOnly t) => (t.ToTimeSpan() - ObservationStart.ToTimeSpan()).TotalMinutes;

    /// <summary>
    /// Builds the fully-timed customer list for a counter, normalized to
    /// simulation minutes, sorted by arrival order (this is what the
    /// Simulator page replays directly - no randomness involved).
    /// </summary>
    public List<Customer> GetRecordedCustomers(CounterType counter)
    {
        var raw = counter == CounterType.FemaleCounter ? FemaleRaw : MaleRaw;

        var records = raw
            .Select(r => new
            {
                Arrival = ToMinutes(r.Arrival),
                ServiceStart = ToMinutes(r.ServiceStart),
                Exit = ToMinutes(r.Exit)
            })
            .OrderBy(r => r.Arrival)
            .ToList();

        var customers = new List<Customer>();
        double previousArrival = 0;
        for (int i = 0; i < records.Count; i++)
        {
            var r = records[i];
            customers.Add(new Customer
            {
                Id = i + 1,
                InterArrivalTime = i == 0 ? r.Arrival : r.Arrival - previousArrival,
                ArrivalTime = r.Arrival,
                ServiceStartTime = r.ServiceStart,
                ServiceTime = r.Exit - r.ServiceStart,
                ServiceEndTime = r.Exit,
                IsCompleted = true
            });
            previousArrival = r.Arrival;
        }
        return customers;
    }

    /// <summary>Recorded service durations (Exit - ServiceStart) in minutes, sorted ascending - the empirical distribution used by M/G/1.</summary>
    public List<double> GetSortedServiceTimes(CounterType counter) =>
        GetRecordedCustomers(counter).Select(c => c.ServiceTime).OrderBy(x => x).ToList();

    /// <summary>Recorded inter-arrival times in minutes (excludes the first customer, whose "inter-arrival" is just their raw arrival offset).</summary>
    public List<double> GetInterArrivalTimes(CounterType counter) =>
        GetRecordedCustomers(counter).Skip(1).Select(c => c.InterArrivalTime).ToList();

    /// <summary>Recorded inter-arrival times, sorted ascending - the empirical arrival-process distribution used by G/G/1.</summary>
    public List<double> GetSortedInterArrivalTimes(CounterType counter) =>
        GetInterArrivalTimes(counter).OrderBy(x => x).ToList();

    public double GetMeanServiceTime(CounterType counter) => StatisticsService.Mean(GetSortedServiceTimes(counter));
    public double GetServiceTimeVariance(CounterType counter) => StatisticsService.Variance(GetSortedServiceTimes(counter));
    public double GetMeanInterArrivalTime(CounterType counter) => StatisticsService.Mean(GetInterArrivalTimes(counter));
    public double GetInterArrivalVariance(CounterType counter) => StatisticsService.Variance(GetInterArrivalTimes(counter));

    /// <summary>Squared coefficient of variation of inter-arrival times: Var(A) / Mean(A)^2. Feeds Kingman's G/G/1 approximation.</summary>
    public double GetCaSquared(CounterType counter)
    {
        double mean = GetMeanInterArrivalTime(counter);
        return mean > 0 ? GetInterArrivalVariance(counter) / (mean * mean) : 0;
    }

    /// <summary>Squared coefficient of variation of service times: Var(S) / Mean(S)^2. Feeds Kingman's G/G/1 approximation.</summary>
    public double GetCsSquared(CounterType counter)
    {
        double mean = GetMeanServiceTime(counter);
        return mean > 0 ? GetServiceTimeVariance(counter) / (mean * mean) : 0;
    }

    /// <summary>Length of the field observation window in minutes (16:20-17:20).</summary>
    public const double ObservationWindowMinutes = 60.0;

    /// <summary>Ground-truth server utilization: total time the cashier spent actively serving, divided by the observation window - no queueing-model assumptions, straight from the stopwatch data.</summary>
    public double GetGroundTruthUtilization(CounterType counter) =>
        GetSortedServiceTimes(counter).Sum() / ObservationWindowMinutes;

    public double GetGroundTruthAverageWait(CounterType counter) =>
        StatisticsService.Mean(GetRecordedCustomers(counter).Select(c => c.WaitingTime).ToList());

    public string GetCounterLabel(CounterType counter) =>
        counter == CounterType.FemaleCounter ? "Female Counter (Women's Section)" : "Male Counter (Men's Section)";

    public const string ObservationWindowLabel = "16:20 - 17:20, Zellbury Outlet, Lucky One Mall, Gulberg Town, Karachi";
}
