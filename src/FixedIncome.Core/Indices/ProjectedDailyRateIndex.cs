using fixed_income_pricing.Curvers.Interface;
using fixed_income_pricing.Dates.Interface;
using fixed_income_pricing.Market;

namespace fixed_income_pricing.Indices;

public class ProjectedDailyRateIndex : IDailyRateIndex
{
    private readonly IDailyRateIndex _fixings;
    private readonly IYieldCurve _curve;

    public ProjectedDailyRateIndex(IDailyRateIndex fixings, IYieldCurve curve)
    {
        _fixings = fixings;
        _curve = curve;
    }

    public static ProjectedDailyRateIndex From(MarketContext market, string indexName, string curveName) =>
        new(market.Index<IDailyRateIndex>(indexName), market.Curve(curveName));

    public string Name => $"{_fixings.Name}(Projected)";
    public IBusinessDayCalendar Calendar => _fixings.Calendar;

    public double DailyRate(DateOnly date)
    {
        if (date < _curve.ReferenceDate)
            return _fixings.DailyRate(date);

        return DiscountFactor(date) / DiscountFactor(Calendar.AddBusinessDay(date, 1)) - 1.0;
    }

    public double AccrualFactor(DateOnly startDate, DateOnly endDate) =>
        DailyRateAccrual.Factor(this, startDate, endDate);

    private double DiscountFactor(DateOnly date) =>
        date <= _curve.ReferenceDate ? 1.0 : _curve.DiscountFactor(date);
}
