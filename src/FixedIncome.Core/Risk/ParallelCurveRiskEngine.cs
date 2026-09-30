using fixed_income_pricing.Curvers;
using fixed_income_pricing.Instruments;
using fixed_income_pricing.Market;
using fixed_income_pricing.Pricing.Interface;
using fixed_income_pricing.Risk.Interface;

namespace fixed_income_pricing.Risk;

public class ParallelCurveRiskEngine<TInstrument> : IRiskEngine<TInstrument> where TInstrument : IInstrument
{
    private const double Bump = 0.0001;

    private readonly ICurvePricingEngine<TInstrument> _pricingEngine;

    public ParallelCurveRiskEngine(ICurvePricingEngine<TInstrument> pricingEngine)
    {
        _pricingEngine = pricingEngine;
    }

    public RiskMetric Compute(TInstrument instrument, MarketContext market)
    {
        double basePv = _pricingEngine.Price(instrument, market).presentValue;
        double pvUp = _pricingEngine.Price(instrument, Shifted(market, +Bump)).presentValue;
        double pvDown = _pricingEngine.Price(instrument, Shifted(market, -Bump)).presentValue;

        double dv01 = (pvDown - pvUp) / 2.0;
        double modifiedDuration = -(pvUp - pvDown) / (2*Bump*basePv);
        double convexity = (pvUp + pvDown - 2*basePv) / (basePv*Bump*Bump);

        return new RiskMetric(instrument.Id, modifiedDuration, convexity, dv01);
    }

    private MarketContext Shifted(MarketContext market, double shift)
    {
        var curveName = _pricingEngine.CurveName;
        return market.WithCurve(curveName,
            new ParallelShiftedCurve(market.Curve(curveName), shift, market.DayCount, market.Calendar));
    }
}
