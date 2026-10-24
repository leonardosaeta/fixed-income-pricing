namespace FixedIncome.Derivatives.Volatility;

public class GridVolSurface:IVolSurface
{
    private readonly double[] _expiries;
    private readonly double[] _logMoneyness;
    private readonly double[][] _vols;

    public GridVolSurface(double[] expiries, double[] logMoneyss, double[][] vols)
    {
        if (expiries.Length == 0 || logMoneyss.Length == 0) throw new ArgumentException("grid cannot be empty");
        if (vols.Length != expiries.Length || vols.Any(row => row.Length != logMoneyss.Length))
            throw new ArgumentException("vols must have one row per expiry and one column per strike");
        if (!StrictlyIncreasing(expiries) || expiries[0] <= 0)
            throw new ArgumentException("expiries must be positive and increasing");
        if (!StrictlyIncreasing(logMoneyss)) throw new ArgumentException("logMoneyss must be positive and increasing");
        if (vols.Any(row => row.Any(v => v <= 0))) throw new ArgumentException("vols must be positive");


        for (int j = 0; j < logMoneyss.Length; j++)
        for (int i = 1; i < expiries.Length; i++)
            if (TotalVariance(vols[i][j], expiries[i]) < TotalVariance(vols[i - 1][j], expiries[i - 1]))
                throw new ArgumentException(
                    $"calendar arbitrage: total variance falls between expiries {expiries[i - 1]} and {expiries[i]} at log-moneyness {logMoneyss[j]}");

        _expiries = expiries;
        _logMoneyness = logMoneyss;
        _vols = vols;
    }
    
    public double Volatility(double timeToExpiry, double logMoneyness)
    {
        if (timeToExpiry <= _expiries[0]) return SliceVol(0, logMoneyness);
        if (timeToExpiry >= _expiries[^1]) return SliceVol(_expiries.Length - 1, logMoneyness);

        int upper = Array.FindIndex(_expiries, e => e >= timeToExpiry);
        int lower = upper - 1;

        double wLower = TotalVariance(SliceVol(lower, logMoneyness), _expiries[lower]);
        double wUpper = TotalVariance(SliceVol(upper, logMoneyness), _expiries[upper]);
        double weight = (timeToExpiry - _expiries[lower]) / (_expiries[upper] - _expiries[lower]);

        return Math.Sqrt((wLower + weight * (wUpper - wLower)) / timeToExpiry);
    }
    
    private double SliceVol(int expiryIndex, double k)
    {
        var row = _vols[expiryIndex];
        if (k <= _logMoneyness[0]) return row[0];
        if (k >= _logMoneyness[^1]) return row[^1];

        int upper = Array.FindIndex(_logMoneyness, x => x >= k);
        int lower = upper - 1;
        double weight = (k - _logMoneyness[lower]) / (_logMoneyness[upper] - _logMoneyness[lower]);
        return row[lower] + weight * (row[upper] - row[lower]);
    }

    private static double TotalVariance(double vol, double t) => vol * vol * t;
    
    private static bool StrictlyIncreasing(double[] xs)
    {
        for (int i = 1; i < xs.Length; i++)
            if (xs[i] <= xs[i - 1]) return false;
        return true;
    }

}