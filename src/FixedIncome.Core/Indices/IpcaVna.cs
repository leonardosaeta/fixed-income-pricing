using fixed_income_pricing.Pricing;

namespace fixed_income_pricing.Indices;

public class IpcaVna
{
    public static decimal Project(
        decimal vnaAtLast15th,
        decimal projectedMontlhyIpca,
        DateOnly last15th,
        DateOnly settlement)
    {
        var next15th = last15th.AddMonths(1);
        var elapsed = settlement.DayNumber - last15th.DayNumber;
        var period  = next15th.DayNumber - last15th.DayNumber;
        
        var factor = Math.Pow(1.0+(double)projectedMontlhyIpca, (double)elapsed/period);
        return Rounding.Truncate(vnaAtLast15th * (decimal)factor, 6);
    }
}