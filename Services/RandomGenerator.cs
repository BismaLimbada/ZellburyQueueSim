namespace ZellburyQueueSim.Services;

/// <summary>
/// Seeded random-variate generator used throughout the simulation.
///
/// Wraps System.Random with a fixed seed so that every simulation run is
/// fully reproducible: the same seed always produces the same stream of
/// arrivals and service times, which is required to compare configurations
/// fairly and to re-run a scenario.
/// </summary>
public class RandomGenerator
{
    private Random _random;
    public long Seed { get; private set; }

    public RandomGenerator()
    {
        Seed = Environment.TickCount64;
        _random = new Random((int)Seed);
    }

    /// <summary>Re-seeds the generator so a fresh, reproducible stream starts from this point.</summary>
    public void Reseed(long seed)
    {
        Seed = seed;
        _random = new Random((int)seed);
    }

    /// <summary>
    /// Uniform(0,1) draw, open interval, using 1 - NextDouble() so the value
    /// is never exactly 0 (Math.Log(0) would be -Infinity, which would break
    /// the exponential inverse-transform below).
    /// </summary>
    public double NextUniform() => 1.0 - _random.NextDouble();

    /// <summary>
    /// Exponential(rate) variate via the inverse-transform method:
    /// for U ~ Uniform(0,1), X = -ln(U) / rate ~ Exponential(rate).
    /// This is exactly how Poisson-process inter-arrival times and
    /// exponential service times are generated for M/M/1.
    /// </summary>
    public double NextExponential(double rate)
    {
        if (rate <= 0) throw new ArgumentOutOfRangeException(nameof(rate), "Rate must be positive.");
        double u = NextUniform();
        return -Math.Log(u) / rate;
    }

    /// <summary>
    /// Next inter-arrival time for a Poisson arrival process with mean rate
    /// lambda (customers per minute). Inter-arrival times of a Poisson
    /// process are themselves Exponential(lambda) distributed.
    /// </summary>
    public double NextPoissonInterArrival(double lambda) => NextExponential(lambda);

    /// <summary>
    /// Draws a variate from the empirical distribution defined by
    /// <paramref name="sortedSample"/> using bootstrap resampling: pick one
    /// of the recorded values uniformly at random and return it exactly.
    /// This is the "general" service-time distribution used by M/G/1, built
    /// directly from the recorded Zellbury service times rather than an
    /// assumed theoretical shape.
    ///
    /// An earlier version of this method interpolated between sorted
    /// neighbouring values instead. That gave the two extreme recorded
    /// values (the smallest and the largest) only half as much weight as
    /// every value in between, which pulled the long-run average of the
    /// generated samples slightly away from the true sample mean. Picking
    /// a recorded value directly removes that bias: every recorded value
    /// has exactly the same 1/n chance of being drawn, so the long-run
    /// average of the generated samples converges to the real sample mean.
    /// </summary>
    public double NextEmpirical(IReadOnlyList<double> sortedSample)
    {
        if (sortedSample.Count == 0)
            throw new ArgumentException("Empirical sample cannot be empty.", nameof(sortedSample));

        int n = sortedSample.Count;
        int index = (int)(NextUniform() * n);
        if (index >= n) index = n - 1; // guard the (extremely rare) u == 1.0 edge case

        return sortedSample[index];
    }
}
