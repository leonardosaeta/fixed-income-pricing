using fixed_income_pricing.Dates.Interface;

namespace fixed_income_pricing.Indices;

public class ShiftedIndex:IIndex
{
    private readonly IIndex _baseIndex;
    private readonly double _shift;
    private readonly IDayCountConvention _dayCount;
    private readonly IBusinessDayCalendar _calendar;


    public string Name => $"{_baseIndex}(Shifted)";

    public ShiftedIndex(IIndex baseIndex, double shift, IDayCountConvention dayCount, IBusinessDayCalendar calendar)
    {
        _baseIndex = baseIndex;
        _shift = shift;
        _dayCount = dayCount;
        _calendar = calendar;
    }

    public double AccrualFactor(DateOnly start, DateOnly end)
    {
        double baseFactor = _baseIndex.AccrualFactor(start, end);
        double t = _dayCount.YearFraction(start, end, _calendar);
        return baseFactor*Math.Pow(1+_shift, t);
    }
}