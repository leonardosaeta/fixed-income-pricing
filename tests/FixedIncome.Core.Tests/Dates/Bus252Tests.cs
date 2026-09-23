using fixed_income_pricing.dates;
using fixed_income_pricing.Dates;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Dates;

public class Bus252Tests
{
    private readonly Bus252 _convention = new();
    private readonly B3Calendar _calendar = new(2024, 2026);

    [Fact]
    public void YearFraction_DividesBusinessDaysBy252()
    {
        var start = new DateOnly(2025, 1, 2);
        var end = new DateOnly(2025, 1, 6);

        // 2 business days between start and end (2025-01-03 and 2025-01-06)
        _convention.YearFraction(start, end, _calendar).Should().Be(2 / 252.0);
    }

    [Fact]
    public void YearFraction_IsZero_WhenStartEqualsEnd()
    {
        var date = new DateOnly(2025, 1, 2);

        _convention.YearFraction(date, date, _calendar).Should().Be(0);
    }
}
