using System;
using fixed_income_pricing.Dates.Interface;

namespace fixed_income_pricing.dates;

public class Bus252:IDayCountConvention
{
    public double YearFraction(DateOnly startDate, DateOnly endDate, IBusinessDayCalendar calendar)
    {
        int businessDays = calendar.CountBusinessDaysBetween(startDate, endDate);
        return businessDays / 252.0;
    }
      
}