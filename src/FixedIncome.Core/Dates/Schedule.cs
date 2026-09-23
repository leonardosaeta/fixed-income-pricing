using System.ComponentModel;
using fixed_income_pricing.Dates.Interface;

namespace fixed_income_pricing.Dates;


public enum BusinessDayConvention
{
    Following,
    Preceding
}


public class Schedule
{
    public static List<DateOnly> GenerateCouponDates(
        DateOnly issueDate,
        DateOnly maturityDate,
        int couponsPerYear,
        IBusinessDayCalendar calendar,
        BusinessDayConvention convention = BusinessDayConvention.Following)
    {
        if (couponsPerYear <= 0)
        {
            throw new ArgumentException("couponsPerYear must be positive");
        }

        int monthsBetweenCoupons = 12 / couponsPerYear;
        var rawDate = new List<DateOnly>();

        DateOnly current = maturityDate;
        while (current > issueDate)
        {
            rawDate.Insert(0, current);
            current = current.AddMonths(-monthsBetweenCoupons);
        }

        var adjustedDate = new List<DateOnly>(rawDate.Count);
        foreach (var date in rawDate)
            adjustedDate.Add(AdjustedToBusinessDay(date, calendar, convention));
        return adjustedDate;
    }

    public static DateOnly AdjustedToBusinessDay(
        DateOnly date,
        IBusinessDayCalendar calendar,
        BusinessDayConvention convention)
    {
        if (calendar.IsBusinessDays(date))
            return date;

        int step = convention == BusinessDayConvention.Following ? 1 : -1;
        DateOnly adjusted = date;

        while (!calendar.IsBusinessDays(adjusted))
        {
            adjusted = adjusted.AddDays(step);
        }
        return adjusted;
    }
}
