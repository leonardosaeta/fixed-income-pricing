using FixedIncome.Derivatives.Options;

namespace FixedIncome.Derivatives.Models;

public static class Black76
{
    public static double Price(double forward, double strike, double volatility, double timeToExpiry,
        double discoutFactor, OptionType type)
    {
        double sign = Sign(type);
        if (volatility <= 0.0 || timeToExpiry <= 0.0)
            return discoutFactor * Math.Max(sign * (forward - strike), 0.0);
        
        var (d1, d2) = D1D2(forward, strike, volatility, timeToExpiry);
        return discoutFactor * sign * (forward * NormalDistribution.Cdf(sign * d1) - strike * NormalDistribution.Cdf(sign * d2));
    }

    public static Black76Greeks Greeks(double forward, double strike, double volatility, double timeToExpiry, double discoutFactor, OptionType type)
    {
        double sign = Sign(type);
        double price = Price (forward, strike, volatility, timeToExpiry, discoutFactor, type);

        if (volatility <= 0.0 || timeToExpiry <= 0.0)
        {
            bool inTheMoney = sign * (forward - strike) > 0.0;
            return new Black76Greeks(price, inTheMoney? sign * discoutFactor : 0.0, 0.0, 0.0);
        }
        double sqrtT = Math.Sqrt(timeToExpiry);
        var (d1, _) = D1D2(forward, strike, volatility, timeToExpiry);
        double pdf = NormalDistribution.Pdf(d1);

        return new Black76Greeks(
            price,
            discoutFactor * sign * NormalDistribution.Cdf(sign * d1),
            discoutFactor * pdf / (forward * volatility * sqrtT),
            discoutFactor * forward * pdf * sqrtT); 
    }
    
    public static double ImpliedVolatility(double price, double forward, double strike, double timeToExpiry, double discountFactor, OptionType type)
    {
        double sign = Sign(type);
        double intrinsic = discountFactor * Math.Max(sign * (forward - strike), 0.0);
        double upperBound = discountFactor * (type == OptionType.call ? forward : strike);

        if (timeToExpiry <= 0.0) throw new ArgumentException("no implied volatility at or after expiry");
        if (price <= intrinsic || price >= upperBound)
            throw new ArgumentException(
                $"price {price} is outside the no-arbitrage range ({intrinsic}, {upperBound}) for any volatility");

        double lo = 1e-8, hi = 10.0, vol = 0.2;
        for (int i = 0; i < 200; i++)
        {
            var g = Greeks(forward, strike, vol, timeToExpiry, discountFactor, type);
            double error = g.Price - price;
            if (Math.Abs(error) <= 1e-13 * price || hi - lo < 1e-15) return vol;

            if (error > 0) hi = vol; else lo = vol;

            double newton = g.Vega > 1e-14 ? vol - error / g.Vega : double.NaN;
            vol = newton > lo && newton < hi ? newton : 0.5 * (lo + hi);
        }

        throw new InvalidOperationException("implied volatility did not converge");
    }

    public sealed record Black76Greeks(double Price, double Delta, double Gamma, double Vega);
    
    private static (double d1, double d2) D1D2(double forward, double strike, double volatility, double timeToExpiry)
    {
        double sd = volatility * Math.Sqrt(timeToExpiry);
        double d1 = (Math.Log(forward / strike) + 0.5 * sd * sd) / sd;
        return (d1, d1 - sd);
    }

    private static double Sign(OptionType type) => type == OptionType.call ? 1.0 : -1.0;
}