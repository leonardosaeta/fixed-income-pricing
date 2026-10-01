using fixed_income_pricing.Curvers.Interface;
using fixed_income_pricing.Dates.Interface;

namespace fixed_income_pricing.Curvers;

// Shifts the zero rate by shift · w_k(t), where w_k is a tent around tenor k (flat beyond the first and last
// tenors). The tents sum to 1 at every t, so the key-rate shifts add up to a parallel shift.
public class BucketShiftedCurve : IYieldCurve
{
    private readonly IYieldCurve _baseCurve;
    private readonly IReadOnlyList<double> _tenors;
    private readonly int _bucket;
    private readonly double _shift;
    private readonly IDayCountConvention _dayCount;
    private readonly IBusinessDayCalendar _calendar;

    public DateOnly ReferenceDate => _baseCurve.ReferenceDate;

    public BucketShiftedCurve(IYieldCurve baseCurve, IReadOnlyList<double> tenors, int bucket, double shift,
        IDayCountConvention dayCount, IBusinessDayCalendar calendar)
    {
        if (tenors.Count == 0) throw new ArgumentException("at least one tenor is required");
        if (bucket < 0 || bucket >= tenors.Count) throw new ArgumentOutOfRangeException(nameof(bucket));
        for (int i = 1; i < tenors.Count; i++)
            if (tenors[i] <= tenors[i - 1]) throw new ArgumentException("tenors must be strictly increasing");

        _baseCurve = baseCurve;
        _tenors = tenors;
        _bucket = bucket;
        _shift = shift;
        _dayCount = dayCount;
        _calendar = calendar;
    }

    public static double Weight(IReadOnlyList<double> tenors, int bucket, double t)
    {
        int last = tenors.Count - 1;
        if (t <= tenors[0]) return bucket == 0 ? 1 : 0;
        if (t >= tenors[last]) return bucket == last ? 1 : 0;

        int j = 0;
        while (tenors[j + 1] <= t) j++;
        double w = (t - tenors[j]) / (tenors[j + 1] - tenors[j]);
        return bucket == j ? 1 - w : bucket == j + 1 ? w : 0;
    }

    public double ZeroRate(DateOnly date) =>
        _baseCurve.ZeroRate(date) + _shift * Weight(_tenors, _bucket, _dayCount.YearFraction(ReferenceDate, date, _calendar));

    public double DiscountFactor(DateOnly date)
    {
        double t = _dayCount.YearFraction(ReferenceDate, date, _calendar);
        if (t <= 0) return 0;
        return Math.Pow(1 + ZeroRate(date), -t);
    }
}
