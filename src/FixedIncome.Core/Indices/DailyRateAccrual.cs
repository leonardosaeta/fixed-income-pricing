using fixed_income_pricing.Pricing;

namespace fixed_income_pricing.Indices;

public enum AccrualRounding
{
    None,
    B3
}

public static class DailyRateAccrual
{
    public static double Factor(IDailyRateIndex index, DateOnly startDate, DateOnly endDate, double percentage = 1.0)
    {
        double factor = 1.0;
        foreach (var day in Overnights(index, startDate, endDate))
            factor *= 1.0 + percentage * index.DailyRate(day);

        return factor;
    }

    public static double Factor(IDailyRateIndex index, DateOnly startDate, DateOnly endDate, double percentage,
        AccrualRounding rounding) =>
        rounding == AccrualRounding.B3
            ? (double)B3Factor(index, startDate, endDate, (decimal)percentage)
            : Factor(index, startDate, endDate, percentage);

    public static decimal B3Factor(IDailyRateIndex index, DateOnly startDate, DateOnly endDate, decimal percentage = 1m)
    {
        decimal factor = 1m;
        foreach (var day in Overnights(index, startDate, endDate))
        {
            decimal daily = Math.Round(1m + percentage * B3DailyRate(index.DailyRate(day)), 16, MidpointRounding.AwayFromZero);
            factor = Rounding.Truncate(factor * daily, 16);
        }

        return Math.Round(factor, 8, MidpointRounding.AwayFromZero);
    }

    public static decimal B3DailyFactor(double annualRate) =>
        1m + B3DailyRate(Math.Pow(1 + annualRate, 1.0 / 252.0) - 1.0);

    private static decimal B3DailyRate(double dailyRate) =>
        Math.Round((decimal)dailyRate, 8, MidpointRounding.AwayFromZero);

    private static IEnumerable<DateOnly> Overnights(IDailyRateIndex index, DateOnly startDate, DateOnly endDate)
    {
        if (startDate > endDate) throw new ArgumentException("startDate must be <= endDate");

        var calendar = index.Calendar;
        DateOnly current = calendar.IsBusinessDays(startDate) ? startDate : calendar.AddBusinessDay(startDate, 1);

        while (current < endDate)
        {
            yield return current;
            current = calendar.AddBusinessDay(current, 1);
        }
    }
}
