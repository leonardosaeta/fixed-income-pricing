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
    public static List<DateTime> GenerateCouponDates(
        DateTime issueDate,
        DateTime maturityDate,
        int couponsPerYear,
        IBusinessDayCalendar calendar,
        BusinessDayConvention convention = BusinessDayConvention.Following)
    {
        if (couponsPerYear <= 0)
        {
            throw new ArgumentException("couponsPerYear must be positive");
        }

        int monthsBetweenCoupons = 12 / couponsPerYear;
        var rawDate = new List<DateTime>();
        
        DateTime current = maturityDate;
        while (current > issueDate)
        {
            rawDate.Insert(0, current);
            current = current.AddMonths(-monthsBetweenCoupons);
        }
        
        var adjustedDate = new List<DateTime>(rawDate.Count);
        foreach (var date in rawDate)
            adjustedDate.Add(AdjustedToBusinessDay(date, calendar, convention));
        return adjustedDate;
    }

    public static DateTime AdjustedToBusinessDay(
        DateTime date,
        IBusinessDayCalendar calendar,
        BusinessDayConvention convention)
    {
        if (calendar.IsBusinessDays(date))
            return date;

        int step = convention == BusinessDayConvention.Following ? 1 : -1;
        DateTime adjusted = date;

        while (!calendar.IsBusinessDays(adjusted))
        {
            adjusted = adjusted.AddDays(step);
        }
        return adjusted;
    }
}