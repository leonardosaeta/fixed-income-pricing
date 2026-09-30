using fixed_income_pricing.Curvers;
using fixed_income_pricing.Curvers.Interface;
using fixed_income_pricing.dates;
using fixed_income_pricing.Instruments.Government;
using FixedIncome.Core.Tests.Instruments;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Curves;

public class LtnBootstrapAnbimaTests
{
    [Fact]
    public void BootstrappedPillarRates_EqualAnbimaIndicativeRates()
    {
        var reference = AnbimaTestData.ReferenceDate;
        var quotes = AnbimaTestData.Quotes("LTN");
        var bootstrapper = new ICurveBootstrapper.PreFixedCurveBootstrapper(
            AnbimaTestData.Calendar, new Bus252(), new FlatForwardInterpolator());

        var curve = bootstrapper.Bootstrap(reference, quotes.Select(q =>
            new ICurveBootstrapper.LtnQuote(new Ltn($"LTN {q.Maturity:yyyyMMdd}", reference.AddYears(-5), q.Maturity), (double)q.Pu)));

        foreach (var q in quotes)
            curve.ZeroRate(q.Maturity).Should().BeApproximately((double)q.IndicativeRate, 1e-6, $"LTN {q.Maturity:dd/MM/yyyy}");
    }
}
