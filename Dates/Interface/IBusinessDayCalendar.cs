namespace fixed_income_pricing.Dates.Interface;

public interface IBusinessDayCalendar
{
    bool IsBusinessDays(DateTime date);
    bool IsHoliday(DateTime date);
    DateTime AddBusinessDay(DateTime date, int n);
    int CountBusinessDaysBetween(DateTime startDate, DateTime endDate);
}