using fixed_income_pricing.Dates;
using fixed_income_pricing.Indices;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Indices;

public class DailyCompoundingIndexTests
{
    private static readonly B3Calendar Calendar = new(2024, 2027);

    private static readonly Dictionary<DateOnly, double> Rates = BuildRates();
    private static readonly CDIIndex Cdi = new(Rates, Calendar);

    private static Dictionary<DateOnly, double> BuildRates()
    {
        var rates = new Dictionary<DateOnly, double>();
        for (var d = new DateOnly(2026, 9, 1); d <= new DateOnly(2026, 9, 30); d = d.AddDays(1))
            if (Calendar.IsBusinessDays(d)) rates[d] = 0.1365;
        rates[new DateOnly(2026, 9, 18)] = 0.15;
        return rates;
    }

    private static double Daily(double annual) => Math.Pow(1 + annual, 1.0 / 252.0);

    [Fact]
    public void AccrualFactor_IsOne_OverAnEmptyPeriod()
    {
        var day = new DateOnly(2026, 9, 21);

        Cdi.AccrualFactor(day, day).Should().Be(1.0);
    }

    [Fact]
    public void AccrualFactor_UsesTheRateFixedAtTheStartOfEachOvernight()
    {
        Cdi.AccrualFactor(new DateOnly(2026, 9, 18), new DateOnly(2026, 9, 21))
            .Should().BeApproximately(Daily(0.15), 1e-15);
    }

    [Fact]
    public void AccrualFactor_CompoundsOneFactorPerBusinessDayInHalfOpenWindow()
    {
        Cdi.AccrualFactor(new DateOnly(2026, 9, 14), new DateOnly(2026, 9, 21))
            .Should().BeApproximately(Math.Pow(Daily(0.1365), 4) * Daily(0.15), 1e-14);
    }

    [Fact]
    public void AccrualFactor_ProjectsFlatAfterTheLastFixing()
    {
        Cdi.AccrualFactor(new DateOnly(2026, 9, 30), new DateOnly(2026, 10, 5))
            .Should().BeApproximately(Math.Pow(Daily(0.1365), 3), 1e-14);
    }

    [Fact]
    public void AccrualFactor_Throws_OnAMissingFixingInsideTheHistory()
    {
        var withHole = new Dictionary<DateOnly, double>(Rates);
        withHole.Remove(new DateOnly(2026, 9, 16));
        var index = new CDIIndex(withHole, Calendar);

        var act = () => index.AccrualFactor(new DateOnly(2026, 9, 14), new DateOnly(2026, 9, 21));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PercentageFactor_AppliesThePercentageToEachDailyRate()
    {
        var start = new DateOnly(2026, 9, 14);
        var end = new DateOnly(2026, 9, 21);
        double expected = Math.Pow(1 + 1.02 * (Daily(0.1365) - 1), 4) * (1 + 1.02 * (Daily(0.15) - 1));

        DailyRateAccrual.Factor(Cdi, start, end, 1.02).Should().BeApproximately(expected, 1e-14);
    }

    [Fact]
    public void ShiftedIndex_AddsTheShiftToEveryAnnualFixing()
    {
        var shifted = new ShiftedDailyRateIndex(Cdi, 0.01);
        var day = new DateOnly(2026, 9, 21);

        (1 + shifted.DailyRate(day)).Should().BeApproximately(Daily(0.1365) * Daily(0.01), 1e-15);
    }
}
