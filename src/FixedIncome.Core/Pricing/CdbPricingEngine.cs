using fixed_income_pricing.Curvers.Interface;
using fixed_income_pricing.Instruments.Bank;
using fixed_income_pricing.Pricing.Interface;

namespace fixed_income_pricing.Pricing;

public class CdbPricingEngine:IPricingEngine<PostFixedCDB>
{
    public PricingResult Price(PostFixedCDB instruement, IYieldCurve curve, DateOnly valuationDate)
    {
        if(curve.ReferenceDate != valuationDate)
            throw new ArgumentException("Curve must match valuation date");
        double value = instruement.AccruedValue(valuationDate);
        return new PricingResult(instruement.Id, valuationDate, value);
    }
}