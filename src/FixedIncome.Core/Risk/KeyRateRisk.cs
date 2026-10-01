namespace fixed_income_pricing.Risk;

public sealed record KeyRateBucket(double Tenor, double Dv01);

public sealed record KeyRateRisk(string InstrumentId, string CurveName, IReadOnlyList<KeyRateBucket> Buckets)
{
    public double TotalDv01 => Buckets.Sum(b => b.Dv01);
}

public static class KeyRateReport
{
    // Σ quantity · DV01, by curve and tenor
    public static IReadOnlyDictionary<(string Curve, double Tenor), double> Aggregate(
        IEnumerable<(double Quantity, KeyRateRisk Risk)> positions) =>
        positions
            .SelectMany(p => p.Risk.Buckets.Select(b => (Key: (p.Risk.CurveName, b.Tenor), Dv01: p.Quantity * b.Dv01)))
            .GroupBy(x => x.Key)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Dv01));

    public static IReadOnlyDictionary<string, double> ByCurve(IReadOnlyDictionary<(string Curve, double Tenor), double> report) =>
        report.GroupBy(kv => kv.Key.Curve).ToDictionary(g => g.Key, g => g.Sum(kv => kv.Value));
}
