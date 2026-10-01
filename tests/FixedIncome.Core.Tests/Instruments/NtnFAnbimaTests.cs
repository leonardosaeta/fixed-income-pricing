using fixed_income_pricing.Instruments.Government;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Instruments;

public class NtnFAnbimaTests
{
    public static TheoryData<DateOnly, DateOnly, decimal, decimal> NtnFQuotes()
    {
        var data = new TheoryData<DateOnly, DateOnly, decimal, decimal>();
        foreach (var q in AnbimaTestData.Quotes("NTN-F"))
            data.Add(q.ReferenceDate, q.Maturity, q.IndicativeRate, q.Pu);
        return data;
    }

    [Theory]
    [MemberData(nameof(NtnFQuotes))]
    public void NtnF_Pu_Matches_Anbima(DateOnly referenceDate, DateOnly maturity, decimal rate, decimal expectedPu)
    {
        var bond = new NtnF($"NTN-F {maturity:yyyyMMdd}", referenceDate.AddYears(-10), maturity);

        bond.Price(referenceDate, rate, AnbimaTestData.Calendar).Should().BeApproximately(expectedPu, 0.000001m);
    }
}
