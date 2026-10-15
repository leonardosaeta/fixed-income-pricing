using fixed_income_pricing.Indices;
using fixed_income_pricing.Instruments.Swaps;
using fixed_income_pricing.Market;
using fixed_income_pricing.Pricing.Interface;

namespace fixed_income_pricing.Pricing;

public class DiPreSwapPricingEngine:ICurvePricingEngine<DiPreSwap>
{
   public string CurveName { get; }
   public string IndexName { get; }

   public DiPreSwapPricingEngine(string curveName = CurveNames.Di, string indexName = IndexNames.Cdi)
   {
      CurveName = curveName;
      IndexName = indexName;
   }

   public PricingResult Price(DiPreSwap swap, MarketContext market)
   {
      if (market.ValuationDate >= swap.MaturityDate)
         return new PricingResult(swap.Id, market.ValuationDate, 0.0);


      var curve = market.Curve(CurveName);
      double fixedLeg = swap.FixedLegAmount(market.Calendar);
      double floatingLeg = FloatingLegAmount(swap, market);

      double pv = swap.FloatingSign * (floatingLeg - fixedLeg) * curve.DiscountFactor(swap.MaturityDate);
      return new PricingResult(swap.Id, market.ValuationDate, pv);
   }

   public double ParRate(DiPreSwap swap, MarketContext market)
   {
      int du = market.Calendar.CountBusinessDaysBetween(swap.StartDate, swap.MaturityDate);
      double growth = FloatingLegAmount(swap, market)/swap.Notional;
      return Math.Pow(growth, 252.0/du) - 1.0;
   }
   
   private double FloatingLegAmount(DiPreSwap swap, MarketContext market)
   {
      var cdi = ProjectedDailyRateIndex.From(market, IndexName, CurveName);
      return swap.Notional * cdi.AccrualFactor(swap.StartDate, swap.MaturityDate); 
   }
}