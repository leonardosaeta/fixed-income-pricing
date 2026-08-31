using System;
using System.Collections.Generic;
using fixed_income_pricing.Dates;
using fixed_income_pricing.Dates.Interface;

namespace fixed_income_pricing.Indices;

public abstract class DailyCompoundingIndex : IIndex
{
    public string Name  { get; }

    private readonly IReadOnlyDictionary<DateTime, double> _dailyRates;
    private readonly IBusinessDayCalendar _calendar;
    
    protected DailyCompoundingIndex(
        string name,
        IReadOnlyDictionary<DateTime, double> dailyRates,
        IBusinessDayCalendar calendar)
    {
      Name   = name;
      _dailyRates = dailyRates;
      _calendar = calendar;
    }

    private double DailyFactor(DateTime date)
    {
        if (!_dailyRates.TryGetValue(date.Date, out double annualRate))
            throw new InvalidOperationException($"{Name}: no rate available:yyyy-mm-dd");
        return Math.Pow(1+annualRate, 1.0/252.0);
    }

    public double AccrualFactor(DateTime startDate, DateTime endDate)
    { 
        if (startDate > endDate) throw new ArgumentException("startDate must <= endDate");

        double factor = 1.0;
        DateTime current =  startDate.Date;

        while (current <= endDate)
        {
            current = _calendar.AddBusinessDay(current, 1);
            factor *= DailyFactor(current);
        }
        
        return factor;
    }   
}