using fixed_income_pricing.Curvers.Interface;
using fixed_income_pricing.Instruments;

namespace fixed_income_pricing.Risk.Interface;

public interface IRiskEngine<in TInstrument> where TInstrument:IInstrument
{
    RiskMetric Compute(TInstrument instrument, IYieldCurve curve, DateTime valuationDate);
}