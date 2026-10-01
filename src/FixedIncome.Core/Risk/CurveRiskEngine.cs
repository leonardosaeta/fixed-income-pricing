using fixed_income_pricing.Instruments;
using fixed_income_pricing.Market;
using fixed_income_pricing.Pricing.Interface;
using fixed_income_pricing.Risk.Interface;

namespace fixed_income_pricing.Risk;

// Bump-and-reprice on the engine's curve: parallel DV01/duration/convexity and key-rate DV01s,
// all by ±1bp central differences.
public class CurveRiskEngine<TInstrument> : IRiskEngine<TInstrument> where TInstrument : IInstrument
{
    private const double Bump = 0.0001;

    private readonly ICurvePricingEngine<TInstrument> _pricingEngine;

    public CurveRiskEngine(ICurvePricingEngine<TInstrument> pricingEngine)
    {
        _pricingEngine = pricingEngine;
    }

    public RiskMetric Compute(TInstrument instrument, MarketContext market)
    {
        var parallel = new ParallelShift();
        double basePv = Pv(instrument, market);
        double pvUp = Pv(instrument, Shifted(market, parallel, +Bump));
        double pvDown = Pv(instrument, Shifted(market, parallel, -Bump));

        double dv01 = (pvDown - pvUp) / 2.0;
        double modifiedDuration = -(pvUp - pvDown) / (2*Bump*basePv);
        double convexity = (pvUp + pvDown - 2*basePv) / (basePv*Bump*Bump);

        return new RiskMetric(instrument.Id, modifiedDuration, convexity, dv01);
    }

    public KeyRateRisk KeyRates(TInstrument instrument, MarketContext market, IReadOnlyList<double>? tenors = null)
    {
        tenors ??= KeyRateTenors.Default;
        var buckets = tenors
            .Select((tenor, i) => new KeyRateBucket(tenor, Dv01(instrument, market, new BucketShift(tenors, i))))
            .ToList();
        return new KeyRateRisk(instrument.Id, _pricingEngine.CurveName, buckets);
    }

    private double Dv01(TInstrument instrument, MarketContext market, ICurveShift shift) =>
        (Pv(instrument, Shifted(market, shift, -Bump)) - Pv(instrument, Shifted(market, shift, +Bump))) / 2.0;

    private double Pv(TInstrument instrument, MarketContext market) => _pricingEngine.Price(instrument, market).presentValue;

    private MarketContext Shifted(MarketContext market, ICurveShift shift, double size)
    {
        var name = _pricingEngine.CurveName;
        return market.WithCurve(name, shift.Apply(market.Curve(name), market, size));
    }
}
