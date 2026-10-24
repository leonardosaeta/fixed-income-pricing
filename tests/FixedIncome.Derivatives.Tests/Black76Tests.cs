using FixedIncome.Derivatives.Models;
using FixedIncome.Derivatives.Options;
using FluentAssertions;

namespace FixedIncome.Derivatives.Tests;

public class Black76Tests
{
    private const double Forward = 100.0, Strike = 105.0, Vol = 0.20, T = 1.5, Df = 0.92;

    [Fact]
    public void AtTheMoney_WithUnitDiscount_MatchesTheClosedForm()
    {
        double expected = 100.0 * (2 * NormalDistribution.Cdf(0.5 * 0.2) - 1);

        Black76.Price(100, 100, 0.2, 1.0, 1.0, OptionType.call).Should().BeApproximately(expected, 1e-12);
        Black76.Price(100, 100, 0.2, 1.0, 1.0, OptionType.call).Should().BeApproximately(7.9655674554, 1e-8);
    }

    [Theory]
    [InlineData(80.0)]
    [InlineData(100.0)]
    [InlineData(125.0)]
    public void PutCallParity_Holds(double strike)
    {
        double call = Black76.Price(Forward, strike, Vol, T, Df, OptionType.call);
        double put = Black76.Price(Forward, strike, Vol, T, Df, OptionType.put);

        (call - put).Should().BeApproximately(Df * (Forward - strike), 1e-12);
    }

    [Fact]
    public void Price_IsIncreasingInVol_AndBoundedByNoArbitrage()
    {
        double previous = 0;
        foreach (var vol in new[] { 0.05, 0.1, 0.2, 0.4, 0.8 })
        {
            double price = Black76.Price(Forward, Strike, vol, T, Df, OptionType.call);
            price.Should().BeGreaterThan(previous);
            price.Should().BeInRange(Df * Math.Max(Forward - Strike, 0), Df * Forward);
            previous = price;
        }
    }

    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(0.2, 0.0)]
    public void ZeroVolOrZeroTime_GivesDiscountedIntrinsicValue(double vol, double t)
    {
        Black76.Price(110, 100, vol, t, 0.9, OptionType.call).Should().BeApproximately(0.9 * 10, 1e-12);
        Black76.Price(110, 100, vol, t, 0.9, OptionType.put).Should().Be(0.0);
    }

    [Theory]
    [InlineData(OptionType.call)]
    [InlineData(OptionType.put)]
    public void Greeks_MatchFiniteDifferences(OptionType type)
    {
        var g = Black76.Greeks(Forward, Strike, Vol, T, Df, type);
        double Price(double f, double v) => Black76.Price(f, Strike, v, T, Df, type);
        const double hf = 1e-3, hv = 1e-5;

        g.Price.Should().BeApproximately(Price(Forward, Vol), 1e-14);
        g.Delta.Should().BeApproximately((Price(Forward + hf, Vol) - Price(Forward - hf, Vol)) / (2 * hf), 1e-7);
        g.Gamma.Should().BeApproximately((Price(Forward + hf, Vol) - 2 * Price(Forward, Vol) + Price(Forward - hf, Vol)) / (hf * hf), 1e-5);
        g.Vega.Should().BeApproximately((Price(Forward, Vol + hv) - Price(Forward, Vol - hv)) / (2 * hv), 1e-6);
    }

    [Fact]
    public void CallAndPutShareGammaAndVega_AndDeltasDifferByTheDiscountFactor()
    {
        var call = Black76.Greeks(Forward, Strike, Vol, T, Df, OptionType.call);
        var put = Black76.Greeks(Forward, Strike, Vol, T, Df, OptionType.put);

        put.Gamma.Should().BeApproximately(call.Gamma, 1e-14);
        put.Vega.Should().BeApproximately(call.Vega, 1e-12);
        (call.Delta - put.Delta).Should().BeApproximately(Df, 1e-14);
    }

    [Theory]
    [InlineData(OptionType.call, 80.0, 0.05)]
    [InlineData(OptionType.call, 100.0, 0.20)]
    [InlineData(OptionType.call, 140.0, 0.75)]
    [InlineData(OptionType.put, 80.0, 0.12)]
    [InlineData(OptionType.put, 120.0, 0.45)]
    public void ImpliedVolatility_RecoversTheInputVol(OptionType type, double strike, double vol)
    {
        double price = Black76.Price(Forward, strike, vol, T, Df, type);

        Black76.ImpliedVolatility(price, Forward, strike, T, Df, type).Should().BeApproximately(vol, 1e-9);
    }

    [Fact]
    public void ImpliedVolatility_RejectsPricesNoVolCanProduce()
    {
        double intrinsic = Df * (Forward - 80.0);

        var below = () => Black76.ImpliedVolatility(intrinsic - 0.01, Forward, 80.0, T, Df, OptionType.call);
        var above = () => Black76.ImpliedVolatility(Df * Forward + 0.01, Forward, 80.0, T, Df, OptionType.call);

        below.Should().Throw<ArgumentException>();
        above.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MonteCarlo_AgreesWithTheClosedForm()
    {
        var rng = new Random(12345);
        const int n = 400_000;
        double sum = 0, sumSq = 0;

        for (int i = 0; i < n; i += 2)
        {
            double z = Math.Sqrt(-2 * Math.Log(1 - rng.NextDouble())) * Math.Cos(2 * Math.PI * rng.NextDouble());
            foreach (var zz in new[] { z, -z }) 
            {
                double ft = Forward * Math.Exp(-0.5 * Vol * Vol * T + Vol * Math.Sqrt(T) * zz);
                double payoff = Df * Math.Max(ft - Strike, 0);
                sum += payoff;
                sumSq += payoff * payoff;
            }
        }

        double mean = sum / n;
        double standardError = Math.Sqrt((sumSq / n - mean * mean) / n);

        mean.Should().BeApproximately(Black76.Price(Forward, Strike, Vol, T, Df, OptionType.call), 4 * standardError);
    }
}
