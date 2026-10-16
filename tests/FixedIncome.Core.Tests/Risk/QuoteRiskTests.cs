using fixed_income_pricing.Curvers;
using fixed_income_pricing.Dates;
using fixed_income_pricing.dates;
using fixed_income_pricing.Instruments;
using fixed_income_pricing.Instruments.Futures;
using fixed_income_pricing.Instruments.Government;
using fixed_income_pricing.Market;
using fixed_income_pricing.Pricing;
using fixed_income_pricing.Risk;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Risk;

public class QuoteRiskTests
{
    private static readonly B3Calendar Calendar = new(2020, 2045);
    private static readonly DateOnly Reference = new(2026, 9, 25);
    private static readonly IterativeCurveBootstrapper Bootstrapper = new(Calendar, new Bus252(), new FlatForwardInterpolator());

    private static readonly IReadOnlyList<RateQuote> Quotes =
        new[] { ("DI1F27", 0.1475), ("DI1N27", 0.1420), ("DI1F28", 0.1390), ("DI1F29", 0.1375), ("DI1F31", 0.1380) }
            .Select(q => new RateQuote(Di1Future.FromTicker(q.Item1, Calendar), q.Item2))
            .ToList();

    private static readonly MarketContext Market = new MarketContext(Reference, Calendar, new Bus252())
        .WithCurve(CurveNames.Di, Bootstrapper.Bootstrap(Reference, Quotes));

    private static readonly CashflowPricingEngine Pricing = new(CurveNames.Di);
    private static readonly QuoteRiskEngine<ICashflowInstrument> Engine = new(Pricing, Bootstrapper, Quotes);

    [Fact]
    public void QuotedContract_CarriesAllItsRiskInItsOwnQuote()
    {
        var f28 = Quotes[2];
        double pv = Pricing.Price(f28.Instrument, Market).PresentValue;
        double t = Calendar.CountBusinessDaysBetween(Reference, f28.Instrument.MaturityDate) / 252.0;

        var risk = Engine.Compute(f28.Instrument, Market);

        risk.CurveName.Should().Be(CurveNames.Di);
        risk.Buckets[2].Dv01.Should().BeApproximately(pv * t / (1 + f28.Rate) * 1e-4, 1e-6);
        risk.Buckets.Where((_, i) => i != 2).Should().OnlyContain(b => Math.Abs(b.Dv01) < 1e-9);
    }

    [Fact]
    public void FlowBetweenPillars_SplitsItsRiskBetweenTheNeighbouringQuotes()
    {
        var ltn = new Ltn("LTN 2028-07", new DateOnly(2024, 1, 5), new DateOnly(2028, 7, 1));

        var risk = Engine.Compute(ltn, Market);

        risk.Buckets.Select(b => b.QuoteId).Should().Equal("DI1F27", "DI1N27", "DI1F28", "DI1F29", "DI1F31");
        risk.Buckets[2].Dv01.Should().BePositive();
        risk.Buckets[3].Dv01.Should().BePositive();
        risk.Buckets.Where((_, i) => i is not (2 or 3)).Should().OnlyContain(b => Math.Abs(b.Dv01) < 1e-9);
    }

    [Fact]
    public void TotalQuoteDv01_IsCloseToTheParallelZeroRateDv01()
    {
        var ntnf = new NtnF("NTN-F 2031", new DateOnly(2020, 1, 10), new DateOnly(2031, 1, 1));

        var quoteRisk = Engine.Compute(ntnf, Market);
        var parallel = new CurveRiskEngine<ICashflowInstrument>(Pricing).Compute(ntnf, Market);

        quoteRisk.TotalDv01.Should().BeApproximately(parallel.Dv01, Math.Abs(parallel.Dv01) * 1e-3);
    }
}
