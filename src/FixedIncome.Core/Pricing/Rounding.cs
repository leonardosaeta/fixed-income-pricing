namespace fixed_income_pricing.Pricing;

public class Rounding
{
    public static decimal Truncate(decimal value, int decimals)
    {
        var factor = Pow10(decimals);
        return Math.Truncate(value * factor) / factor;
    }

    private static decimal Pow10(int n)
    {
        var r = 1m;
        for (var i = 0; i < n; i++)
        {
            r *= 10m;
        }
        return r;
    }
}