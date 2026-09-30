using fixed_income_pricing.Instruments;
using fixed_income_pricing.Instruments.Government;
using fixed_income_pricing.Market;
using fixed_income_pricing.Pricing.Interface;

namespace fixed_income_pricing.Pricing;

public class NtnBPricingEngine:ICurvePricingEngine<NtnB>
{
    public string CurveName { get; }
    public string VnaQuoteName { get; }

    public NtnBPricingEngine(string realCurveName = CurveNames.IpcaReal, string vnaQuoteName = QuoteNames.NtnBVna)
    {
        CurveName = realCurveName;
        VnaQuoteName = vnaQuoteName;
    }

    public PricingResult Price(NtnB instrument, MarketContext market)
    {
        var realCurve = market.Curve(CurveName);
        var dates = CouponSchedule.SemiAnnual(market.ValuationDate, instrument.MaturityDate, market.Calendar);
        double cotacao = 0;
        for (var i = 0; i < dates.Count; i++)
        {
            var flow = (double)NtnB.SemiAnnualCupon + (i == dates.Count - 1 ? 1.0 : 0.0);
            cotacao += flow * realCurve.DiscountFactor(dates[i]);
        }

        double pv = (double)market.Quote(VnaQuoteName) * cotacao;
        return new PricingResult(instrument.Id, market.ValuationDate, pv);
    }
}
