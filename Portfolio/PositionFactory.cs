using fixed_income_pricing.Pricing;
using fixed_income_pricing.Risk;

namespace fixed_income_pricing.Portfolio;

public class PositionFactory
{
   public static Position FromResults(double quantity, PricingResult pricing, RiskMetric risk)
   {
      if (pricing.InstrumnetId != risk.InstrumentId)
         throw new ArgumentException("pricing and risk results must reference the same instrument");

      return new Position(
         pricing.InstrumnetId,
         quantity,
         pricing.presentValue,
         risk.ModifiedDuration,
         risk.Convexity,
         risk.Dv01);
   } 
    
}