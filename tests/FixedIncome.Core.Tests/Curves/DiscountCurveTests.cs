using fixed_income_pricing.Curvers;
using fixed_income_pricing.Dates;
using fixed_income_pricing.dates;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Curves;

public class DiscountCurveTests
{
    private static readonly B3Calendar Calendar = new(2020, 2070);
    private static readonly Bus252 DayCount = new();
    private static readonly DateOnly Reference = new(2026, 9, 25);
    private static readonly DateOnly OneYear = new(2027, 9, 24);
    private static readonly DateOnly ThreeYears = new(2029, 9, 25);

    private static DiscountCurve Curve(params (DateOnly, double)[] pillars) =>
        new(Reference, Calendar, DayCount, pillars, new FlatForwardInterpolator());

    [Fact]
    public void SinglePillar_ActsAsFlatCurve()
    {
        var curve = Curve((OneYear, 0.10));

        curve.ZeroRate(new DateOnly(2027, 3, 15)).Should().BeApproximately(0.10, 1e-12);
        curve.ZeroRate(OneYear).Should().BeApproximately(0.10, 1e-12);
        curve.ZeroRate(ThreeYears).Should().BeApproximately(0.10, 1e-12);
    }

    [Fact]
    public void ReproducesPillarRates()
    {
        var curve = Curve((OneYear, 0.10), (ThreeYears, 0.12));

        curve.ZeroRate(OneYear).Should().BeApproximately(0.10, 1e-12);
        curve.ZeroRate(ThreeYears).Should().BeApproximately(0.12, 1e-12);
    }

    [Fact]
    public void BetweenPillars_UsesConstantForward()
    {
        var curve = Curve((OneYear, 0.10), (ThreeYears, 0.12));
        var mid = Calendar.AddBusinessDay(OneYear, Calendar.CountBusinessDaysBetween(OneYear, ThreeYears) / 2);

        double Forward(DateOnly a, DateOnly b) =>
            Math.Log(curve.DiscountFactor(a) / curve.DiscountFactor(b)) / DayCount.YearFraction(a, b, Calendar);

        Forward(OneYear, mid).Should().BeApproximately(Forward(mid, ThreeYears), 1e-10);
    }

    [Fact]
    public void BeyondLastPillar_ExtendsLastForward()
    {
        var curve = Curve((OneYear, 0.10));
        var fiveYears = new DateOnly(2031, 9, 25);

        curve.DiscountFactor(fiveYears).Should().BeLessThan(curve.DiscountFactor(ThreeYears));
    }
}
