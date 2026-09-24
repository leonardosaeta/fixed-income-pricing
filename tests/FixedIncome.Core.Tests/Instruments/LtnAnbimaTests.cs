using fixed_income_pricing.Dates;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Instruments;

public class LtnAnbimaTests
{
    private const decimal Face = 1000m;
    private const decimal Tolerance = 1e-4m;

    private static B3Calendar Calendar => AnbimaTestData.Calendar;

    public static TheoryData<DateOnly, DateOnly, decimal, decimal> LtnQuotes()
    {
        var data = new TheoryData<DateOnly, DateOnly, decimal, decimal>();
        foreach (var q in AnbimaTestData.Quotes("LTN"))
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
        var payment = Calendar.IsBusinessDays(maturity) ? maturity : Calendar.AddBusinessDay(maturity, 1);
        var du = Calendar.CountBusinessDaysBetween(referenceDate, payment);

        var pu = Face / (decimal)Math.Pow(1.0 + (double)rate, du / 252.0);
        pu.Should().BeApproximately(expectedPu, Tolerance, $"LTN {maturity:dd/MM/yyyy} at {rate:p4}, du={du}");
    }
    

}