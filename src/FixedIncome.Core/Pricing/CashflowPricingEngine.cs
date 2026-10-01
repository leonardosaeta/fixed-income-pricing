using fixed_income_pricing.Instruments;
using fixed_income_pricing.Market;
using fixed_income_pricing.Pricing.Interface;

namespace fixed_income_pricing.Pricing;

// PV = Σ CF_i · P(0, t_i) on one named curve, for any instrument with known cash flows (in R$).
public class CashflowPricingEngine : ICurvePricingEngine<ICashflowInstrument>
{
    public string CurveName { get; }

    public CashflowPricingEngine(string curveName = CurveNames.Pre)
    {
        CurveName = curveName;
    }

    public PricingResult Price(ICashflowInstrument instrument, MarketContext market)
    {
        var curve = market.Curve(CurveName);
        double pv = instrument.GenerateCashflows(market.ValuationDate)
            .Sum(cf => cf.Amount * curve.DiscountFactor(cf.PaymentDate));
        return new PricingResult(instrument.Id, market.ValuationDate, pv);
    }
}
