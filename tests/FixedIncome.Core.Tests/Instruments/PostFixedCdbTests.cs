using fixed_income_pricing.Dates;
using fixed_income_pricing.dates;
using fixed_income_pricing.Indices;
using fixed_income_pricing.Instruments.Bank;
using fixed_income_pricing.Market;
using fixed_income_pricing.Pricing;
using fixed_income_pricing.Risk;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Instruments;

public class PostFixedCdbTests
{
    private static readonly B3Calendar Calendar = new(2024, 2028);
    private static readonly DateOnly Issue = new(2025, 9, 25);
    private static readonly DateOnly Valuation = new(2026, 9, 25);
    private const double Notional = 1_000_000;
    private const double Rate = 0.1365;

    private static readonly CDIIndex Cdi = new(FlatFixings(Issue, Valuation, Rate), Calendar);
    private static readonly PostFixedCDB Cdb = new("CDB 102% CDI", Issue, new DateOnly(2027, 9, 24), Notional, 1.02);

    private static Dictionary<DateOnly, double> FlatFixings(DateOnly from, DateOnly to, double rate)
    {
        var rates = new Dictionary<DateOnly, double>();
        for (var d = from; d < to; d = d.AddDays(1))
            if (Calendar.IsBusinessDays(d)) rates[d] = rate;
        return rates;
    }

    private static MarketContext Market => new MarketContext(Valuation, Calendar, new Bus252()).WithIndex(IndexNames.Cdi, Cdi);

    [Fact]
    public void AccruedValue_IsTheNotional_OnTheIssueDate()
    {
        Cdb.AccruedValue(Issue, Cdi).Should().Be(Notional);
    }

    [Fact]
    public void AccruedValue_AppliesThePercentageToEachDailyRate()
    {
        int du = Calendar.CountBusinessDaysBetween(Issue, Valuation);
        double daily = Math.Pow(1 + Rate, 1.0 / 252.0) - 1;

        Cdb.AccruedValue(Valuation, Cdi).Should().BeApproximately(Notional * Math.Pow(1 + 1.02 * daily, du), 1e-6);
    }

    [Fact]
    public void Engine_PricesWithTheMarketsCdiIndex()
    {
        var result = new CdbPricingEngine().Price(Cdb, Market);

        result.presentValue.Should().Be(Cdb.AccruedValue(Valuation, Cdi));
    }

    [Fact]
    public void Risk_Dv01IsNegativeAndMatchesTheAnalyticApproximation()
    {
        var engine = new CdbPricingEngine();
        var value = engine.Price(Cdb, Market).presentValue;
        double tau = Calendar.CountBusinessDaysBetween(Issue, Valuation) / 252.0;
        double daily = Math.Pow(1 + Rate, 1.0 / 252.0);

        var risk = new CdbRiskEngine(engine).Compute(Cdb, Market);

        double expected = -value * tau * (1.02 * daily / (1 + 1.02 * (daily - 1))) * 1e-4;
        risk.Dv01.Should().BeNegative();
        risk.Dv01.Should().BeApproximately(expected, Math.Abs(expected) * 1e-3);
        risk.ModifiedDuration.Should().BeNull();
    }
}
