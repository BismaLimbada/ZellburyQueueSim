using ZellburyQueueSim.Models;

namespace ZellburyQueueSim.Queueing;

/// <summary>
/// M/G/1 steady-state results via the Pollaczek-Khinchine (P-K) formula:
/// Poisson arrivals (rate lambda), one server, a general service-time
/// distribution characterized only by its mean and variance (here, the
/// empirical mean/variance of the recorded Zellbury service times), FIFO.
///
///   Wq = (lambda * E[S^2]) / (2 * (1 - rho))   where E[S^2] = Var(S) + E[S]^2
///   W  = Wq + E[S]
///   Lq = lambda * Wq            (Little's Law)
///   L  = lambda * W             (Little's Law)
///
/// M/G/1 has no simple closed form for P0 with a general service
/// distribution, so it is intentionally left null.
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
            P0 = null
        };

        if (!stable)
            return result;

        double secondMoment = serviceVariance + serviceMean * serviceMean; // E[S^2]
        result.Wq = (lambda * secondMoment) / (2 * (1 - rho));
        result.W = result.Wq + serviceMean;
        result.Lq = lambda * result.Wq;
        result.L = lambda * result.W;

        return result;
    }
}
