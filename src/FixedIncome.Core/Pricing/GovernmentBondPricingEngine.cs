using fixed_income_pricing.Curvers.Interface;
using fixed_income_pricing.Instruments.Government;
using fixed_income_pricing.Pricing.Interface;

namespace fixed_income_pricing.Pricing;

public class GovernmentBondPricingEngine:IPricingEngine<Ltn>
{
    public PricingResult Price(Ltn instrument, IYieldCurve curve, DateOnly valuationDate)
    {
        if(curve.ReferenceDate != valuationDate)
            throw new ArgumentException("Curve must match valuation date");
        double pv = instrument.GenerateCashflows(valuationDate)
            .Sum(cf => cf.Amount * curve.DiscountFactor(cf.PaymentDate));
        
        return new PricingResult(instrument.Id,valuationDate, pv);
    }
}