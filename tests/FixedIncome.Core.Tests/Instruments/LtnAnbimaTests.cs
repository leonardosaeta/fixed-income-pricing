using fixed_income_pricing.Dates;
using FixedIncome.MarketData.Anbima;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Instruments;

public class LtnAnbimaTests
{
    private const decimal Face = 1000m;
    private const decimal Tolerance = 1e-4m;

    private static readonly B3Calendar Calendar = new(2020, 2050);

    private static string DataFile => Path.Combine(AppContext.BaseDirectory, "data", "anbima", "ms260915.txt");

    public static TheoryData<DateOnly, DateOnly, decimal, decimal> LtnQuotes()
    {
        var data = new TheoryData<DateOnly, DateOnly, decimal, decimal>();
        foreach (var q in AnbimaSecondaryMarketFile.Parse(DataFile)
                     .Where(q => q.Instrument.Equals("LTN", StringComparison.OrdinalIgnoreCase)))
        {
            data.Add(q.ReferenceDate, q.Maturity, q.IndicativeRate, q.Pu);
            
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(LtnQuotes))]
    public void Ltn_Pu_Matches_Anbima(
        DateOnly referenceDate, DateOnly maturity, decimal rate, decimal expectedPu)
    {
        var du = Calendar.CountBusinessDaysBetween(referenceDate, maturity);

        var pu = Face / (decimal)Math.Pow(1.0 + (double)rate, du / 252.0);
        pu.Should().BeApproximately(expectedPu, Tolerance, $"LTN {maturity:dd/MM/yyyy} at {rate:p4}, du={du}");
    }
    

}