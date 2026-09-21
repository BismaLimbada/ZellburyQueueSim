namespace ZellburyQueueSim.Models;

/// <summary>
/// Closed-form / steady-state theoretical results for a queueing model
/// (M/M/1 exact formulas, or M/G/1 via the Pollaczek-Khinchine formula).
/// </summary>
public class TheoreticalResult
{
    public double Lambda { get; set; }
    public double Mu { get; set; }
    public double ServiceMean { get; set; }
    public double ServiceVariance { get; set; }

    public double Rho { get; set; }
    public bool IsStable { get; set; }

    /// <summary>P0: probability the system is empty (M/M/1 only; not shown for M/G/1).</summary>
    public double? P0 { get; set; }

    public double L { get; set; }   // average number of customers in the system
    public double Lq { get; set; }  // average number of customers in the queue
    public double W { get; set; }   // average time in the system
    public double Wq { get; set; }  // average waiting time in the queue
}
