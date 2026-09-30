using fixed_income_pricing.Instruments.Government;
using fixed_income_pricing.Market;
using fixed_income_pricing.Pricing.Interface;

namespace fixed_income_pricing.Pricing;

public class GovernmentBondPricingEngine:ICurvePricingEngine<Ltn>
{
    public string CurveName { get; }

    public GovernmentBondPricingEngine(string curveName = CurveNames.Pre)
    {
        CurveName = curveName;
    }

    public PricingResult Price(Ltn instrument, MarketContext market)
    {
        var curve = market.Curve(CurveName);
        double pv = instrument.GenerateCashflows(market.ValuationDate)
            .Sum(cf => cf.Amount * curve.DiscountFactor(cf.PaymentDate));
        
        return new PricingResult(instrument.Id, market.ValuationDate, pv);
    }
}
