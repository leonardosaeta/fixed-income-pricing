using fixed_income_pricing.Curvers;
using fixed_income_pricing.dates;
using fixed_income_pricing.Instruments;
using fixed_income_pricing.Instruments.Government;
using FixedIncome.Core.Tests.Instruments;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Curves;

public class IterativeBootstrapperTests
{
    private static readonly IterativeCurveBootstrapper Bootstrapper =
        new(AnbimaTestData.Calendar, new Bus252(), new FlatForwardInterpolator());

    // All LTNs plus the NTN-Fs whose maturity is not already an LTN pillar.
    private static List<BootstrapQuote> PreQuotes(DateOnly reference)
    {
        var ltns = AnbimaTestData.Quotes("LTN");
        var ltnDates = ltns.Select(q => q.Maturity).ToHashSet();
        var quotes = ltns
            .Select(q => new BootstrapQuote(new Ltn($"LTN {q.Maturity:yyyyMMdd}", reference.AddYears(-5), q.Maturity), (double)q.Pu))
            .ToList();
        quotes.AddRange(AnbimaTestData.Quotes("NTN-F")
            .Where(q => !ltnDates.Contains(q.Maturity))
            .Select(q => new BootstrapQuote(new NtnF($"NTN-F {q.Maturity:yyyyMMdd}", reference.AddYears(-10), q.Maturity), (double)q.Pu)));
        return quotes;
    }

    private static double Pv(ICashflowInstrument instrument, DiscountCurve curve, DateOnly reference) =>
        instrument.GenerateCashflows(reference).Sum(cf => cf.Amount * curve.DiscountFactor(cf.PaymentDate));

    [Fact]
    public void Curve_RepricesEveryLtnAndNtnF()
    {
        var reference = AnbimaTestData.ReferenceDate;
        var quotes = PreQuotes(reference);

        var curve = Bootstrapper.Bootstrap(reference, quotes);

        foreach (var q in quotes)
            Pv(q.Instrument, curve, reference).Should().BeApproximately(q.Price, 1e-8, q.Instrument.Id);
    }

    [Fact]
    public void Curve_ExtendsBeyondTheLastLtnWithNtnF()
    {
        var reference = AnbimaTestData.ReferenceDate;
        var quotes = PreQuotes(reference);
        var lastLtn = AnbimaTestData.Quotes("LTN").Max(q => q.Maturity);

        quotes.Max(q => q.Instrument.MaturityDate).Should().BeAfter(lastLtn);
        var curve = Bootstrapper.Bootstrap(reference, quotes);
        curve.ZeroRate(quotes.Max(q => q.Instrument.MaturityDate)).Should().BeInRange(0.05, 0.25);
    }

    [Fact]
    public void Bootstrap_Rejects_TwoQuotesOnTheSamePillarDate()
    {
        var reference = new DateOnly(2026, 9, 25);
        var maturity = new DateOnly(2029, 1, 1);
        var quotes = new[]
        {
            new BootstrapQuote(new Ltn("LTN", reference.AddYears(-1), maturity), 750),
            new BootstrapQuote(new NtnF("NTN-F", reference.AddYears(-1), maturity), 960),
        };

        var act = () => Bootstrapper.Bootstrap(reference, quotes);

        act.Should().Throw<ArgumentException>();
    }
}
