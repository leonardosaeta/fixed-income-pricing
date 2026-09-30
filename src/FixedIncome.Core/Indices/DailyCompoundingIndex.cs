using System;
using System.Collections.Generic;
using System.Linq;
using fixed_income_pricing.Dates.Interface;

namespace fixed_income_pricing.Indices;

public abstract class DailyCompoundingIndex : IDailyRateIndex
{
    public string Name  { get; }
    public IBusinessDayCalendar Calendar { get; }

    private readonly IReadOnlyDictionary<DateOnly, double> _dailyRates;
    private readonly DateOnly _firstFixing;
    private readonly DateOnly _lastFixing;

    protected DailyCompoundingIndex(
        string name,
        IReadOnlyDictionary<DateOnly, double> dailyRates,
        IBusinessDayCalendar calendar)
    {
        if (dailyRates.Count == 0) throw new ArgumentException($"{name}: at least one fixing is required");

        Name = name;
        _dailyRates = dailyRates;
        Calendar = calendar;
        _firstFixing = dailyRates.Keys.Min();
        _lastFixing = dailyRates.Keys.Max();
    }

    public double DailyRate(DateOnly date)
    {
        if (_dailyRates.TryGetValue(date, out double annualRate))
            return Math.Pow(1 + annualRate, 1.0 / 252.0) - 1.0;

        if (date > _lastFixing)
            return Math.Pow(1 + _dailyRates[_lastFixing], 1.0 / 252.0) - 1.0;

        throw new InvalidOperationException(date < _firstFixing
            ? $"{Name}: no rate available for {date:yyyy-MM-dd} (series starts {_firstFixing:yyyy-MM-dd})"
            : $"{Name}: missing fixing for {date:yyyy-MM-dd} inside the published history");
    }

    public double AccrualFactor(DateOnly startDate, DateOnly endDate) =>
        DailyRateAccrual.Factor(this, startDate, endDate);
}
