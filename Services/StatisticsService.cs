namespace ZellburyQueueSim.Services;

public record KsTestResult(double Statistic, double CriticalValueAt05, bool RejectsNullAt05, int SampleSize);
/// <param name="Applicable">False when the sample is too small for the test (see <see cref="StatisticsService.ChiSquareAgainstExponential"/>); the other fields are then meaningless.</param>
public record ChiSquareTestResult(double Statistic, int DegreesOfFreedom, double CriticalValueAt05, bool RejectsNullAt05, int Bins, bool Applicable = true);
public record IndependenceTestResult(double Lag1Autocorrelation, double ApproxCriticalValueAt05, bool SuggestsDependence, int SampleSize);

/// <summary>
/// Descriptive statistics plus the statistical validation tests used to
/// justify the distributional assumptions behind the queueing models
/// (e.g. checking whether the recorded inter-arrival times are consistent
/// with a Poisson/exponential process, and whether the empirical service
/// sample used for M/G/1 is internally independent).
/// </summary>
public class StatisticsService
{
    public static double Mean(IReadOnlyList<double> data) => data.Count == 0 ? 0 : data.Average();

    public static double Variance(IReadOnlyList<double> data)
    {
        if (data.Count < 2) return 0;
        double mean = Mean(data);
        double sumSq = data.Sum(x => (x - mean) * (x - mean));
        return sumSq / (data.Count - 1); // sample variance
    }

    /// <summary>
    /// One-sample Kolmogorov-Smirnov test comparing an empirical sample
    /// against an exponential(rate) CDF whose rate was ESTIMATED from the same
    /// sample (rate = 1 / sample mean). Statistic D = max |F_n(x) - F(x)|.
    ///
    /// Because the rate is estimated, the classic critical value 1.36 / sqrt(n)
    /// (valid only for a fully specified distribution) is far too lenient: a Monte
    /// Carlo check at n = 19..22 showed it rejects a truly exponential sample only
    /// ~0.3% of the time instead of 5%. This method therefore uses the
    /// D'Agostino &amp; Stephens (1986) modification for an exponential with estimated
    /// scale: reject at alpha = 0.05 when
    ///     (D - 0.2/n) * (sqrt(n) + 0.26 + 0.5/sqrt(n))  &gt;  1.094,
    /// reported here as the equivalent critical value of D itself. (Monte Carlo
    /// size at n = 19..22 is 4.6-4.8%.) Times recorded to the whole minute contain
    /// many ties, so the test is approximate for this data.
    /// </summary>
    public static KsTestResult KsTestAgainstExponential(IReadOnlyList<double> sample, double rate)
    {
        var sorted = sample.OrderBy(x => x).ToArray();
        int n = sorted.Length;
        double maxDiff = 0;

        for (int i = 0; i < n; i++)
        {
            double empiricalCdfBefore = (double)i / n;
            double empiricalCdfAfter = (double)(i + 1) / n;
            double theoreticalCdf = 1 - Math.Exp(-rate * sorted[i]);

            maxDiff = Math.Max(maxDiff, Math.Abs(theoreticalCdf - empiricalCdfBefore));
            maxDiff = Math.Max(maxDiff, Math.Abs(theoreticalCdf - empiricalCdfAfter));
        }

        double critical = double.PositiveInfinity;
        if (n > 0)
        {
            double rootN = Math.Sqrt(n);
            critical = 1.094 / (rootN + 0.26 + 0.5 / rootN) + 0.2 / n;
        }
        return new KsTestResult(maxDiff, critical, maxDiff > critical, n);
    }

    /// <summary>Minimum expected count per bin for the chi-square approximation to be trusted (the usual "at least 5" rule).</summary>
    private const int MinExpectedPerBin = 5;

    /// <summary>
    /// Chi-square goodness-of-fit test against an exponential(rate) distribution whose
    /// rate was estimated from the sample. Uses EQUAL-PROBABILITY bins under the fitted
    /// distribution (bin edges at the i/k quantiles), so every bin has expected count n/k.
    /// The number of bins is k = min(<paramref name="maxBins"/>, n / 5), which keeps the
    /// expected count per bin at 5 or more. Statistic = sum (O - E)^2 / E with
    /// df = k - 1 - 1 (one for the total, one for the estimated rate).
    ///
    /// Needs k &gt;= 3 (so df &gt;= 1), i.e. at least 15 observations; otherwise the result
    /// has <c>Applicable = false</c>. (The earlier version used equal-width bins out to the
    /// sample maximum, which left most bins with expected counts of 1-3 at n of about 20.)
    /// Recorded values are whole minutes, so ties make this approximate for this data.
    /// </summary>
    public static ChiSquareTestResult ChiSquareAgainstExponential(IReadOnlyList<double> sample, double rate, int maxBins = 10)
    {
        int n = sample.Count;
        int bins = Math.Min(maxBins, n / MinExpectedPerBin);
        if (rate <= 0 || bins < 3)
            return new ChiSquareTestResult(0, 0, 0, false, Math.Max(bins, 0), false);

        var observed = new int[bins];
        foreach (var x in sample)
        {
            double u = 1 - Math.Exp(-rate * x);              // fitted CDF value in [0, 1)
            int idx = Math.Clamp((int)(u * bins), 0, bins - 1);
            observed[idx]++;
        }

        double expected = (double)n / bins;
        double statistic = 0;
        foreach (int o in observed)
            statistic += (o - expected) * (o - expected) / expected;

        int df = bins - 2;
        double critical = ChiSquareCriticalValue05(df);
        return new ChiSquareTestResult(statistic, df, critical, statistic > critical, bins);
    }

    /// <summary>
    /// Lag-1 autocorrelation of a sequence, used as an independence check:
    /// values close to 0 are consistent with an independent (uncorrelated)
    /// sequence of inter-arrival or service times, as the models assume.
    /// Approximate 95% critical value for "no autocorrelation" is
    /// 1.96 / sqrt(n).
    /// </summary>
    public static IndependenceTestResult Lag1Independence(IReadOnlyList<double> sequence)
    {
        int n = sequence.Count;
        if (n < 3) return new IndependenceTestResult(0, 1, false, n);

        double mean = Mean(sequence);
        double num = 0, den = 0;
        for (int i = 0; i < n - 1; i++)
            num += (sequence[i] - mean) * (sequence[i + 1] - mean);
        for (int i = 0; i < n; i++)
            den += Math.Pow(sequence[i] - mean, 2);

        double r1 = den == 0 ? 0 : num / den;
        double critical = 1.96 / Math.Sqrt(n);
        return new IndependenceTestResult(r1, critical, Math.Abs(r1) > critical, n);
    }

    /// <summary>
    /// Small lookup table of chi-square critical values at alpha = 0.05 for
    /// the (small) degrees-of-freedom range this app can produce, with a
    /// Wilson-Hilferty approximation fallback for anything larger.
    /// </summary>
    private static double ChiSquareCriticalValue05(int df)
    {
        var table = new Dictionary<int, double>
        {
            { 1, 3.841 }, { 2, 5.991 }, { 3, 7.815 }, { 4, 9.488 },
            { 5, 11.070 }, { 6, 12.592 }, { 7, 14.067 }, { 8, 15.507 },
            { 9, 16.919 }, { 10, 18.307 }
        };
        if (table.TryGetValue(df, out var v)) return v;

        // Wilson-Hilferty approximation for larger df.
        double z = 1.645; // z_0.05
        double term = 1 - 2.0 / (9 * df) + z * Math.Sqrt(2.0 / (9 * df));
        return df * Math.Pow(term, 3);
    }
}
