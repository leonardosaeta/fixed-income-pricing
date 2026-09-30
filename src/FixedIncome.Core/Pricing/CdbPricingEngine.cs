using fixed_income_pricing.Indices;
using fixed_income_pricing.Instruments.Bank;
using fixed_income_pricing.Market;
using fixed_income_pricing.Pricing.Interface;

namespace fixed_income_pricing.Pricing;

public class CdbPricingEngine:IPricingEngine<PostFixedCDB>
{
    public string IndexName { get; }

    public CdbPricingEngine(string indexName = IndexNames.Cdi)
    {
        IndexName = indexName;
    }

    public PricingResult Price(PostFixedCDB instruement, MarketContext market)
    {
        double value = instruement.AccruedValue(market.ValuationDate, market.Index<IDailyRateIndex>(IndexName));
        return new PricingResult(instruement.Id, market.ValuationDate, value);
    }
}
