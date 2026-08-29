namespace fixed_income_pricing.Dates.Interface;

public interface IDayCountConvention
{
    double YearFraction(DateTime startDate, DateTime endDate, IBusinessDayCalendar calendar);
}