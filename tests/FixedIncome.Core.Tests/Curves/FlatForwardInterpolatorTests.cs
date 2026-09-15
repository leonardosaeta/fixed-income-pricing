using fixed_income_pricing.Curvers;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Curves;

public class FlatForwardInterpolatorTests
{
    private readonly FlatForwardInterpolator _interpolator = new();

    [Fact]
    public void Interpolate_ClampsToFirstValue_BelowLowerBound()
    {
        double[] x = [1, 2, 3];
        double[] y = [10, 20, 30];

        _interpolator.Interpolate(x, y, 0).Should().Be(10);
    }

    [Fact]
    public void Interpolate_ClampsToLastValue_AboveUpperBound()
    {
        double[] x = [1, 2, 3];
        double[] y = [10, 20, 30];

        _interpolator.Interpolate(x, y, 5).Should().Be(30);
    }

    [Fact]
    public void Interpolate_InterpolatesBetweenPillars()
    {
        double[] x = [1, 3];
        double[] y = [10, 30];

        // Midpoint should sit halfway between the two pillar values
        _interpolator.Interpolate(x, y, 2).Should().Be(20);
    }

    [Fact]
    public void Interpolate_Throws_WhenArraysHaveDifferentLengths()
    {
        double[] x = [1, 2];
        double[] y = [10];

        var act = () => _interpolator.Interpolate(x, y, 1);

        act.Should().Throw<ArgumentException>();
    }
}
