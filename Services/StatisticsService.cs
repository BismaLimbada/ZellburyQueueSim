namespace ZellburyQueueSim.Services;

public record KsTestResult(double Statistic, double CriticalValueAt05, bool RejectsNullAt05, int SampleSize);
public record ChiSquareTestResult(double Statistic, int DegreesOfFreedom, double CriticalValueAt05, bool RejectsNullAt05, int Bins);
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

    public static double StdDev(IReadOnlyList<double> data) => Math.Sqrt(Variance(data));

    /// <summary>
    /// One-sample Kolmogorov-Smirnov test comparing an empirical sample
    /// against a fitted exponential(rate) CDF. Used to check whether the
    /// recorded inter-arrival times (or service times) are plausibly
    /// exponential, as required for the M/M/1 assumption.
    /// Critical value approximation: 1.36 / sqrt(n) (alpha = 0.05, n large).
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

        double critical = n > 0 ? 1.36 / Math.Sqrt(n) : double.PositiveInfinity;
        return new KsTestResult(maxDiff, critical, maxDiff > critical, n);
    }

    /// <summary>
    /// Chi-square goodness-of-fit test comparing the sample's histogram
    /// (equal-width bins) against the frequencies expected under a fitted
    /// exponential(rate) distribution.
    /// </summary>
    public static ChiSquareTestResult ChiSquareAgainstExponential(IReadOnlyList<double> sample, double rate, int bins = 5)
    {
        if (sample.Count == 0 || bins < 2)
            return new ChiSquareTestResult(0, 0, 0, false, bins);

        double max = sample.Max();
        double binWidth = max / bins;
        if (binWidth <= 0) binWidth = 1;

        var observed = new int[bins];
        foreach (var x in sample)
        {
            int idx = Math.Min((int)(x / binWidth), bins - 1);
            observed[idx]++;
        }

        double statistic = 0;
        int usableBins = 0;
        for (int i = 0; i < bins; i++)
        {
            double lower = i * binWidth;
            double upper = (i == bins - 1) ? double.PositiveInfinity : (i + 1) * binWidth;
            double pLower = 1 - Math.Exp(-rate * lower);
            double pUpper = double.IsPositiveInfinity(upper) ? 1.0 : 1 - Math.Exp(-rate * upper);
            double expected = (pUpper - pLower) * sample.Count;

            if (expected < 1e-6) continue;
            usableBins++;
            statistic += Math.Pow(observed[i] - expected, 2) / expected;
        }

        int df = Math.Max(usableBins - 1 - 1, 1); // -1 for total, -1 for the estimated rate parameter
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
