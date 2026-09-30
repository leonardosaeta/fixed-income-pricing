using fixed_income_pricing.Indices;
using fixed_income_pricing.Instruments.Bank;
using fixed_income_pricing.Market;
using fixed_income_pricing.Pricing;
using fixed_income_pricing.Risk.Interface;

namespace fixed_income_pricing.Risk;

public class CdbRiskEngine:IRiskEngine<PostFixedCDB>
{
    private const double Bump = 0.0001; 

    private readonly CdbPricingEngine _pricingEngine;

    public CdbRiskEngine(CdbPricingEngine pricingEngine)
    {
        _pricingEngine = pricingEngine;
    }

    public RiskMetric Compute(PostFixedCDB instrument, MarketContext market)
    {
        var cdi = market.Index<IDailyRateIndex>(_pricingEngine.IndexName);

        double pvUp = _pricingEngine.Price(instrument,
            market.WithIndex(_pricingEngine.IndexName, new ShiftedDailyRateIndex(cdi, +Bump))).presentValue;
        double pvDown = _pricingEngine.Price(instrument,
            market.WithIndex(_pricingEngine.IndexName, new ShiftedDailyRateIndex(cdi, -Bump))).presentValue;

        double dv01 = (pvDown - pvUp) / 2.0;

        return new RiskMetric(instrument.Id, ModifiedDuration: null, Convexity: null, dv01);
    } 
}
