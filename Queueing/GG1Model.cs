using ZellburyQueueSim.Models;

namespace ZellburyQueueSim.Queueing;

/// <summary>
/// G/G/1 has no exact closed-form solution, so this uses Kingman's formula
/// (also called the VUT equation - Variability, Utilization, Time): a
/// heuristic approximation for the average queueing delay of a single
/// server fed by a general (non-Poisson) arrival process and a general
/// service-time distribution.
///
///   Wq ~= ( (Ca^2 + Cs^2) / 2 ) * ( rho / (1 - rho) ) * (1 / mu)
///
/// where Ca^2 and Cs^2 are the squared coefficients of variation
/// (Variance / Mean^2) of the inter-arrival times and service times
/// respectively. Ca^2 = Cs^2 = 1 recovers the M/M/1 formula exactly -
/// Kingman's formula is a generalization of M/M/1, not a different model.
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
            P0 = null
        };

        if (!stable)
            return result;

        double variabilityTerm = (caSquared + csSquared) / 2.0;
        double utilizationTerm = rho / (1 - rho);
        double serviceMean = 1.0 / mu;

        result.Wq = variabilityTerm * utilizationTerm * serviceMean;
        result.W = result.Wq + serviceMean;
        result.Lq = lambda * result.Wq;   // Little's Law
        result.L = lambda * result.W;     // Little's Law

        return result;
    }
}
