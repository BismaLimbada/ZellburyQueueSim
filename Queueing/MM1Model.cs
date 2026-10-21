using ZellburyQueueSim.Models;

namespace ZellburyQueueSim.Queueing;

/// <summary>
/// Steady-state formulas for the M/M/1 queue (Poisson arrivals at rate
/// lambda, exponential service at rate mu, one server, FIFO), computed in
/// the same order as the course notes:
///
///   rho = lambda / mu
///   Lq  = rho^2 / (1 - rho)
///   Wq  = Lq / lambda
///   W   = Wq + 1/mu
///   L   = lambda * W
/// </summary>
public static class MM1Model
{
    public static TheoreticalResult Compute(double lambda, double mu)
    {
        double rho = mu > 0 ? lambda / mu : double.PositiveInfinity;
        bool stable = rho < 1.0;

        var result = new TheoreticalResult
        {
            Lambda = lambda,
            Mu = mu,
            ServiceMean = mu > 0 ? 1.0 / mu : 0,
            ServiceVariance = mu > 0 ? 1.0 / (mu * mu) : 0, // Var of Exponential(mu) = 1/mu^2
            Rho = rho,
            IsStable = stable,
            P0 = stable ? (double?)(1 - rho) : null   // P0 = idle time = 1 - rho whenever a steady state exists
        };

        if (!stable || lambda <= 0)
            return result;                      // no steady state (or no arrivals): Lq, Wq, W, L stay at 0

        result.Lq = (rho * rho) / (1 - rho);
        result.Wq = result.Lq / lambda;
        result.W = result.Wq + 1.0 / mu;
        result.L = lambda * result.W;

        return result;
    }
}
