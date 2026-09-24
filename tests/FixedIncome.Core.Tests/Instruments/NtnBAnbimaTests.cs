using fixed_income_pricing.Dates;
using fixed_income_pricing.Instruments.Government;
using FixedIncome.MarketData.Anbima;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Instruments;

public class NtnBAnbimaTests
{
    private const decimal PublishedVna = 0m;   // TODO: fill in from ANBIMA

    private const decimal Tolerance = 0.01m;

    private static readonly B3Calendar Calendar = new(2020, 2070);

    private static string DataFile =>
        Path.Combine(AppContext.BaseDirectory, "data", "anbima", "ms260915.txt");

    public static TheoryData<DateOnly, DateOnly, decimal, decimal> NtnBQuotes()
    {
        var data = new TheoryData<DateOnly, DateOnly, decimal, decimal>();
        foreach (var q in AnbimaSecondaryMarketFile.Parse(DataFile)
                     .Where(q => q.Instrument.Equals("NTN-B", StringComparison.OrdinalIgnoreCase)))
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
        PublishedVna.Should().BePositive("fill in the ANBIMA VNA for the reference date");

        var pu = new NtnB(maturity).Price(referenceDate, realRate, PublishedVna, Calendar);

        pu.Should().BeApproximately(expectedPu, Tolerance,
            $"NTN-B {maturity:dd/MM/yyyy} at {realRate:P4}, ratio={pu / expectedPu:F8}");
    }
}