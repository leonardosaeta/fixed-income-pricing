using FixedIncome.Derivatives.Models;
using FluentAssertions;

namespace FixedIncome.Derivatives.Tests;

public class NormalDistributionTests
{
    [Theory]
    [InlineData(0.0, 0.5)]
    [InlineData(1.0, 0.8413447460685429)]
    [InlineData(-1.0, 0.15865525393145707)]
    [InlineData(1.96, 0.9750021048517795)]
    [InlineData(-3.0, 0.0013498980316300946)]
    [InlineData(-8.0, 6.220960574271785e-16)]
    public void Cdf_MatchesReferenceValues(double x, double expected)
    {
        NormalDistribution.Cdf(x).Should().BeApproximately(expected, Math.Max(1e-14, expected * 1e-9));
    }

    [Theory]
    [InlineData(0.3)]
    [InlineData(1.7)]
    [InlineData(5.0)]
    public void Cdf_IsSymmetric(double x)
    {
        (NormalDistribution.Cdf(x) + NormalDistribution.Cdf(-x)).Should().BeApproximately(1.0, 1e-14);
    }

    [Fact]
    public void Pdf_IsTheDerivativeOfTheCdf()
    {
        const double x = 0.8, h = 1e-5;

        double numeric = (NormalDistribution.Cdf(x + h) - NormalDistribution.Cdf(x - h)) / (2 * h);

        NormalDistribution.Pdf(x).Should().BeApproximately(numeric, 1e-9);
    }
}
