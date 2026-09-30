using fixed_income_pricing.Dates.Interface;

namespace fixed_income_pricing.Indices;

public class ShiftedDailyRateIndex : IDailyRateIndex
{
    private readonly IDailyRateIndex _baseIndex;
    private readonly double _dailyShiftFactor;

    public ShiftedDailyRateIndex(IDailyRateIndex baseIndex, double shift)
    {
        _baseIndex = baseIndex;
        _dailyShiftFactor = Math.Pow(1 + shift, 1.0 / 252.0);
    }

    public string Name => $"{_baseIndex.Name}(Shifted)";
    public IBusinessDayCalendar Calendar => _baseIndex.Calendar;

    public double DailyRate(DateOnly date) => (1 + _baseIndex.DailyRate(date)) * _dailyShiftFactor - 1;

    public double AccrualFactor(DateOnly startDate, DateOnly endDate) =>
        DailyRateAccrual.Factor(this, startDate, endDate);
}
