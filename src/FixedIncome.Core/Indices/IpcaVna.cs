using fixed_income_pricing.Dates.Interface;
using fixed_income_pricing.Pricing;

namespace fixed_income_pricing.Indices;

public class IpcaVna
{
    public static decimal Project(
        decimal vnaAtLast15th,
        decimal projectedMonthlyIpca,
        DateOnly last15th,
        DateOnly settlement,
        IBusinessDayCalendar calendar)
    {
        var next15th = last15th.AddMonths(1);
        var elapsed = calendar.CountBusinessDaysBetween(last15th, settlement);
        var period  = calendar.CountBusinessDaysBetween(last15th, next15th);
        
        var factor = Math.Pow(1.0+(double)projectedMonthlyIpca, (double)elapsed/period);
        return Rounding.Truncate(vnaAtLast15th * (decimal)factor, 6);
    }
}
