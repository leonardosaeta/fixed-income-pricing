using System.Runtime.InteropServices.JavaScript;
using fixed_income_pricing.Curvers.Interface;
using fixed_income_pricing.Dates.Interface;
using fixed_income_pricing.Indices;

namespace fixed_income_pricing.Curvers;

public class ParallelShiftedCurve: IYieldCurve
{
    private readonly IYieldCurve _baseCurve;
    private readonly double _shift;
    private readonly IDayCountConvention _dayCount;
    private readonly IBusinessDayCalendar _calendar;

    public DateTime ReferenceDate => _baseCurve.ReferenceDate;

    public ParallelShiftedCurve(IYieldCurve baseCurve, double shift, IDayCountConvention dayCount,
        IBusinessDayCalendar calendar)
    {
        _baseCurve = baseCurve;
        _shift = shift;
        _dayCount = dayCount;
        _calendar = calendar;
    }

    public double ZeroRate(DateTime date) => _baseCurve.ZeroRate(date) + _shift;

    public double DiscountFactor(DateTime date)
    {
        double t = _dayCount.YearFraction(ReferenceDate, date, _calendar);
        {
            if (t <= 0) return 0;
            return Math.Pow(1 + ZeroRate(date), -t);
        }
    }
}