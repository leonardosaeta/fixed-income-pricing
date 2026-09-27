using fixed_income_pricing.Curvers.Interface;
using fixed_income_pricing.Dates.Interface;
using fixed_income_pricing.Instruments;
using fixed_income_pricing.Instruments.Government;
using fixed_income_pricing.Pricing.Interface;

namespace fixed_income_pricing.Pricing;

public class NtnBPricingEngine:IPricingEngine<NtnB>
{
    private readonly Func<DateOnly, decimal> _projectedVna;
    private readonly IBusinessDayCalendar _calendar;

    public NtnBPricingEngine(Func<DateOnly, decimal> projectedVna, IBusinessDayCalendar calendar)
    {
        _projectedVna = projectedVna;
        _calendar = calendar;
    }

    public PricingResult Price(NtnB instrument, IYieldCurve realCurve, DateOnly valuationDate)
    {
        if(realCurve.ReferenceDate != valuationDate)
            throw new ArgumentException("Curve must match valuation date");

        var dates = CouponSchedule.SemiAnnual(valuationDate, instrument.MaturityDate, _calendar);
        double cotacao = 0;
        for (var i = 0; i < dates.Count; i++)
        {
            var flow = (double)NtnB.SemiAnnualCupon + (i == dates.Count - 1 ? 1.0 : 0.0);
            cotacao += flow * realCurve.DiscountFactor(dates[i]);
        }

        double pv = (double)_projectedVna(valuationDate) * cotacao;
        return new PricingResult(instrument.Id, valuationDate, pv);
    }
}
