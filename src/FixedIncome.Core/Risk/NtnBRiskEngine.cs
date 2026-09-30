using fixed_income_pricing.Instruments.Government;
using fixed_income_pricing.Pricing;

namespace fixed_income_pricing.Risk;

public class NtnBRiskEngine : ParallelCurveRiskEngine<NtnB>
{
    public NtnBRiskEngine(NtnBPricingEngine pricingEngine) : base(pricingEngine)
    {
    }
}
