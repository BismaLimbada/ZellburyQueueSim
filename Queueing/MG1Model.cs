using ZellburyQueueSim.Models;

namespace ZellburyQueueSim.Queueing;

/// <summary>
/// M/G/1 steady-state results (Poisson arrivals, one server, a general
/// service-time distribution described only by its mean 1/mu and variance
/// sigma_s^2), computed in the same order as the course notes:
///
///   rho = lambda * E[S] = lambda / mu
///   P0  = 1 - rho                  (probability the system is empty = fraction of time the server is idle)
///   Lq  = (lambda^2 * sigma_s^2 + rho^2) / (2 * (1 - rho))
///   Wq  = Lq / lambda
///   W   = Wq + 1/mu
///   L   = lambda * W
///
/// (For a Uniform(min, max) service time, sigma_s^2 = (max - min)^2 / 12.)
/// </summary>
public static class MG1Model
{
    public static TheoreticalResult Compute(double lambda, double serviceMean, double serviceVariance)
    {
        double rho = lambda * serviceMean;
        bool stable = rho < 1.0;

        var result = new TheoreticalResult
        {
            Lambda = lambda,
            Mu = serviceMean > 0 ? 1.0 / serviceMean : 0,
            ServiceMean = serviceMean,
            ServiceVariance = serviceVariance,
            Rho = rho,
            IsStable = stable,
            P0 = stable ? (double?)(1 - rho) : null   // P0 = 1 - rho for every stable M/G/1 queue
        };

        if (!stable || lambda <= 0)
            return result;

        result.Lq = (lambda * lambda * serviceVariance + rho * rho) / (2 * (1 - rho));
        result.Wq = result.Lq / lambda;
        result.W = result.Wq + serviceMean;
        result.L = lambda * result.W;

        return result;
    }
}
