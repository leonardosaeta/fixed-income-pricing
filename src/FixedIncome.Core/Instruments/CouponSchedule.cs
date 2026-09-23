using fixed_income_pricing.Dates.Interface;

namespace fixed_income_pricing.Instruments;

public static class CouponSchedule
{
    public static IReadOnlyList<DateOnly> SemiAnnual(DateOnly settlement, DateOnly maturity,
        IBusinessDayCalendar calendar)
    {
        var dates = new List<DateOnly>();
        for (var d = maturity; d>settlement; d=d.AddMonths(-6))
            dates.Add(d);
        
        dates.Reverse();
        return dates
            .Select(d => RollForward(d, calendar))
            .Where(d => d > settlement)
            .ToList();
    }

    private static DateOnly RollForward(DateOnly date, IBusinessDayCalendar calendar) =>
        calendar.IsBusinessDays(date) ? date : calendar.AddBusinessDay(date, 1);
}