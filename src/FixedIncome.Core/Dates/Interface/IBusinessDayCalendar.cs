namespace fixed_income_pricing.Dates.Interface;

public interface IBusinessDayCalendar
{
    bool IsBusinessDays(DateOnly date);
    bool IsHoliday(DateOnly date);
    DateOnly AddBusinessDay(DateOnly date, int n);
    int CountBusinessDaysBetween(DateOnly startDate, DateOnly endDate);
}
