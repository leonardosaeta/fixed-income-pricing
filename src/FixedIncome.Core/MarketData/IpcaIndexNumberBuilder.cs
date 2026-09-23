using System;
using System.Collections;
using System.Linq;

namespace fixed_income_pricing.MarketData;

public class IpcaIndexNumberBuilder
{
    public static IReadOnlyDictionary<DateOnly, double> BuildIndexNumber(
        IReadOnlyDictionary<DateOnly, double> monthlyPercentVariation,
        double baseValue = 100.0)
    {
        var result = new Dictionary<DateOnly, double>();
        double runningValue = baseValue;

        foreach (var kvp in monthlyPercentVariation.OrderBy(k => k.Key))
        {
            DateOnly referenceMonth = new DateOnly(kvp.Key.Year, kvp.Key.Month, 1);
            runningValue *= (1 + kvp.Value);
            result[referenceMonth] = runningValue;
        }
        return result;
    }
    
}