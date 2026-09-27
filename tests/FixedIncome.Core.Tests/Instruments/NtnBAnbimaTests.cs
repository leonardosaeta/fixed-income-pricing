using fixed_income_pricing.Instruments.Government;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Instruments;

public class NtnBAnbimaTests
{
    private const decimal Tolerance = 0.01m;

    public static TheoryData<DateOnly, DateOnly, decimal, decimal> NtnBQuotes()
    {
        var data = new TheoryData<DateOnly, DateOnly, decimal, decimal>();
        foreach (var q in AnbimaTestData.Quotes("NTN-B"))
        {
            data.Add(q.ReferenceDate, q.Maturity, q.IndicativeRate, q.Pu);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(NtnBQuotes))]
    public void NtnB_Pu_Matches_Anbima_GivenPublishedVna(
        DateOnly referenceDate, DateOnly maturity, decimal realRate, decimal expectedPu)
    {
        var vna = AnbimaTestData.Vna("NTN-B");
        var pu = new NtnB($"NTN-B {maturity:yyyyMMdd}", referenceDate, maturity).Price(referenceDate, realRate, vna, AnbimaTestData.Calendar);

        pu.Should().BeApproximately(expectedPu, Tolerance,
            $"NTN-B {maturity:dd/MM/yyyy} at {realRate:P4}, VNA={vna}, ratio={pu / expectedPu:F8}");
    }
}