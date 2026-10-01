using fixed_income_pricing.Dates.Interface;
using fixed_income_pricing.Curvers.Interface;
using fixed_income_pricing.Instruments;

namespace fixed_income_pricing.Curvers;

// Price in the same unit as the instrument's cash flows (R$ for LTN/NTN-F, fraction of VNA for NTN-B).
public sealed record BootstrapQuote(ICashflowInstrument Instrument, double Price);

// Sequential bootstrap: one pillar per instrument, at its maturity, ordered by maturity. Each pillar's zero rate
// is solved so the instrument reprices exactly on the curve built so far plus that pillar. Flows between the
// previous pillar and the new one depend on it through the interpolator, so the solve is numerical.
public class IterativeCurveBootstrapper
{
    private const double Tolerance = 1e-12;
    private const int MaxIterations = 100;

    private readonly IBusinessDayCalendar _calendar;
    private readonly IDayCountConvention _dayCount;
    private readonly IInterpolator _interpolator;

    public IterativeCurveBootstrapper(IBusinessDayCalendar calendar, IDayCountConvention dayCount, IInterpolator interpolator)
    {
        _calendar = calendar;
        _dayCount = dayCount;
        _interpolator = interpolator;
    }

    public DiscountCurve Bootstrap(DateOnly referenceDate, IEnumerable<BootstrapQuote> quotes)
    {
        var ordered = quotes.OrderBy(q => q.Instrument.MaturityDate).ToList();
        if (ordered.Count == 0) throw new ArgumentException("at least one quote is required");

        var duplicate = ordered.GroupBy(q => q.Instrument.MaturityDate).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new ArgumentException($"Two quotes share the pillar date {duplicate.Key:yyyy-MM-dd}; keep one per date");

        var pillars = new List<(DateOnly date, double zeroRate)>();
        foreach (var quote in ordered)
        {
            var flows = quote.Instrument.GenerateCashflows(referenceDate).ToList();
            if (flows.Count == 0)
                throw new ArgumentException($"{quote.Instrument.Id} has no cash flows after {referenceDate:yyyy-MM-dd}");

            double Error(double z)
            {
                var curve = Curve(referenceDate, pillars.Append((quote.Instrument.MaturityDate, z)));
                return flows.Sum(cf => cf.Amount * curve.DiscountFactor(cf.PaymentDate)) - quote.Price;
            }

            double guess = pillars.Count > 0 ? pillars[^1].zeroRate : 0.10;
            pillars.Add((quote.Instrument.MaturityDate, Solve(Error, guess, quote.Price)));
        }

        return Curve(referenceDate, pillars);
    }

    private DiscountCurve Curve(DateOnly referenceDate, IEnumerable<(DateOnly date, double zeroRate)> pillars) =>
        new(referenceDate, _calendar, _dayCount, pillars, _interpolator);

    // Safeguarded Newton: PV falls as the pillar rate rises, so [lo, hi] brackets the root;
    // steps that leave the bracket fall back to bisection.
    private static double Solve(Func<double, double> error, double guess, double scale)
    {
        double lo = -0.9, hi = 10.0;
        double z = Math.Clamp(guess, lo, hi);

        for (int i = 0; i < MaxIterations; i++)
        {
            double f = error(z);
            if (Math.Abs(f) <= Tolerance * Math.Max(1.0, Math.Abs(scale))) return z;
            if (f > 0) lo = z; else hi = z;

            const double h = 1e-7;
            double slope = (error(z + h) - f) / h;
            double next = slope < 0 ? z - f / slope : double.NaN;
            z = next > lo && next < hi ? next : 0.5 * (lo + hi);
        }

        throw new InvalidOperationException("Bootstrap did not converge");
    }
}
