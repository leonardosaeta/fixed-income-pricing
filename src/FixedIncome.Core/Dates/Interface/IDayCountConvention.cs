namespace fixed_income_pricing.Dates.Interface;

public interface IDayCountConvention
{
    double YearFraction(DateOnly startDate, DateOnly endDate, IBusinessDayCalendar calendar);
}