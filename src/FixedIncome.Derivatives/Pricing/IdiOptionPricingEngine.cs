using FixedIncome.Derivatives.Models;
using FixedIncome.Derivatives.Options;
using FixedIncome.Derivatives.Volatility;
using fixed_income_pricing.Market;
using fixed_income_pricing.Pricing;
using fixed_income_pricing.Pricing.Interface;

namespace FixedIncome.Derivatives.Pricing;

public sealed record IdiOptionGreeks(double Price, double Delta, double Gamma, double Vega);

public class IdiOptionPricingEngine : ICurvePricingEngine<IdiOption>
{
    private readonly IVolSurface _surface;
    public string CurveName { get; }

    public IdiOptionPricingEngine(IVolSurface surface, string curveName = CurveNames.Di)
    {
        _surface = surface;
        CurveName = curveName;
    }

    public PricingResult Price(IdiOption option, MarketContext market) =>
        new(option.Id, market.ValuationDate, Greeks(option, market).Price);

    public IdiOptionGreeks Greeks(IdiOption option, MarketContext market)
    {
        if (market.ValuationDate > option.MaturityDate) return new IdiOptionGreeks(0, 0, 0, 0);

        var inputs = Inputs(option, market);
        var g = Black76.Greeks(inputs.Forward, option.Strike, inputs.Volatility, inputs.Time, inputs.Discount, option.Type);

        return new IdiOptionGreeks(
            g.Price * IdiOption.PointValue,
            g.Delta / inputs.Discount,
            g.Gamma / (inputs.Discount * inputs.Discount),
            g.Vega * IdiOption.PointValue);
    }
    
    public double ImpliedVolatility(IdiOption option, MarketContext market, double premium)
    {
        var inputs = Inputs(option, market);
        return Black76.ImpliedVolatility(premium / IdiOption.PointValue, inputs.Forward, option.Strike, inputs.Time,
            inputs.Discount, option.Type);
    }

    private (double Forward, double Discount, double Time, double Volatility) Inputs(IdiOption option, MarketContext market)
    {
        double spot = (double)market.Quote(QuoteNames.Idi);
        double discount = market.Curve(CurveName).DiscountFactor(option.MaturityDate);
        double time = market.DayCount.YearFraction(market.ValuationDate, option.MaturityDate, market.Calendar);
        double forward = spot / discount;

        double vol = time > 0 ? _surface.Volatility(time, Math.Log(option.Strike / forward)) : 0.0;
        return (forward, discount, time, vol);
    }
}