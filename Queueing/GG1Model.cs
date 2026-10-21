using ZellburyQueueSim.Models;

namespace ZellburyQueueSim.Queueing;

/// <summary>
/// G/G/1 approximation (general arrivals, general service, one server),
/// computed in the same order as the course notes:
///
///   rho = lambda / mu
///   Ca^2 = sigma_a^2 / (1/lambda)^2        Cs^2 = sigma_s^2 / (1/mu)^2
///
///               rho^2 (1 + Cs^2) (Ca^2 + rho^2 Cs^2)
///   Lq  ~=  ---------------------------------------------
///               2 (1 - rho) (1 + rho^2 Cs^2)
///
///   Wq  = Lq / lambda
///   W   = Wq + 1/mu
///   L   = lambda * W
///   Idle time = 1 - rho
///
/// Ca^2 and Cs^2 are the squared coefficients of variation
/// (Variance / Mean^2) of the inter-arrival and service times.
/// </summary>
public static class GG1Model
{
    public static TheoreticalResult Compute(double lambda, double mu, double caSquared, double csSquared)
    {
        double rho = mu > 0 ? lambda / mu : double.PositiveInfinity;
        bool stable = rho < 1.0;

        var result = new TheoreticalResult
        {
            Lambda = lambda,
            Mu = mu,
            ServiceMean = mu > 0 ? 1.0 / mu : 0,
            Rho = rho,
            IsStable = stable,
            P0 = stable ? (double?)(1 - rho) : null   // idle time = 1 - rho whenever a steady state exists
        };

        if (!stable || lambda <= 0)
            return result;

        double rho2 = rho * rho;
        double numerator = rho2 * (1 + csSquared) * (caSquared + rho2 * csSquared);
        double denominator = 2 * (1 - rho) * (1 + rho2 * csSquared);

        result.Lq = numerator / denominator;
        result.Wq = result.Lq / lambda;
        result.W = result.Wq + 1.0 / mu;
        result.L = lambda * result.W;

        return result;
    }
}
