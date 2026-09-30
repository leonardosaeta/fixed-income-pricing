using fixed_income_pricing.Curvers;
using fixed_income_pricing.Dates;
using fixed_income_pricing.dates;
using fixed_income_pricing.Indices;
using fixed_income_pricing.Instruments.Government;
using fixed_income_pricing.Market;
using fixed_income_pricing.Pricing;
using fixed_income_pricing.Risk;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Market;

public class MarketContextTests
{
    private static readonly B3Calendar Calendar = new(2020, 2040);
    private static readonly DateOnly Reference = new(2026, 9, 25);

    private static DiscountCurve Flat(double rate, DateOnly? reference = null) =>
        new(reference ?? Reference, Calendar, new Bus252(), [(new DateOnly(2030, 1, 2), rate)], new FlatForwardInterpolator());

    private static MarketContext Empty => new(Reference, Calendar, new Bus252());

    [Fact]
    public void Curve_Throws_WithTheMissingName()
    {
        var act = () => Empty.WithCurve(CurveNames.Pre, Flat(0.13)).Curve(CurveNames.IpcaReal);

        act.Should().Throw<KeyNotFoundException>().WithMessage($"*{CurveNames.IpcaReal}*{CurveNames.Pre}*");
    }

    [Fact]
    public void WithCurve_ReturnsANewContext_AndLeavesTheOriginalUntouched()
    {
        var baseMarket = Empty.WithCurve(CurveNames.Pre, Flat(0.13));
        var bumped = baseMarket.WithCurve(CurveNames.Pre, Flat(0.14));

        baseMarket.Curve(CurveNames.Pre).ZeroRate(new DateOnly(2028, 1, 3)).Should().BeApproximately(0.13, 1e-12);
        bumped.Curve(CurveNames.Pre).ZeroRate(new DateOnly(2028, 1, 3)).Should().BeApproximately(0.14, 1e-12);
    }

    [Fact]
    public void WithCurve_Rejects_ACurveFromAnotherDate()
    {
        var act = () => Empty.WithCurve(CurveNames.Pre, Flat(0.13, Reference.AddDays(-1)));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TypedIndex_Throws_WhenTheIndexHasAnotherType()
    {
        var market = Empty.WithIndex(IndexNames.Ipca, new IPCAIndex(new Dictionary<DateOnly, double>()));

        var act = () => market.Index<IDailyRateIndex>(IndexNames.Ipca);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void LtnRisk_OnAFlatCurve_MatchesTheClosedForm()
    {
        var market = Empty.WithCurve(CurveNames.Pre, Flat(0.14));
        var ltn = new Ltn("LTN 2028", new DateOnly(2024, 1, 5), new DateOnly(2028, 1, 1));
        double t = Calendar.CountBusinessDaysBetween(Reference, ltn.MaturityDate) / 252.0;

        var risk = new GovernmentBondRiskEngine(new GovernmentBondPricingEngine()).Compute(ltn, market);

        risk.ModifiedDuration!.Value.Should().BeApproximately(t / 1.14, 1e-6);
        risk.Convexity!.Value.Should().BeApproximately(t * (t + 1) / (1.14 * 1.14), 1e-5);
    }
}
