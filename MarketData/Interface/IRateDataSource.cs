using System;
using System.Collections.Generic;

namespace fixed_income_pricing.MarketData.Interface;

public interface IRateDataSource
{
    IReadOnlyDictionary<DateTime, double> GetDailyRates(int seriesCode, DateTime startDate, DateTime endDate); 
}