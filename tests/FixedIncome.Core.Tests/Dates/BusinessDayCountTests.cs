using fixed_income_pricing.Dates;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Dates;

public class BusinessDayCountTests
{
    private readonly B3Calendar _calendar = new(2024, 2030);

    [Fact]
    public void Count_IncludesStartAndExcludesEnd()
    {
        _calendar.CountBusinessDaysBetween(new DateOnly(2025, 1, 6), new DateOnly(2025, 1, 10)).Should().Be(4);
    }

    [Fact]
    public void Count_IsTheSameForAHolidayMaturityAndItsRolledPaymentDate()
    {
        var reference = new DateOnly(2026, 9, 25);

        _calendar.CountBusinessDaysBetween(reference, new DateOnly(2028, 1, 1))
            .Should().Be(_calendar.CountBusinessDaysBetween(reference, new DateOnly(2028, 1, 3)))
            .And.Be(317);
    }

    [Fact]
    public void Count_MatchesDayByDayLoop()
    {
        var random = new Random(42);
        var origin = new DateOnly(2024, 1, 1);

        for (int i = 0; i < 500; i++)
        {
            var a = origin.AddDays(random.Next(0, 2500));
            var b = origin.AddDays(random.Next(0, 2500));

            _calendar.CountBusinessDaysBetween(a, b).Should().Be(Naive(a, b), $"between {a} and {b}");
        }
    }

    [Fact]
    public void Count_Throws_OutsideCalendarRange()
    {
        var act = () => _calendar.CountBusinessDaysBetween(new DateOnly(2025, 1, 1), new DateOnly(2031, 1, 2));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private int Naive(DateOnly a, DateOnly b)
    {
        if (a > b) return -Naive(b, a);
        int count = 0;
        for (var d = a; d < b; d = d.AddDays(1))
            if (_calendar.IsBusinessDays(d)) count++;
        return count;
    }
}
