using fixed_income_pricing.Instruments;
using fixed_income_pricing.Market;

namespace fixed_income_pricing.Risk.Interface;

public interface IRiskEngine<in TInstrument> where TInstrument:IInstrument
{
    RiskMetric Compute(TInstrument instrument, MarketContext market);
}
