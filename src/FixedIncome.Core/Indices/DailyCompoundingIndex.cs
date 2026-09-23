using System;
using System.Collections.Generic;
using fixed_income_pricing.Dates;
using fixed_income_pricing.Dates.Interface;

namespace fixed_income_pricing.Indices;

public abstract class DailyCompoundingIndex : IIndex
{
    public string Name  { get; }

    private readonly IReadOnlyDictionary<DateOnly, double> _dailyRates;
    private readonly IBusinessDayCalendar _calendar;

    protected DailyCompoundingIndex(
        string name,
        IReadOnlyDictionary<DateOnly, double> dailyRates,
        IBusinessDayCalendar calendar)
    {
      Name   = name;
      _dailyRates = dailyRates;
      _calendar = calendar;
    }

    private double DailyFactor(DateOnly date, ref double lastKnownRate)
    {
        if (_dailyRates.TryGetValue(date, out double annualRate))
        {
            lastKnownRate = annualRate;
        }
        else if (double.IsNaN(lastKnownRate))
        {
            throw new InvalidOperationException($"{Name}: no rate available for {date:yyyy-MM-dd}");
        }

        return Math.Pow(1 + lastKnownRate, 1.0/252.0);
    }

    public double AccrualFactor(DateOnly startDate, DateOnly endDate)
    {
        if (startDate > endDate) throw new ArgumentException("startDate must <= endDate");

        double factor = 1.0;
        double lastKnownRate = double.NaN;
        DateOnly current =  startDate;

        while (current <= endDate)
        {
            current = _calendar.AddBusinessDay(current, 1);
            factor *= DailyFactor(current, ref lastKnownRate);
        }

        return factor;
    }
}