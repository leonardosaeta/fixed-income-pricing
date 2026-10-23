namespace FixedIncome.Derivatives.Volatility;

public interface IVolSurface
{
    double Volatility(double timeToExpiry, double logMoneyness);
}