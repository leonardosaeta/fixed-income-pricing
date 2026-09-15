using fixed_income_pricing.Curvers.Interface;
using fixed_income_pricing.Dates.Interface;
using fixed_income_pricing.Indices;
using fixed_income_pricing.Instruments.Bank;
using fixed_income_pricing.Pricing;
using fixed_income_pricing.Risk.Interface;

namespace fixed_income_pricing.Risk;

public class CdbRiskEngine:IRiskEngine<PostFixedCDB>
{
    private const double Bump = 0.0001; 

    private readonly CdbPricingEngine _pricingEngine;
    private readonly IIndex _cdiIndex;
    private readonly IDayCountConvention _dayCount;
    private readonly IBusinessDayCalendar _calendar;

    public CdbRiskEngine(
        CdbPricingEngine pricingEngine,
        IIndex cdiIndex,
        IDayCountConvention dayCount,
        IBusinessDayCalendar calendar)
    {
        _pricingEngine = pricingEngine;
        _cdiIndex = cdiIndex;
        _dayCount = dayCount;
        _calendar = calendar;
    }

    public RiskMetric Compute(PostFixedCDB Instrument, IYieldCurve Curve, DateTime ValuationDate)
    {
        double basePv = _pricingEngine.Price(Instrument, Curve, ValuationDate).presentValue;

        var cdiUp = new ShiftedIndex(_cdiIndex, +Bump, _dayCount, _calendar);
        var cdiDown = new ShiftedIndex(_cdiIndex, -Bump, _dayCount, _calendar);

        double pvUp = _pricingEngine.Price(Instrument.WithIndex(cdiUp), Curve, ValuationDate).presentValue;
        double pvDown = _pricingEngine.Price(Instrument.WithIndex(cdiDown), Curve, ValuationDate).presentValue;

        double dv01 = (pvDown - pvUp) / 2.0;

        return new RiskMetric(Instrument.Id, ModifiedDuration: null, Convexity: null, dv01);
    } 
}