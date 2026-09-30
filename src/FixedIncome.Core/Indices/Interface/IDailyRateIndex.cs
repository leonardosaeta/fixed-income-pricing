using fixed_income_pricing.Dates.Interface;

namespace fixed_income_pricing.Indices;

public interface IDailyRateIndex : IIndex
{
    IBusinessDayCalendar Calendar { get; }

    double DailyRate(DateOnly date);
}
