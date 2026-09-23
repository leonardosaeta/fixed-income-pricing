using System.Collections.Generic;

namespace fixed_income_pricing.MarketData.Interface;

public interface IRateDataSource
{
    IReadOnlyDictionary<DateOnly, double> GetDailyRates(int seriesCode, DateOnly startDate, DateOnly endDate);
}
