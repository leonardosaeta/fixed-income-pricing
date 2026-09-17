using fixed_income_pricing.Dates;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Dates;

public class B3CalendarTests
{
    private readonly B3Calendar _calendar = new(2024, 2026);

    [Fact]
    public void IsHoliday_ReturnsTrue_ForFixedNationalHoliday()
    {
        _calendar.IsHoliday(new DateTime(2025, 1, 1)).Should().BeTrue();
    }

    [Fact]
    public void IsBusinessDays_ReturnsFalse_ForWeekend()
    {
        _calendar.IsBusinessDays(new DateTime(2025, 1, 4)).Should().BeFalse();
    }

    [Fact]
    public void AddBusinessDay_SkipsWeekend()
    {
        // 2025-01-03 is a Friday, next business day should be Monday 2025-01-06
        var result = _calendar.AddBusinessDay(new DateTime(2025, 1, 3), 1);

        result.Should().Be(new DateTime(2025, 1, 6));
    }

    [Fact]
    public void CountBusinessDaysBetween_IsSymmetricUnderSwap()
    {
        var start = new DateTime(2025, 1, 2);
        var end = new DateTime(2025, 1, 6);

        _calendar.CountBusinessDaysBetween(start, end)
            .Should().Be(-_calendar.CountBusinessDaysBetween(end, start));
    }

    [Fact]
    public void Constructor_Throws_WhenMinYearGreaterThanMaxYear()
    {
        var act = () => new B3Calendar(2026, 2024);

        act.Should().Throw<ArgumentException>();
    }
}
