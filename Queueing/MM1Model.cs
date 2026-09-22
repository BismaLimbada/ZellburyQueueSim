using ZellburyQueueSim.Models;

namespace ZellburyQueueSim.Queueing;

/// <summary>
/// Exact steady-state formulas for the M/M/1 queue (Poisson arrivals at
/// rate lambda, exponential service at rate mu, one server, FIFO,
/// infinite capacity/population).
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
            IsStable = stable
        };

        if (!stable)
        {
            // Steady state does not exist; leave metrics at zero/undefined.
            result.P0 = 0;
            return result;
        }

        result.P0 = 1 - rho;
        result.L = rho / (1 - rho);
        result.Lq = (rho * rho) / (1 - rho);
        result.W = 1.0 / (mu - lambda);
        result.Wq = rho / (mu - lambda);

        return result;
    }
}
