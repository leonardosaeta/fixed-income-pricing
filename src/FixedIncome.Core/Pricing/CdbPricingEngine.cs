using fixed_income_pricing.Indices;
using fixed_income_pricing.Instruments.Bank;
using fixed_income_pricing.Market;
using fixed_income_pricing.Pricing.Interface;

namespace fixed_income_pricing.Pricing;

public class CdbPricingEngine:IPricingEngine<PostFixedCDB>
{
    public string IndexName { get; }
    public AccrualRounding Rounding { get; }

    public CdbPricingEngine(string indexName = IndexNames.Cdi, AccrualRounding rounding = AccrualRounding.B3)
    {
        IndexName = indexName;
        Rounding = rounding;
    }

    public PricingResult Price(PostFixedCDB instrument, MarketContext market)
    {
        double value = instrument.AccruedValue(market.ValuationDate, market.Index<IDailyRateIndex>(IndexName), Rounding);
        return new PricingResult(instrument.Id, market.ValuationDate, value);
    }
}
