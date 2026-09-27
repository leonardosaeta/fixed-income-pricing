using fixed_income_pricing.Instruments.Government;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Instruments;

public class NtnBTests
{
    private static readonly DateOnly Issue = new(2020, 1, 15);
    private static readonly DateOnly Maturity = new(2030, 8, 15);

    [Fact]
    public void GenerateCashflows_PaysSemiAnnualCouponsAndPrincipalAtMaturity()
    {
        var bond = new NtnB("NTN-B 2030", Issue, Maturity);

        var flows = bond.GenerateCashflows(new DateOnly(2029, 1, 10)).ToList();

        flows.Select(cf => cf.PaymentDate).Should().Equal(
            new DateOnly(2029, 2, 15),
            new DateOnly(2029, 8, 15),
            new DateOnly(2030, 2, 15),
            new DateOnly(2030, 8, 15));
        flows.Take(3).Should().OnlyContain(cf => cf.Amount == (double)NtnB.SemiAnnualCupon);
        flows.Last().Amount.Should().Be((double)(1m + NtnB.SemiAnnualCupon));
    }

    [Fact]
    public void GenerateCashflows_ExcludesCouponPaidOnValuationDate()
    {
        var bond = new NtnB("NTN-B 2030", Issue, Maturity);

        var flows = bond.GenerateCashflows(new DateOnly(2030, 2, 15)).ToList();

        flows.Should().ContainSingle().Which.PaymentDate.Should().Be(Maturity);
    }

    [Fact]
    public void GenerateCashflows_IsEmptyAtOrAfterMaturity()
    {
        var bond = new NtnB("NTN-B 2030", Issue, Maturity);

        bond.GenerateCashflows(Maturity).Should().BeEmpty();
    }

    [Fact]
    public void Constructor_RejectsMaturityNotAfterIssue()
    {
        var act = () => new NtnB("bad", Maturity, Maturity);

        act.Should().Throw<ArgumentException>();
    }
}
