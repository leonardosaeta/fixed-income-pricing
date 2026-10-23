namespace FixedIncome.Derivatives.Volatility;

public sealed class FlatVolSurface:IVolSurface
{
    private readonly double _volatility;

    public FlatVolSurface(double volatility)
    {
        if (volatility < 0) throw new ArgumentException("volatility cannot be negative");
        _volatility = volatility;
    }

    public double Volatility(double timeToExpiry, double logMoneyness);
}