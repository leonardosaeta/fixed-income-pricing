using fixed_income_pricing.Curvers;
using fixed_income_pricing.Dates;
using fixed_income_pricing.dates;
using fixed_income_pricing.Instruments.Futures;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Instruments;

public class Di1FutureTests
{
    private static readonly B3Calendar Calendar = new(2020, 2045);
    private static readonly DateOnly Reference = new(2026, 9, 25);

    [Theory]
    [InlineData("DI1F27", 2027, 1, 4)] // 1 Jan is a holiday, 2–3 Jan a weekend
    [InlineData("N27", 2027, 7, 1)]
    [InlineData("di1v26", 2026, 10, 1)]
    public void FromTicker_MaturesOnTheFirstBusinessDayOfTheMonth(string ticker, int year, int month, int day)
    {
        Di1Future.FromTicker(ticker, Calendar).MaturityDate.Should().Be(new DateOnly(year, month, day));
    }

    [Theory]
    [InlineData("DI1A27")]
    [InlineData("DI1F2027")]
    [InlineData("")]
    public void FromTicker_RejectsMalformedTickers(string ticker)
    {
        var act = () => Di1Future.FromTicker(ticker, Calendar);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Pu_DiscountsTheNotionalAtTheRate_RoundedToCents()
    {
        var di1 = Di1Future.FromTicker("DI1F28", Calendar);
        int du = Calendar.CountBusinessDaysBetween(Reference, di1.MaturityDate);

        var pu = di1.Pu(Reference, 0.1405m, Calendar);

        pu.Should().Be(decimal.Round(pu, 2));
        ((double)pu).Should().BeApproximately(100_000 / Math.Pow(1.1405, du / 252.0), 0.005);
        di1.ImpliedRate(Reference, pu, Calendar).Should().BeApproximately(0.1405, 1e-6);
    }

    [Fact]
    public void DailySettlement_IsNearZero_WhenTheRateDoesNotMove()
    {
        var di1 = Di1Future.FromTicker("DI1F28", Calendar);
        var yesterday = Calendar.AddBusinessDay(Reference, -1);
        const double cdi = 0.14;

        var previousPu = di1.Pu(yesterday, 0.14m, Calendar);
        var pu = di1.Pu(Reference, 0.14m, Calendar);

        Di1Future.DailySettlement(previousPu, pu, cdi).Should().BeInRange(-0.02m, 0.02m);
        Di1Future.DailySettlement(previousPu, di1.Pu(Reference, 0.1410m, Calendar), cdi).Should().BeNegative();
    }

    [Fact]
    public void DiCurve_RepricesEveryContract_AndItsPillarsAreTheQuotedRates()
    {
        var quotes = new[] { ("DI1V26", 0.1490), ("DI1F27", 0.1475), ("DI1N27", 0.1420), ("DI1F28", 0.1390), ("DI1F30", 0.1370), ("DI1F33", 0.1385) }
            .Select(q => new RateQuote(Di1Future.FromTicker(q.Item1, Calendar), q.Item2))
            .ToList();
        var bootstrapper = new IterativeCurveBootstrapper(Calendar, new Bus252(), new FlatForwardInterpolator());

        var curve = bootstrapper.Bootstrap(Reference, quotes);

        foreach (var q in quotes)
        {
            curve.ZeroRate(q.Instrument.MaturityDate).Should().BeApproximately(q.Rate, 1e-12, q.Instrument.Id);
            (100_000 * curve.DiscountFactor(q.Instrument.MaturityDate))
                .Should().BeApproximately(bootstrapper.PriceFromRate(Reference, q), 1e-6, q.Instrument.Id);
        }
    }
}
