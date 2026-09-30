namespace fixed_income_pricing.Indices;

public static class DailyRateAccrual
{
    public static double Factor(IDailyRateIndex index, DateOnly startDate, DateOnly endDate, double percentage = 1.0)
    {
        if (startDate > endDate) throw new ArgumentException("startDate must be <= endDate");

        var calendar = index.Calendar;
        double factor = 1.0;
        DateOnly current = calendar.IsBusinessDays(startDate) ? startDate : calendar.AddBusinessDay(startDate, 1);

        while (current < endDate)
        {
            factor *= 1.0 + percentage * index.DailyRate(current);
            current = calendar.AddBusinessDay(current, 1);
        }

        return factor;
    }
}
