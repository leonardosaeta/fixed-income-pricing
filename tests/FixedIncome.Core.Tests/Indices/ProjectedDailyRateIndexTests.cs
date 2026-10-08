using fixed_income_pricing.Curvers;
using fixed_income_pricing.Dates;
using fixed_income_pricing.dates;
using fixed_income_pricing.Indices;
using fixed_income_pricing.Instruments.Bank;
using fixed_income_pricing.Market;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Indices;

public class ProjectedDailyRateIndexTests
{
    private static readonly B3Calendar Calendar = new(2024, 2032);
    private static readonly DateOnly Reference = new(2026, 9, 25);
    private const double HistoricalRate = 0.1365;

    private static readonly CDIIndex Fixings = new(BuildFixings(), Calendar);

    private static readonly DiscountCurve PreCurve = new(Reference, Calendar, new Bus252(),
        [(new DateOnly(2027, 4, 1), 0.1336), (new DateOnly(2028, 1, 3), 0.1365), (new DateOnly(2030, 1, 2), 0.1402)],
        new FlatForwardInterpolator());

    private static readonly ProjectedDailyRateIndex Projected = new(Fixings, PreCurve);

    private static Dictionary<DateOnly, double> BuildFixings()
    {
        var rates = new Dictionary<DateOnly, double>();
        for (var d = new DateOnly(2025, 9, 1); d < Reference; d = d.AddDays(1))
            if (Calendar.IsBusinessDays(d)) rates[d] = HistoricalRate;
        return rates;
    }

    [Fact]
    public void BeforeTheCurveDate_UsesPublishedFixings()
    {
        var day = new DateOnly(2026, 9, 21);

        Projected.DailyRate(day).Should().Be(Fixings.DailyRate(day));
    }

    [Fact]
    public void FromTheCurveDate_CompoundsToTheInverseDiscountFactor()
    {
        foreach (var maturity in new[] { new DateOnly(2026, 10, 1), new DateOnly(2027, 7, 1), new DateOnly(2031, 1, 2) })
            Projected.AccrualFactor(Reference, maturity)
                .Should().BeApproximately(1.0 / PreCurve.DiscountFactor(maturity), 1e-12, maturity.ToString());
    }

    [Fact]
    public void ProjectedOvernights_FollowTheCurveForwards()
    {
        var a = Projected.DailyRate(new DateOnly(2028, 6, 1));
        var b = Projected.DailyRate(new DateOnly(2029, 6, 1));

        a.Should().BeApproximately(b, 1e-15);
        a.Should().NotBeApproximately(Fixings.DailyRate(new DateOnly(2026, 9, 24)), 1e-7);
    }

    [Fact]
    public void FullCdiLeg_IsWorthItsAccruedValue_OnTheSameCurve()
    {
        var issue = new DateOnly(2025, 9, 25);
        var maturity = new DateOnly(2029, 1, 2);
        var cdb = new PostFixedCDB("CDB 100%", issue, maturity, 1_000_000, 1.0);

        double expectedRedemption = cdb.RedemptionValue(Projected);
        double pv = expectedRedemption * PreCurve.DiscountFactor(maturity);

        pv.Should().BeApproximately(cdb.AccruedValue(Reference, Fixings), 1e-6);
    }

    [Fact]
    public void From_ReadsIndexAndCurveFromTheMarket()
    {
        var market = new MarketContext(Reference, Calendar, new Bus252())
            .WithIndex(IndexNames.Cdi, Fixings)
            .WithCurve(CurveNames.Pre, PreCurve);

        var index = ProjectedDailyRateIndex.From(market, IndexNames.Cdi, CurveNames.Pre);

        index.AccrualFactor(Reference, new DateOnly(2027, 7, 1))
            .Should().BeApproximately(Projected.AccrualFactor(Reference, new DateOnly(2027, 7, 1)), 1e-15);
    }
}
