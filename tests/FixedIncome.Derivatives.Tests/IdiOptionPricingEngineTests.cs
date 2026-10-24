using FixedIncome.Derivatives.Models;
using FixedIncome.Derivatives.Options;
using FixedIncome.Derivatives.Pricing;
using FixedIncome.Derivatives.Volatility;
using fixed_income_pricing.Curvers;
using fixed_income_pricing.Dates;
using fixed_income_pricing.dates;
using fixed_income_pricing.Instruments.Futures;
using fixed_income_pricing.Market;
using fixed_income_pricing.Risk;
using FluentAssertions;

namespace FixedIncome.Derivatives.Tests;

public class IdiOptionPricingEngineTests
{
    private static readonly B3Calendar Calendar = new(2020, 2045);
    private static readonly DateOnly Reference = new(2026, 9, 25);
    private static readonly DateOnly Maturity = Di1Future.FromTicker("DI1F28", Calendar).MaturityDate;
    private const decimal IdiLevel = 1_000_000m; 
    private const double Vol = 0.18;

    private static readonly IdiOptionPricingEngine Engine = new(new FlatVolSurface(Vol));

    private static MarketContext Market()
    {
        var quotes = new[] { ("DI1V26", 0.1490), ("DI1F27", 0.1475), ("DI1N27", 0.1420), ("DI1F28", 0.1390), ("DI1F30", 0.1370) }
            .Select(q => new RateQuote(Di1Future.FromTicker(q.Item1, Calendar), q.Item2));
        var curve = new IterativeCurveBootstrapper(Calendar, new Bus252(), new FlatForwardInterpolator())
            .Bootstrap(Reference, quotes);

        return new MarketContext(Reference, Calendar, new Bus252())
            .WithCurve(CurveNames.Di, curve)
            .WithQuote(QuoteNames.Idi, IdiLevel);
    }

    private static double Forward(MarketContext m) => (double)IdiLevel / m.Curve(CurveNames.Di).DiscountFactor(Maturity);

    private static IdiOption Option(double strike, OptionType type) => new($"IDI {type} {strike:N0}", Maturity, strike, type);

    [Fact]
    public void Price_IsBlack76OnTheForwardImpliedByTheDiCurve()
    {
        var market = Market();
        var option = Option(1_300_000, OptionType.call);
        double df = market.Curve(CurveNames.Di).DiscountFactor(Maturity);
        double t = Calendar.CountBusinessDaysBetween(Reference, Maturity) / 252.0;

        double expected = Black76.Price(Forward(market), 1_300_000, Vol, t, df, OptionType.call);

        Engine.Price(option, market).PresentValue.Should().BeApproximately(expected, 1e-6);
    }

    [Fact]
    public void ForwardOfTheIdi_GrowsAtTheDiCurveRate()
    {
        var market = Market();
        int du = Calendar.CountBusinessDaysBetween(Reference, Maturity);

        Forward(market).Should().BeApproximately(
            (double)IdiLevel * Math.Pow(1 + market.Curve(CurveNames.Di).ZeroRate(Maturity), du / 252.0), 1e-3);
    }

    [Theory]
    [InlineData(1_100_000)]
    [InlineData(1_300_000)]
    [InlineData(1_500_000)]
    public void PutCallParity_Holds_InPoints(double strike)
    {
        var market = Market();
        double df = market.Curve(CurveNames.Di).DiscountFactor(Maturity);

        double call = Engine.Price(Option(strike, OptionType.call), market).PresentValue;
        double put = Engine.Price(Option(strike, OptionType.put), market).PresentValue;

        (call - put).Should().BeApproximately((double)IdiLevel - strike * df, 1e-6);
    }

    [Fact]
    public void Delta_IsTheNormalCdf_AndGammaIsPositive()
    {
        var market = Market();
        var call = Engine.Greeks(Option(1_300_000, OptionType.call), market);
        var put = Engine.Greeks(Option(1_300_000, OptionType.put), market);

        call.Delta.Should().BeInRange(0, 1);
        put.Delta.Should().BeInRange(-1, 0);
        (call.Delta - put.Delta).Should().BeApproximately(1.0, 1e-12); 
        call.Gamma.Should().BePositive();
        call.Vega.Should().BePositive();
    }

    [Fact]
    public void Delta_MatchesBumpingTheIdiLevel()
    {
        var market = Market();
        var option = Option(1_300_000, OptionType.call);
        const decimal h = 100m;

        double up = Engine.Price(option, market.WithQuote(QuoteNames.Idi, IdiLevel + h)).PresentValue;
        double down = Engine.Price(option, market.WithQuote(QuoteNames.Idi, IdiLevel - h)).PresentValue;

        Engine.Greeks(option, market).Delta.Should().BeApproximately((up - down) / (2 * (double)h), 1e-6);
    }

    [Fact]
    public void Dv01_ComesThroughTheEngine_AndHasTheSignOfRatesExposure()
    {
        var market = Market();
        var risk = new CurveRiskEngine<IdiOption>(Engine);

        risk.Compute(Option(1_300_000, OptionType.call), market).Dv01.Should().BeNegative();
        risk.Compute(Option(1_300_000, OptionType.put), market).Dv01.Should().BePositive();
    }

    [Fact]
    public void ImpliedVolatility_RecoversTheSurfaceVol_FromTheEnginePremium()
    {
        var market = Market();
        var option = Option(1_400_000, OptionType.put);
        double premium = Engine.Price(option, market).PresentValue;

        Engine.ImpliedVolatility(option, market, premium).Should().BeApproximately(Vol, 1e-9);
    }

    [Fact]
    public void SkewedSurface_ChangesThePriceOfOutOfTheMoneyStrikes()
    {
        var market = Market();
        var skewed = new IdiOptionPricingEngine(new GridVolSurface([0.5, 5.0], [-0.3, 0.0, 0.3], [[0.30, 0.18, 0.12], [0.30, 0.18, 0.12]]));
        var otmPut = Option(1_000_000 * 0.95, OptionType.put);

        skewed.Price(otmPut, market).PresentValue.Should().BeGreaterThan(Engine.Price(otmPut, market).PresentValue);
    }

    [Fact]
    public void Payoff_IsTheIntrinsicValueInReais()
    {
        var call = Option(900_000, OptionType.call);
        var put = Option(900_000, OptionType.put);

        call.Payoff(1_000_000).Should().Be(100_000);
        call.Payoff(800_000).Should().Be(0);
        put.Payoff(800_000).Should().Be(100_000);
        put.Payoff(1_000_000).Should().Be(0);
    }

    [Fact]
    public void BeforeExpiry_AnInTheMoneyCall_IsWorthMoreThanItsDiscountedIntrinsicValue()
    {
        var market = Market();
        var option = Option(900_000, OptionType.call); 
        double df = market.Curve(CurveNames.Di).DiscountFactor(Maturity);

        Engine.Price(option, market).PresentValue.Should().BeGreaterThan(df * (Forward(market) - 900_000));
    }

    [Fact]
    public void Constructor_RejectsANonPositiveStrike()
    {
        var act = () => Option(0, OptionType.call);

        act.Should().Throw<ArgumentException>();
    }
}
