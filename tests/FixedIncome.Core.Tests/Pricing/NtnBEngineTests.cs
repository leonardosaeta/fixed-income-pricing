using fixed_income_pricing.Curvers;
using fixed_income_pricing.Curvers.Interface;
using fixed_income_pricing.Dates;
using fixed_income_pricing.Dates.Interface;
using fixed_income_pricing.dates;
using fixed_income_pricing.Instruments.Government;
using fixed_income_pricing.Pricing;
using fixed_income_pricing.Risk;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Pricing;

public class NtnBEngineTests
{
    private static readonly B3Calendar Calendar = new(2020, 2070);
    private static readonly DateOnly Settlement = new(2026, 9, 25);
    private static readonly NtnB Bond = new("NTN-B 2035", new DateOnly(2020, 1, 15), new DateOnly(2035, 5, 15));
    private const decimal Vna = 4500.123456m;
    private const decimal RealRate = 0.075m;

    private static NtnBPricingEngine Engine() => new(_ => Vna, Calendar);

    private static IYieldCurve FlatRealCurve(double rate) => new FlatCurve(Settlement, rate, Calendar);

    [Fact]
    public void Price_OnFlatRealCurve_MatchesClosedFormPu()
    {
        var pv = Engine().Price(Bond, FlatRealCurve((double)RealRate), Settlement).presentValue;

        var expected = Bond.Price(Settlement, RealRate, Vna, Calendar);
        pv.Should().BeApproximately((double)expected, 1e-5);
    }

    [Fact]
    public void Price_OnSinglePillarDiscountCurve_MatchesClosedFormPu()
    {
        var curve = new DiscountCurve(Settlement, Calendar, new Bus252(),
            [(Bond.MaturityDate, (double)RealRate)], new FlatForwardInterpolator());

        var pv = Engine().Price(Bond, curve, Settlement).presentValue;

        var expected = Bond.Price(Settlement, RealRate, Vna, Calendar);
        pv.Should().BeApproximately((double)expected, 1e-5);
    }

    [Fact]
    public void Price_ScalesLinearlyWithVna()
    {
        var curve = FlatRealCurve((double)RealRate);

        var pv = Engine().Price(Bond, curve, Settlement).presentValue;
        var pvDoubleVna = new NtnBPricingEngine(_ => 2 * Vna, Calendar).Price(Bond, curve, Settlement).presentValue;

        pvDoubleVna.Should().BeApproximately(2 * pv, 1e-8);
    }

    [Fact]
    public void Price_RejectsCurveFromAnotherDate()
    {
        var act = () => Engine().Price(Bond, FlatRealCurve(0.07), Settlement.AddDays(1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Risk_DurationAndDv01AreConsistent()
    {
        var engine = Engine();
        var curve = FlatRealCurve((double)RealRate);
        var pv = engine.Price(Bond, curve, Settlement).presentValue;

        var risk = new NtnBRiskEngine(engine, new Bus252(), Calendar).Compute(Bond, curve, Settlement);

        risk.ModifiedDuration.Should().BeInRange(0, 8.7);
        risk.Convexity.Should().BePositive();
        risk.Dv01.Should().BeApproximately(risk.ModifiedDuration!.Value * pv * 1e-4, 1e-3);
    }

    private sealed class FlatCurve(DateOnly referenceDate, double rate, IBusinessDayCalendar calendar) : IYieldCurve
    {
        public DateOnly ReferenceDate => referenceDate;

        public double ZeroRate(DateOnly date) => rate;

        public double DiscountFactor(DateOnly date) =>
            Math.Pow(1 + rate, -calendar.CountBusinessDaysBetween(ReferenceDate, date) / 252.0);
    }
}
