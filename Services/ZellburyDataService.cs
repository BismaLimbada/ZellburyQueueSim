using ZellburyQueueSim.Models;

namespace ZellburyQueueSim.Services;

/// <summary>
/// Reference data collected by Group 11 at the Zellbury outlet, Lucky One Mall,
/// Gulberg Town, Karachi, observation window 16:20-17:20 (source workbook:
/// "Group11-data_CORRECTED.xlsx").
///
/// Two checkout counters were timed independently - women's ("Female Counter")
/// and men's ("Male Counter"). For every customer the group wrote down THREE
/// clock times (the sheet's middle column is headed "Service Time" but it holds a
/// clock time, not a duration):
///
///   Arrival Time  = the customer JOINS THE QUEUE
///   Service Start = the customer REACHES THE COUNTER (billing begins)
///   Exit Time     = the customer FINISHES BILLING and leaves
///
/// From these, per customer (all in minutes):
///
///   Waiting Time = Service Start - Arrival
///   Service Time = Exit          - Service Start
///   Total Time   = Exit          - Arrival      (= Waiting + Service)
///
/// Nothing is derived or re-paired any more: every number comes straight from the
/// recorded timestamps. Times were noted to the whole minute, so a value of 0 means
/// "under a minute". All clock values are normalized to minutes since the window
/// opened at 16:20, so ObservationStart = 0.
///
/// <see cref="GetIssues"/> validates the timestamps. Records that fall outside the
/// stated window are KEPT (dropping completed observations would bias the waiting
/// and service statistics) but are flagged, and the observation horizon used for
/// time-averaged quantities is extended to cover them (see <see cref="GetObservationHorizon"/>).
/// </summary>
public class ZellburyDataService
{
    private static readonly TimeOnly ObservationStart = new(16, 20);

    /// <summary>Length of the stated observation window in minutes (16:20-17:20).</summary>
    public const double ObservationWindowMinutes = 60.0;

    // Raw (Arrival, ServiceStart, Exit) triples exactly as recorded, in sheet order.
    private static readonly (TimeOnly Arrival, TimeOnly ServiceStart, TimeOnly Exit)[] FemaleRaw =
    {
        (new(16,30), new(16,30), new(16,31)),
        (new(16,32), new(16,32), new(16,36)),
        (new(16,37), new(16,37), new(16,38)),
        (new(16,38), new(16,38), new(16,39)),
        (new(16,38), new(16,39), new(16,44)),
        (new(16,39), new(16,44), new(16,44)),
        (new(16,42), new(16,44), new(16,45)),
        (new(16,43), new(16,45), new(16,46)),
        (new(16,46), new(16,46), new(16,49)),
        (new(16,46), new(16,52), new(16,54)),
        (new(16,50), new(16,50), new(16,52)),
        (new(16,55), new(16,55), new(16,57)),
        (new(16,57), new(16,57), new(16,59)),
        (new(16,59), new(16,59), new(17, 1)),
        (new(17, 2), new(17, 2), new(17, 6)),
        (new(17, 3), new(17, 6), new(17, 8)),
        (new(17, 4), new(17, 8), new(17, 8)),
        (new(17, 6), new(17, 8), new(17, 9)),
        (new(17,10), new(17,10), new(17,13)),
        (new(17,12), new(17,13), new(17,15)),
        (new(17,13), new(17,15), new(17,15)),
        (new(17,14), new(17,15), new(17,16)),
    };

    private static readonly (TimeOnly Arrival, TimeOnly ServiceStart, TimeOnly Exit)[] MaleRaw =
    {
        (new(16,27), new(16,27), new(16,28)),
        (new(16,30), new(16,30), new(16,31)),
        (new(16,40), new(16,40), new(16,42)),
        (new(16,41), new(16,42), new(16,43)),
        (new(16,42), new(16,43), new(16,45)),
        (new(16,45), new(16,45), new(16,47)),
        (new(16,45), new(16,47), new(16,49)),
        (new(16,50), new(16,50), new(16,53)),
        (new(16,50), new(16,55), new(16,56)),
        (new(16,54), new(16,54), new(16,55)),
        (new(16,55), new(16,56), new(16,59)),
        (new(16,58), new(16,59), new(16,59)),
        (new(17, 1), new(17, 1), new(17, 3)),
        (new(17, 2), new(17, 3), new(17, 5)),
        (new(17, 9), new(17, 9), new(17,11)),
        (new(17,15), new(17,15), new(17,16)),
        (new(17,19), new(17,19), new(17,19)),
        (new(17,19), new(17,19), new(17,21)),
        (new(17,19), new(17,21), new(17,22)),
        (new(17,21), new(17,22), new(17,23)),
    };

    private static double ToMinutes(TimeOnly t) => (t.ToTimeSpan() - ObservationStart.ToTimeSpan()).TotalMinutes;

    private static (TimeOnly Arrival, TimeOnly ServiceStart, TimeOnly Exit)[] RawFor(CounterType counter) =>
        counter == CounterType.FemaleCounter ? FemaleRaw : MaleRaw;

    /// <summary>Formats a simulation-minute value (0 = 16:20) as a clock time, e.g. 63 -> "17:23".</summary>
    public static string ToClock(double minutes) => ObservationStart.AddMinutes(minutes).ToString("HH:mm");

    /// <summary>
    /// The recorded customers for a counter, in arrival order, with times in simulation
    /// minutes. Waiting, service and total time are read directly off the timestamps
    /// (see the class summary) - there is no derivation or re-pairing.
    /// The first customer's InterArrivalTime is their arrival offset from the window start;
    /// it is not a real inter-arrival gap and is excluded from every inter-arrival statistic.
    /// </summary>
    public List<Customer> GetRecordedCustomers(CounterType counter)
    {
        var raw = RawFor(counter);
        var customers = new List<Customer>();
        for (int i = 0; i < raw.Length; i++)
        {
            double arrival = ToMinutes(raw[i].Arrival);
            double start = ToMinutes(raw[i].ServiceStart);
            double end = ToMinutes(raw[i].Exit);
            customers.Add(new Customer
            {
                Id = i + 1,
                InterArrivalTime = i == 0 ? arrival : arrival - ToMinutes(raw[i - 1].Arrival),
                ArrivalTime = arrival,
                ServiceStartTime = start,
                ServiceTime = end - start,
                ServiceEndTime = end,
                IsCompleted = true
            });
        }
        return customers;
    }

    /// <summary>
    /// Observation horizon in minutes: the stated 60-minute window, or the last recorded exit
    /// if that is later (the men's counter's last customers leave at 17:23 = minute 63).
    /// Utilization and time-averaged queue length are measured over this horizon so that the
    /// busy time of a customer who finishes after 17:20 is not divided by too short a period.
    /// </summary>
    public double GetObservationHorizon(CounterType counter) =>
        Math.Max(ObservationWindowMinutes, RawFor(counter).Max(r => ToMinutes(r.Exit)));

    /// <summary>Service durations in minutes (Exit - Service Start), sorted ascending - the empirical distribution used by M/G/1 and G/G/1.</summary>
    public List<double> GetSortedServiceTimes(CounterType counter) =>
        GetRecordedCustomers(counter).Select(c => c.ServiceTime).OrderBy(x => x).ToList();

    /// <summary>Recorded inter-arrival times in minutes, in arrival order (excludes the first customer, whose "inter-arrival" is just their arrival offset).</summary>
    public List<double> GetInterArrivalTimes(CounterType counter) =>
        GetRecordedCustomers(counter).Skip(1).Select(c => c.InterArrivalTime).ToList();

    /// <summary>Recorded inter-arrival times, sorted ascending - the empirical arrival-process distribution used by G/G/1.</summary>
    public List<double> GetSortedInterArrivalTimes(CounterType counter) =>
        GetInterArrivalTimes(counter).OrderBy(x => x).ToList();

    /// <summary>Recorded service durations in arrival order (for independence checks).</summary>
    public List<double> GetServiceTimesInOrder(CounterType counter) =>
        GetRecordedCustomers(counter).Select(c => c.ServiceTime).ToList();

    /// <summary>Ground-truth server utilization: total recorded service time / observation horizon - no queueing-model assumptions, straight from the timestamps.</summary>
    public double GetGroundTruthUtilization(CounterType counter) =>
        GetSortedServiceTimes(counter).Sum() / GetObservationHorizon(counter);

    /// <summary>Mean of (Service Start - Arrival) over the recorded customers.</summary>
    public double GetGroundTruthAverageWait(CounterType counter) =>
        StatisticsService.Mean(GetRecordedCustomers(counter).Select(c => c.WaitingTime).ToList());

    public string GetCounterLabel(CounterType counter) =>
        counter == CounterType.FemaleCounter ? "Female Counter (Women's Section)" : "Male Counter (Men's Section)";

    // ------------------------------------------------------------------
    // Timestamp validation
    // ------------------------------------------------------------------

    /// <summary>
    /// Validates the recorded timestamps for one counter. Checks, in order:
    ///   1. Arrival &lt;= Service Start &lt;= Exit for every row          (Error if violated)
    ///   2. Rows not in non-decreasing arrival order                    (Warning)
    ///   3. Two services overlapping in time (single server!)           (Error)
    ///   4. Any time outside the stated 16:20-17:20 window              (Warning, row kept)
    ///   5. A customer served after a later arrival, with the cashier idle while they waited (Warning)
    ///   6. Zero-minute services (whole-minute resolution)              (Info)
    /// Nothing is silently dropped or "fixed"; the data is used exactly as recorded.
    /// </summary>
    public List<DataIssue> GetIssues(CounterType counter)
    {
        var issues = new List<DataIssue>();
        var rows = GetRecordedCustomers(counter);
        int n = rows.Count;

        // 1. per-row ordering
        foreach (var c in rows)
        {
            if (c.ServiceStartTime < c.ArrivalTime)
                issues.Add(new DataIssue(IssueSeverity.Error, c.Id, $"Service Start {ToClock(c.ServiceStartTime)} is before Arrival {ToClock(c.ArrivalTime)} (negative waiting time)."));
            if (c.ServiceEndTime < c.ServiceStartTime)
                issues.Add(new DataIssue(IssueSeverity.Error, c.Id, $"Exit {ToClock(c.ServiceEndTime)} is before Service Start {ToClock(c.ServiceStartTime)} (negative service time)."));
        }

        // 2. arrival order
        for (int i = 1; i < n; i++)
            if (rows[i].ArrivalTime < rows[i - 1].ArrivalTime)
                issues.Add(new DataIssue(IssueSeverity.Warning, rows[i].Id, $"Arrival {ToClock(rows[i].ArrivalTime)} is earlier than the previous row's arrival {ToClock(rows[i - 1].ArrivalTime)}; rows are not in arrival order."));

        // 3. single-server overlap, and 5. idle-while-waiting (both look at service order)
        var byStart = rows.OrderBy(c => c.ServiceStartTime).ThenBy(c => c.ServiceEndTime).ThenBy(c => c.Id).ToList();
        for (int k = 1; k < n; k++)
        {
            var prev = byStart[k - 1];
            var cur = byStart[k];

            if (cur.ServiceStartTime < prev.ServiceEndTime)
                issues.Add(new DataIssue(IssueSeverity.Error, cur.Id, $"Service starts {ToClock(cur.ServiceStartTime)} before customer {prev.Id} has finished at {ToClock(prev.ServiceEndTime)}; one cashier cannot serve two customers at once."));

            if (cur.ServiceStartTime > prev.ServiceEndTime)
            {
                // Cashier idle from prev.End to cur.Start. Was anyone already waiting at prev.End?
                var waitingThen = byStart.Skip(k)
                    .Where(c => c.ArrivalTime <= prev.ServiceEndTime)
                    .Select(c => c.Id).ToList();
                if (waitingThen.Count > 0)
                    issues.Add(new DataIssue(IssueSeverity.Warning, waitingThen[0],
                        $"Cashier idle {ToClock(prev.ServiceEndTime)}-{ToClock(cur.ServiceStartTime)} although customer {waitingThen[0]} " +
                        $"(arrived {ToClock(rows[waitingThen[0] - 1].ArrivalTime)}) was already waiting; a later arrival (customer {cur.Id}) was served first. " +
                        "Possible queue-jumping, a customer who stepped away, or a mistyped time. Recorded value used as-is.",
                        DataIssueCodes.IdleWhileWaiting));
            }
        }

        // 4. outside the stated window
        foreach (var c in rows)
        {
            var parts = new List<string>();
            if (c.ArrivalTime < 0 || c.ArrivalTime > ObservationWindowMinutes) parts.Add($"Arrival {ToClock(c.ArrivalTime)}");
            if (c.ServiceStartTime < 0 || c.ServiceStartTime > ObservationWindowMinutes) parts.Add($"Service Start {ToClock(c.ServiceStartTime)}");
            if (c.ServiceEndTime < 0 || c.ServiceEndTime > ObservationWindowMinutes) parts.Add($"Exit {ToClock(c.ServiceEndTime)}");
            if (parts.Count > 0)
                issues.Add(new DataIssue(IssueSeverity.Warning, c.Id, $"{string.Join(", ", parts)} falls outside the stated window 16:20-17:20."));
        }

        var spill = rows.Where(c => c.ArrivalTime > ObservationWindowMinutes || c.ServiceStartTime > ObservationWindowMinutes || c.ServiceEndTime > ObservationWindowMinutes).ToList();
        if (spill.Count > 0)
        {
            double horizon = GetObservationHorizon(counter);
            bool lateArrival = spill.Any(c => c.ArrivalTime > ObservationWindowMinutes);
            bool consistent = !issues.Any(i => i.Severity == IssueSeverity.Error);
            issues.Add(new DataIssue(IssueSeverity.Info, null,
                $"Investigation: {spill.Count} record(s) (customers {string.Join(", ", spill.Select(c => c.Id))}) run past 17:20; the latest exit is {ToClock(horizon)} (minute {horizon:0}). " +
                (consistent
                    ? "Every row is internally consistent (Arrival <= Service Start <= Exit, no overlapping services), so this is a continuous busy period that ran on after the window closed, not an isolated mistyped time. "
                    : "Some rows also fail the consistency checks above, so a transcription error is possible. ") +
                (lateArrival
                    ? "At least one of these customers joined the queue after 17:20, i.e. it was observed outside the window, not just finished late. "
                    : "All of these customers joined the queue by 17:20 and were simply still being billed when the window closed. ") +
                $"The records are kept and the observation horizon is extended from {ObservationWindowMinutes:0} to {horizon:0} minutes for utilization and queue-length averages."));
        }

        // 6. zero-minute services
        int zero = rows.Count(c => c.ServiceTime == 0);
        if (zero > 0)
            issues.Add(new DataIssue(IssueSeverity.Info, null, $"{zero} customer(s) have Service Start = Exit (service time 0): times were recorded to the whole minute, so these took under a minute."));

        return issues;
    }
}
