using System;
using System.Collections;
using System.Linq;

namespace fixed_income_pricing.MarketData;

public class IpcaIndexNumberBuilder
{
    public static IReadOnlyDictionary<DateTime, double> BuildIndexNumber(
        IReadOnlyDictionary<DateTime, double> monthlyPercentVariation,
        double baseValue = 100.0)
    {
        var result = new Dictionary<DateTime, double>();
        double runningValue = baseValue;

        foreach (var kvp in monthlyPercentVariation.OrderBy(k => k.Key))
        {
            DateTime referenceMonth = new DateTime(kvp.Key.Year, kvp.Key.Month, 1);
            runningValue *= (1 + kvp.Value);
            result[referenceMonth] = runningValue;
        }
        return result;
    }
    
}