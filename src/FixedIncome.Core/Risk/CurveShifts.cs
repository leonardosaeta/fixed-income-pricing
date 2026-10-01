using fixed_income_pricing.Curvers;
using fixed_income_pricing.Curvers.Interface;
using fixed_income_pricing.Market;

namespace fixed_income_pricing.Risk;

// A curve scenario of a given size (absolute rate), applied to one curve of the market.
public interface ICurveShift
{
    IYieldCurve Apply(IYieldCurve curve, MarketContext market, double size);
}

public sealed class ParallelShift : ICurveShift
{
    public IYieldCurve Apply(IYieldCurve curve, MarketContext market, double size) =>
        new ParallelShiftedCurve(curve, size, market.DayCount, market.Calendar);
}

public sealed class BucketShift(IReadOnlyList<double> tenors, int bucket) : ICurveShift
{
    public IYieldCurve Apply(IYieldCurve curve, MarketContext market, double size) =>
        new BucketShiftedCurve(curve, tenors, bucket, size, market.DayCount, market.Calendar);
}

public static class KeyRateTenors
{
    // Business years
    public static readonly IReadOnlyList<double> Default = [0.25, 0.5, 1, 2, 3, 5, 7, 10, 15, 20, 30];
}
