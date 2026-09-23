using System.Diagnostics.Metrics;
using fixed_income_pricing.Curvers;
using fixed_income_pricing.Curvers.Interface;
using fixed_income_pricing.Dates.Interface;
using fixed_income_pricing.Instruments.Government;
using fixed_income_pricing.Pricing;
using fixed_income_pricing.Risk.Interface;

namespace fixed_income_pricing.Risk;

public class GovernmentBondRiskEngine:IRiskEngine<Ltn>
{
    private const double Bump = 0.0001; 

    private readonly GovernmentBondPricingEngine _pricingEngine;
    private readonly IDayCountConvention _dayCount;
    private readonly IBusinessDayCalendar _calendar;

    public GovernmentBondRiskEngine(
        GovernmentBondPricingEngine pricingEngine,
        IDayCountConvention dayCount,
        IBusinessDayCalendar calendar)
    {
        _pricingEngine = pricingEngine;
        _dayCount = dayCount;
        _calendar = calendar;
    }

    public RiskMetric Compute(Ltn instrument, IYieldCurve curve, DateOnly valuationDate)
    {

        double basePv = _pricingEngine.Price(instrument, curve, valuationDate).presentValue;
        var curveUp = new ParallelShiftedCurve(curve, +Bump, _dayCount, _calendar);
        var curveDown = new ParallelShiftedCurve(curve, -Bump, _dayCount, _calendar);
        
        double pvUp = _pricingEngine.Price(instrument, curveUp, valuationDate).presentValue;
        double pvDown = _pricingEngine.Price(instrument, curveDown, valuationDate).presentValue;
        
        double dv01 = (pvDown - pvUp) / 2.0;
        double modifiedDuration = -(pvUp - pvDown) / (2*Bump*basePv);
        double convexity = (pvUp + pvDown - 2*basePv) / (basePv*Bump*Bump);
        
        return new RiskMetric(instrument.Id, modifiedDuration, convexity, dv01);
    }
}