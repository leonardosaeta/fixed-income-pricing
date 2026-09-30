using System.Collections.Immutable;
using fixed_income_pricing.Curvers.Interface;
using fixed_income_pricing.Dates.Interface;
using fixed_income_pricing.Indices;

namespace fixed_income_pricing.Market;

public sealed class MarketContext
{
    public DateOnly ValuationDate { get; }
    public IBusinessDayCalendar Calendar { get; }
    public IDayCountConvention DayCount { get; }

    private readonly ImmutableDictionary<string, IYieldCurve> _curves;
    private readonly ImmutableDictionary<string, IIndex> _indices;
    private readonly ImmutableDictionary<string, decimal> _quotes;

    public MarketContext(DateOnly valuationDate, IBusinessDayCalendar calendar, IDayCountConvention dayCount)
        : this(valuationDate, calendar, dayCount,
            ImmutableDictionary<string, IYieldCurve>.Empty,
            ImmutableDictionary<string, IIndex>.Empty,
            ImmutableDictionary<string, decimal>.Empty)
    {
    }

    private MarketContext(
        DateOnly valuationDate,
        IBusinessDayCalendar calendar,
        IDayCountConvention dayCount,
        ImmutableDictionary<string, IYieldCurve> curves,
        ImmutableDictionary<string, IIndex> indices,
        ImmutableDictionary<string, decimal> quotes)
    {
        ValuationDate = valuationDate;
        Calendar = calendar;
        DayCount = dayCount;
        _curves = curves;
        _indices = indices;
        _quotes = quotes;
    }

    public IEnumerable<string> CurveNames => _curves.Keys;
    public IEnumerable<string> IndexNames => _indices.Keys;

    public MarketContext WithCurve(string name, IYieldCurve curve)
    {
        if (curve.ReferenceDate != ValuationDate)
            throw new ArgumentException(
                $"Curve '{name}' is dated {curve.ReferenceDate:yyyy-MM-dd}; the market is dated {ValuationDate:yyyy-MM-dd}");
        return new MarketContext(ValuationDate, Calendar, DayCount, _curves.SetItem(name, curve), _indices, _quotes);
    }

    public MarketContext WithIndex(string name, IIndex index) =>
        new(ValuationDate, Calendar, DayCount, _curves, _indices.SetItem(name, index), _quotes);

    public MarketContext WithQuote(string name, decimal value) =>
        new(ValuationDate, Calendar, DayCount, _curves, _indices, _quotes.SetItem(name, value));

    public IYieldCurve Curve(string name) =>
        _curves.TryGetValue(name, out var curve) ? curve : throw Missing("curve", name, _curves.Keys);

    public IIndex Index(string name) =>
        _indices.TryGetValue(name, out var index) ? index : throw Missing("index", name, _indices.Keys);

    public T Index<T>(string name) where T : IIndex =>
        Index(name) is T typed
            ? typed
            : throw new InvalidOperationException($"Index '{name}' is not a {typeof(T).Name}");

    public decimal Quote(string name) =>
        _quotes.TryGetValue(name, out var value) ? value : throw Missing("quote", name, _quotes.Keys);

    private static KeyNotFoundException Missing(string kind, string name, IEnumerable<string> available) =>
        new($"Market has no {kind} '{name}'. Available: [{string.Join(", ", available)}]");
}
