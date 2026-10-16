using fixed_income_pricing.Curvers;
using fixed_income_pricing.Instruments;
using fixed_income_pricing.Market;
using fixed_income_pricing.Pricing.Interface;

namespace fixed_income_pricing.Risk;

public sealed record QuoteBucket(string QuoteId, DateOnly Maturity, double Dv01);

public sealed record QuoteRisk(string InstrumentId, string CurveName, IReadOnlyList<QuoteBucket> Buckets)
{
    public double TotalDv01 => Buckets.Sum(b => b.Dv01);
}

// Risk to the curve's market quotes: bump one quoted rate by ±1bp, re-bootstrap, reprice. This is the risk the
// quoted instruments (DI1 contracts, LTNs) hedge, as opposed to the zero-rate key rates of CurveRiskEngine.
// The engine must read the curve from the market on every Price call for the bumped curve to reach it.
public class QuoteRiskEngine<TInstrument> where TInstrument : IInstrument
{
    private const double Bump = 0.0001;

    private readonly ICurvePricingEngine<TInstrument> _pricingEngine;
    private readonly IterativeCurveBootstrapper _bootstrapper;
    private readonly IReadOnlyList<RateQuote> _quotes;

    public QuoteRiskEngine(ICurvePricingEngine<TInstrument> pricingEngine, IterativeCurveBootstrapper bootstrapper,
        IReadOnlyList<RateQuote> quotes)
    {
        if (quotes.Count == 0) throw new ArgumentException("at least one quote is required");

        _pricingEngine = pricingEngine;
        _bootstrapper = bootstrapper;
        _quotes = quotes;
    }

    public QuoteRisk Compute(TInstrument instrument, MarketContext market)
    {
        var buckets = _quotes
            .Select((q, i) => new QuoteBucket(q.Instrument.Id, q.Instrument.MaturityDate,
                (Pv(instrument, market, i, -Bump) - Pv(instrument, market, i, +Bump)) / 2.0))
            .ToList();
        return new QuoteRisk(instrument.Id, _pricingEngine.CurveName, buckets);
    }

    private double Pv(TInstrument instrument, MarketContext market, int bumped, double size)
    {
        var quotes = _quotes.Select((q, i) => i == bumped ? q with { Rate = q.Rate + size } : q);
        var curve = _bootstrapper.Bootstrap(market.ValuationDate, quotes);
        return _pricingEngine.Price(instrument, market.WithCurve(_pricingEngine.CurveName, curve)).PresentValue;
    }
}
