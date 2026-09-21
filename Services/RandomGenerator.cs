namespace ZellburyQueueSim.Services;

/// <summary>
/// Seeded random-variate generator used throughout the simulation.
///
/// Wraps System.Random (a linear-congruential-family PRNG) with a fixed
/// seed so that every simulation run is fully reproducible: the same seed
/// always produces the same stream of arrivals and service times, which is
/// required to compare configurations fairly and to re-run a scenario.
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

    public double NextUniform(double min, double max) => min + (max - min) * NextUniform();

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
    /// <paramref name="sortedSample"/> (must already be sorted ascending)
    /// using inverse-transform sampling on the piecewise-linear empirical
    /// CDF: a uniform U picks a position along the empirical CDF and we
    /// linearly interpolate between the two bracketing order statistics.
    /// This is the "general" service-time distribution used by M/G/1,
    /// built directly from the recorded Zellbury service times rather than
    /// an assumed theoretical shape.
    /// </summary>
    public double NextEmpirical(IReadOnlyList<double> sortedSample)
    {
        if (sortedSample.Count == 0)
            throw new ArgumentException("Empirical sample cannot be empty.", nameof(sortedSample));
        if (sortedSample.Count == 1)
            return sortedSample[0];

        double u = NextUniform();
        int n = sortedSample.Count;

        // Position along the empirical CDF, scaled to the index range [0, n-1].
        double position = u * (n - 1);
        int lowerIndex = (int)Math.Floor(position);
        int upperIndex = Math.Min(lowerIndex + 1, n - 1);
        double fraction = position - lowerIndex;

        double value = sortedSample[lowerIndex] + fraction * (sortedSample[upperIndex] - sortedSample[lowerIndex]);
        return Math.Max(value, 0.01); // guard against a zero-duration service event
    }
}
