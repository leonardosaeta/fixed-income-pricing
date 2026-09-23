using System;
using System.Collections.Generic;
using System.Data;
using fixed_income_pricing.Dates.Interface;

namespace fixed_income_pricing.Indices;

public class CDIIndex : DailyCompoundingIndex
{
    public CDIIndex(IReadOnlyDictionary<DateOnly, double> dailyRates, IBusinessDayCalendar calendar):base("CDI", dailyRates, calendar)
    {
    }
}