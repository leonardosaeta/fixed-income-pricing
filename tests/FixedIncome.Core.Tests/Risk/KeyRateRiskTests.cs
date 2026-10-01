using fixed_income_pricing.Curvers;
using fixed_income_pricing.Dates;
using fixed_income_pricing.dates;
using fixed_income_pricing.Instruments;
using fixed_income_pricing.Instruments.Government;
using fixed_income_pricing.Market;
using fixed_income_pricing.Pricing;
using fixed_income_pricing.Risk;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Risk;

public class KeyRateRiskTests
{
    private static readonly B3Calendar Calendar = new(2020, 2045);
    private static readonly DateOnly Reference = new(2026, 9, 25);

    private static readonly MarketContext Market = new MarketContext(Reference, Calendar, new Bus252())
        .WithCurve(CurveNames.Pre, new DiscountCurve(Reference, Calendar, new Bus252(),
            [(new DateOnly(2027, 7, 1), 0.134), (new DateOnly(2030, 1, 1), 0.140), (new DateOnly(2037, 1, 1), 0.141)],
            new FlatForwardInterpolator()));

    private static readonly CurveRiskEngine<ICashflowInstrument> Engine = new(new CashflowPricingEngine());

    [Theory]
    [InlineData(0.3)]
    [InlineData(1.7)]
    [InlineData(4.0)]
    [InlineData(40.0)]
    public void TentWeights_SumToOne(double t)
    {
        var tenors = KeyRateTenors.Default;

        Enumerable.Range(0, tenors.Count).Sum(i => BucketShiftedCurve.Weight(tenors, i, t)).Should().BeApproximately(1, 1e-15);
    }

    [Fact]
    public void KeyRateDv01s_AddUpToTheParallelDv01()
    {
        var bond = new NtnF("NTN-F 2037", new DateOnly(2026, 1, 9), new DateOnly(2037, 1, 1));

        var keyRates = Engine.KeyRates(bond, Market);
        var parallel = Engine.Compute(bond, Market);

        keyRates.CurveName.Should().Be(CurveNames.Pre);
        keyRates.TotalDv01.Should().BeApproximately(parallel.Dv01, Math.Abs(parallel.Dv01) * 1e-6);
        keyRates.Buckets.Count(b => Math.Abs(b.Dv01) > 1e-9).Should().BeGreaterThan(3);
    }

    [Fact]
    public void ZeroCouponOnATenor_PutsAllItsRiskInThatBucket()
    {
        var ltn = new Ltn("LTN 2029", new DateOnly(2024, 1, 5), new DateOnly(2029, 1, 1));
        double t = Calendar.CountBusinessDaysBetween(Reference, ltn.MaturityDate) / 252.0;
        double[] tenors = [1.0, t, 5.0];

        var keyRates = Engine.KeyRates(ltn, Market, tenors);

        keyRates.Buckets[1].Dv01.Should().BeApproximately(Engine.Compute(ltn, Market).Dv01, 1e-9);
        keyRates.Buckets[0].Dv01.Should().BeApproximately(0, 1e-12);
        keyRates.Buckets[2].Dv01.Should().BeApproximately(0, 1e-12);
    }

    [Fact]
    public void Report_AggregatesByCurveAndTenor()
    {
        var ltn = new Ltn("LTN 2029", new DateOnly(2024, 1, 5), new DateOnly(2029, 1, 1));
        var ntnf = new NtnF("NTN-F 2037", new DateOnly(2026, 1, 9), new DateOnly(2037, 1, 1));
        var ltnRisk = Engine.KeyRates(ltn, Market);
        var ntnfRisk = Engine.KeyRates(ntnf, Market);

        var report = KeyRateReport.Aggregate([(1000, ltnRisk), (500, ntnfRisk)]);

        report[(CurveNames.Pre, 2.0)].Should().BeApproximately(
            1000 * ltnRisk.Buckets.Single(b => b.Tenor == 2.0).Dv01 + 500 * ntnfRisk.Buckets.Single(b => b.Tenor == 2.0).Dv01, 1e-9);
        KeyRateReport.ByCurve(report)[CurveNames.Pre].Should().BeApproximately(
            1000 * ltnRisk.TotalDv01 + 500 * ntnfRisk.TotalDv01, 1e-6);
    }
}
