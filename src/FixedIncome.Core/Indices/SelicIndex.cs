using fixed_income_pricing.Dates.Interface;

namespace fixed_income_pricing.Indices;

public class SelicIndex : DailyCompoundingIndex
{
    public SelicIndex(IReadOnlyDictionary<DateTime, double> dailyRates, IBusinessDayCalendar calendar) : base("Selic",
        dailyRates, calendar)
    {
    }
    
}