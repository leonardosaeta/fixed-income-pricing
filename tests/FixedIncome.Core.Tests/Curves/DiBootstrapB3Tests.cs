using fixed_income_pricing.Curvers;
using fixed_income_pricing.dates;
using fixed_income_pricing.Instruments.Futures;
using FixedIncome.Core.Tests.Instruments;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Curves;

public class DiBootstrapB3Tests
{
    private static readonly IterativeCurveBootstrapper Bootstrapper =
        new(B3TestData.Calendar, new Bus252(), new FlatForwardInterpolator());

    private static List<(Di1Future Future, decimal Pu, double Rate)> Contracts() =>
        B3TestData.Di1Settlements()
            .Where(s => s.SettlementRate is not null)
            .Select(s => (Di1Future.FromTicker(s.Ticker, B3TestData.Calendar), s.SettlementPu, (double)s.SettlementRate!.Value))
            .Where(c => c.Item1.MaturityDate > B3TestData.ReferenceDate)
            .ToList();

    [Fact]
    public void CurveFromSettlementRates_RoundsToEveryContractsSettlementPu()
    {
        var reference = B3TestData.ReferenceDate;
        var contracts = Contracts();
        contracts.Should().HaveCountGreaterThan(20);

        var curve = Bootstrapper.Bootstrap(reference, contracts.Select(c => new RateQuote(c.Future, c.Rate)));

        foreach (var c in contracts)
            decimal.Round(Di1Future.Notional * (decimal)curve.DiscountFactor(c.Future.MaturityDate), 2, MidpointRounding.AwayFromZero)
                .Should().Be(c.Pu, $"{c.Future.Id} on {reference:yyyy-MM-dd}");
    }

    [Fact]
    public void CurveFromSettlementPus_PillarRatesAreTheSettlementRates()
    {
        var reference = B3TestData.ReferenceDate;
        var contracts = Contracts();

        var curve = Bootstrapper.Bootstrap(reference, contracts.Select(c => new BootstrapQuote(c.Future, (double)c.Pu)));

        // B3 publishes the rate with 3 decimals in %, so it is known to ±0.0005%.
        foreach (var c in contracts)
            curve.ZeroRate(c.Future.MaturityDate).Should().BeApproximately(c.Rate, 5e-6, $"{c.Future.Id} on {reference:yyyy-MM-dd}");
    }
}
