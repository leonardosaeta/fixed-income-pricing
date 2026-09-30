using fixed_income_pricing.Instruments.Government;
using fixed_income_pricing.Pricing;

namespace fixed_income_pricing.Risk;

public class GovernmentBondRiskEngine : ParallelCurveRiskEngine<Ltn>
{
    public GovernmentBondRiskEngine(GovernmentBondPricingEngine pricingEngine) : base(pricingEngine)
    {
    }
}
