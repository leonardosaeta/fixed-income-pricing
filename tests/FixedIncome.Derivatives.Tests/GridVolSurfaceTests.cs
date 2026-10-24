using FixedIncome.Derivatives.Volatility;
using FluentAssertions;

namespace FixedIncome.Derivatives.Tests;

public class GridVolSurfaceTests
{
    private static readonly double[] Expiries = [0.25, 1.0];
    private static readonly double[] Ks = [-0.10, 0.0, 0.10];
    private static readonly double[][] Vols = [[0.30, 0.25, 0.28], [0.26, 0.22, 0.24]];

    private static GridVolSurface Surface() => new(Expiries, Ks, Vols);

    [Fact]
    public void ReproducesTheQuotedNodes()
    {
        for (int i = 0; i < Expiries.Length; i++)
        for (int j = 0; j < Ks.Length; j++)
            Surface().Volatility(Expiries[i], Ks[j]).Should().BeApproximately(Vols[i][j], 1e-12);
    }

    [Fact]
    public void InterpolatesLinearlyInStrike_WithinAnExpiry()
    {
        Surface().Volatility(0.25, 0.05).Should().BeApproximately(0.265, 1e-12);
    }

    [Fact]
    public void InterpolatesLinearlyInTotalVariance_BetweenExpiries()
    {
        double t = 0.625;
        double w = 0.5 * (0.25 * 0.25 * 0.25) + 0.5 * (0.22 * 0.22 * 1.0);

        Surface().Volatility(t, 0.0).Should().BeApproximately(Math.Sqrt(w / t), 1e-12);
    }

    [Fact]
    public void ExtrapolatesFlat()
    {
        Surface().Volatility(0.01, 0.0).Should().BeApproximately(0.25, 1e-12);
        Surface().Volatility(5.0, 0.0).Should().BeApproximately(0.22, 1e-12);
        Surface().Volatility(0.25, -1.0).Should().BeApproximately(0.30, 1e-12);
        Surface().Volatility(0.25, 1.0).Should().BeApproximately(0.28, 1e-12);
    }

    [Fact]
    public void AcceptsAFallingVolTermStructure_WhenTotalVarianceStillRises()
    {
        var act = () => new GridVolSurface([0.25, 1.0], [0.0], [[0.25], [0.22]]);

        act.Should().NotThrow();
    }

    [Fact]
    public void RejectsCalendarArbitrage()
    {
        var act = () => new GridVolSurface([0.25, 1.0], [0.0], [[0.50], [0.15]]);

        act.Should().Throw<ArgumentException>().WithMessage("*calendar arbitrage*");
    }

    [Fact]
    public void RejectsMalformedGrids()
    {
        new Action(() => new GridVolSurface([1.0, 0.5], [0.0], [[0.2], [0.2]])).Should().Throw<ArgumentException>();
        new Action(() => new GridVolSurface([0.5, 1.0], [0.1, 0.0], [[0.2, 0.2], [0.2, 0.2]])).Should().Throw<ArgumentException>();
        new Action(() => new GridVolSurface([0.5, 1.0], [0.0], [[0.2]])).Should().Throw<ArgumentException>();
        new Action(() => new GridVolSurface([0.5], [0.0], [[-0.2]])).Should().Throw<ArgumentException>();
    }
}
