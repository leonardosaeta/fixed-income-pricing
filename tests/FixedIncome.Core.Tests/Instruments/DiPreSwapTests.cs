using fixed_income_pricing.Curvers;
using fixed_income_pricing.Dates;
using fixed_income_pricing.dates;
using fixed_income_pricing.Indices;
using fixed_income_pricing.Instruments.Futures;
using fixed_income_pricing.Instruments.Swaps;
using fixed_income_pricing.Market;
using fixed_income_pricing.Pricing;
using fixed_income_pricing.Risk;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Instruments;

public class DiPreSwapTests
{
    private static readonly B3Calendar Calendar = new(2020, 2045);
    private static readonly DateOnly Reference = new(2026, 9, 25);
    private const double HistoricalCdi = 0.1365;

    private static readonly DiPreSwapPricingEngine Engine = new();

    private static readonly List<RateQuote> Quotes =
        new[] { ("DI1V26", 0.1490), ("DI1F27", 0.1475), ("DI1N27", 0.1420), ("DI1F28", 0.1390), ("DI1F30", 0.1370) }
            .Select(q => new RateQuote(Di1Future.FromTicker(q.Item1, Calendar), q.Item2))
            .ToList();

    private static MarketContext Market()
    {
        var curve = new IterativeCurveBootstrapper(Calendar, new Bus252(), new FlatForwardInterpolator())
            .Bootstrap(Reference, Quotes);
        var fixings = new Dictionary<DateOnly, double>();
        for (var d = new DateOnly(2025, 9, 1); d < Reference; d = d.AddDays(1))
            if (Calendar.IsBusinessDays(d)) fixings[d] = HistoricalCdi;

        return new MarketContext(Reference, Calendar, new Bus252())
            .WithCurve(CurveNames.Di, curve)
            .WithIndex(IndexNames.Cdi, new CDIIndex(fixings, Calendar));
    }

    private static DiPreSwap Swap(DateOnly start, DateOnly maturity, double rate, SwapSide side) =>
        new("SWAP", start, maturity, side, 1_000_000, rate);

    private static readonly DateOnly Maturity = Di1Future.FromTicker("DI1F28", Calendar).MaturityDate;

    [Fact]
    public void AtThePillarRate_ASwapStartingToday_IsWorthZero()
    {
        var market = Market();
        double pillar = market.Curve(CurveNames.Di).ZeroRate(Maturity);

        var swap = Swap(Reference, Maturity, pillar, SwapSide.PayFixed);

        Engine.Price(swap, market).PresentValue.Should().BeApproximately(0.0, 1e-6);
        Engine.ParRate(swap, market).Should().BeApproximately(pillar, 1e-12);
    }

    [Fact]
    public void ParRate_OfAForwardStartingSwap_IsTheForwardRate_AndPricesToZero()
    {
        var market = Market();
        var start = Di1Future.FromTicker("DI1F27", Calendar).MaturityDate;
        var swap = Swap(start, Maturity, 0.0, SwapSide.PayFixed);

        double par = Engine.ParRate(swap, market);

        var curve = market.Curve(CurveNames.Di);
        int duStart = Calendar.CountBusinessDaysBetween(Reference, start);
        int duEnd = Calendar.CountBusinessDaysBetween(Reference, Maturity);
        double forward = Math.Pow(curve.DiscountFactor(start) / curve.DiscountFactor(Maturity),
            252.0 / (duEnd - duStart)) - 1.0;
        par.Should().BeApproximately(forward, 1e-9);
        Engine.Price(Swap(start, Maturity, par, SwapSide.PayFixed), market).PresentValue
            .Should().BeApproximately(0.0, 1e-6);
    }

    [Fact]
    public void PayAndReceiveFixed_HaveOppositeValues()
    {
        var market = Market();

        var pay = Engine.Price(Swap(Reference, Maturity, 0.12, SwapSide.PayFixed), market).PresentValue;
        var receive = Engine.Price(Swap(Reference, Maturity, 0.12, SwapSide.ReceiveFixed), market).PresentValue;

        pay.Should().BePositive();       // paying 12% while the market is ~13.9%
        receive.Should().BeApproximately(-pay, 1e-9);
    }

    [Fact]
    public void Dv01_IsNegativeForPayFixed_AndPositiveForReceiveFixed()
    {
        var market = Market();
        var risk = new CurveRiskEngine<DiPreSwap>(Engine);

        // DV01 here is (PV(-1bp) - PV(+1bp)) / 2: pay-fixed is short duration, like a short bond.
        risk.Compute(Swap(Reference, Maturity, 0.139, SwapSide.PayFixed), market).Dv01.Should().BeNegative();
        risk.Compute(Swap(Reference, Maturity, 0.139, SwapSide.ReceiveFixed), market).Dv01.Should().BePositive();
    }

    [Fact]
    public void ASwapThatStartedInThePast_UsesPublishedFixings()
    {
        var market = Market();
        var start = Calendar.AddBusinessDay(Reference, -10);
        var swap = Swap(start, Maturity, 0.139, SwapSide.PayFixed);

        // Floating leg = fixings accrued so far × curve growth to maturity.
        var fixings = market.Index<IDailyRateIndex>(IndexNames.Cdi);
        double accrued = fixings.AccrualFactor(start, Reference);
        double expectedFloating = 1_000_000 * accrued / market.Curve(CurveNames.Di).DiscountFactor(Maturity);

        double pv = Engine.Price(swap, market).PresentValue;

        double expected = (expectedFloating - swap.FixedLegAmount(Calendar)) *
                          market.Curve(CurveNames.Di).DiscountFactor(Maturity);
        pv.Should().BeApproximately(expected, 1e-6);
    }

    [Fact]
    public void AtOrAfterMaturity_TheSwapIsWorthZero()
    {
        var market = Market();
        var swap = Swap(Calendar.AddBusinessDay(Reference, -300), Reference, 0.12, SwapSide.PayFixed);

        Engine.Price(swap, market).PresentValue.Should().Be(0.0);
    }

    [Fact]
    public void Constructor_RejectsMaturityBeforeStart()
    {
        var act = () => Swap(Maturity, Reference, 0.12, SwapSide.PayFixed);

        act.Should().Throw<ArgumentException>();
    }
}
