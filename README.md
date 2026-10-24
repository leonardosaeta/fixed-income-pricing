# Fixed Income Pricing — Brazilian Market

[![CI](https://github.com/leonardosaeta/fixed-income-pricing/actions/workflows/ci.yml/badge.svg)](https://github.com/leonardosaeta/fixed-income-pricing/actions/workflows/ci.yml)

A pricing and risk library for Brazilian fixed income, built from first
principles: business-day calendar → Bus/252 day count → discount curve →
instrument cash flows → pricing engines → bump-and-reprice risk → portfolio
aggregation, then options on top. No QuantLib, no external pricing library —
the point of the project is the mechanics. Bonds, futures and curves are
validated against the market's official numbers (ANBIMA, B3, Banco Central);
the option model is verified by identities, finite differences and an
independent Monte Carlo (see [the technical report](docs/idi-options-concepts.pdf)).

## What it prices today

| Instrument | What it is | Validation |
|---|---|---|
| **LTN** | Pré-fixado zero-coupon government bond, R$ 1,000 at maturity | Every LTN in the ANBIMA file reprices to R$ 10⁻⁶ |
| **NTN-F** | Pré-fixado bond, 10% p.a. paid semiannually | Every NTN-F reprices to R$ 10⁻⁶ |
| **NTN-B** | IPCA-linked bond, 6% p.a. real coupon on the VNA | Every NTN-B reprices to R$ 10⁻⁶ given ANBIMA's VNA |
| **CDB pós-fixado** | Bank deposit paying a percentage of CDI | Matches the B3 "DI percentual" accrual formula |
| **DI1 future** | B3 one-day interbank deposit future, 100,000 points at maturity | DI curve reprices every settlement PU; PU and daily-adjustment rounding follow B3 |
| **DI×Pré swap** | Fixed rate against CDI, one settlement at maturity, on the DI curve | Par rate equals the DI curve rate / forward; prices to zero at par |
| **IDI option** | European call/put on the IDI index, strike and premium in index points | Put–call parity, finite-difference Greeks, round-trip implied vol, Monte Carlo cross-check; not yet reconciled to B3 premiums |

Plus the market objects behind them:

- **Pré curve** bootstrapped from LTN and NTN-F prices (to 2037), flat-forward
  interpolation; pillar rates equal ANBIMA's indicative rates.
- **CDI / Selic indices** compounded daily from Banco Central fixings.
- **VNA projection** for NTN-B with ANBIMA's business-day pro rata, and an IPCA
  index-number builder with daily interpolation.
- **DI curve** bootstrapped from B3 DI1 settlements; reprices every contract's
  settlement PU.
- **Volatility surface** on (expiry, log-moneyness), interpolated in total
  variance, with a calendar-arbitrage check at construction.
- **Risk**: DV01, modified duration, convexity and **key-rate DV01s** by
  bump-and-reprice, aggregated by curve and tenor across a portfolio. Options
  add delta, gamma, vega and implied volatility.

## Architecture

```
src/
  FixedIncome.Core         domain library (no third-party dependencies)
    Dates/       IBusinessDayCalendar, B3Calendar, Bus252, Schedule
    Curves/      IYieldCurve, DiscountCurve, FlatForwardInterpolator,
                 IterativeCurveBootstrapper, ParallelShiftedCurve, BucketShiftedCurve
    Indices/     IIndex, IDailyRateIndex, CDIIndex, SelicIndex, IPCAIndex, IpcaVna
    Instruments/ IInstrument, ICashflowInstrument, Ltn, NtnF, NtnB, PostFixedCDB,
                 Di1Future, DiPreSwap
    Market/      MarketContext (named curves, indices and quotes for one date)
    Pricing/     IPricingEngine<T>, CashflowPricingEngine, NtnBPricingEngine, CdbPricingEngine,
                 DiPreSwapPricingEngine
    Risk/        IRiskEngine<T>, CurveRiskEngine<T>, CdbRiskEngine, ICurveShift, KeyRateReport
    Portfolio/   Position, Portfolio, PortfolioRiskReport
    MarketData/  BcbApiSource (BCB SGS API), CsvRateDataSource (local cache)
  FixedIncome.Derivatives  options (depends on Core only)
    Options/     IdiOption, OptionType
    Models/      NormalDistribution, Black76 (price, Greeks, implied vol)
    Volatility/  IVolSurface, FlatVolSurface, GridVolSurface
    Pricing/     IdiOptionPricingEngine (forward from the DI curve, Black-76, Greeks)
  FixedIncome.MarketData   ANBIMA and B3 download clients and file parsers
  FixedIncome.Sandbox      end-to-end pricing cycle on one date (see below)
tests/
  FixedIncome.Core.Tests         xUnit + FluentAssertions, 126 tests
  FixedIncome.Derivatives.Tests  47 tests: model, surface and engine
docs/
  idi-options-concepts.pdf       technical report on the options module
```

A typical valuation:

```csharp
var calendar = new B3Calendar(2020, 2070);
var curve = new IterativeCurveBootstrapper(calendar, new Bus252(), new FlatForwardInterpolator())
    .Bootstrap(date, quotes);                       // LTN + NTN-F prices

var market = new MarketContext(date, calendar, new Bus252())
    .WithCurve(CurveNames.Pre, curve)
    .WithIndex(IndexNames.Cdi, cdiIndex)
    .WithQuote(QuoteNames.NtnBVna, vna);

var engine = new CashflowPricingEngine();           // any instrument with known cash flows
var pv     = engine.Price(ntnf, market);
var risk   = new CurveRiskEngine<ICashflowInstrument>(engine);
var dv01   = risk.Compute(ntnf, market);            // parallel DV01, duration, convexity
var keys   = risk.KeyRates(ntnf, market);           // DV01 per tenor bucket
```

An option on the IDI uses the same market and the same risk engine; the new
input is a volatility surface:

```csharp
var market = new MarketContext(date, calendar, new Bus252())
    .WithCurve(CurveNames.Di, diCurve)              // bootstrapped from DI1 settlements
    .WithQuote(QuoteNames.Idi, idiLevel);           // IDI index level, in points

var surface = new GridVolSurface(expiries, logMoneyness, vols);   // rejects calendar arbitrage
var engine  = new IdiOptionPricingEngine(surface);
var option  = new IdiOption("IDI call", maturity, strike, OptionType.call);

var premium = engine.Price(option, market);         // in points (= R$)
var greeks  = engine.Greeks(option, market);        // delta/gamma w.r.t. the IDI level, vega
var dv01    = new CurveRiskEngine<IdiOption>(engine).Compute(option, market);
var vol     = engine.ImpliedVolatility(option, market, quotedPremium);
```

## Technical decisions worth explaining

- **Bus/252 with ANBIMA's `[start, end)` business-day count.** Brazilian
  accrual is measured in business days / 252. Counting the start date and
  excluding the end date gives the same count for a holiday maturity and for
  its rolled payment date, which is what makes LTN pillars match ANBIMA
  exactly. Counts are O(1) from a precomputed prefix sum.
- **Curve stored as (t, ln DF) pillars, interpolated linearly in log space.**
  Discount factors stay positive and forwards are constant between pillars
  ("flat forward"), the B3 convention for the DI curve. Smoother schemes
  (linear on zero rates, splines) invent forward structure between quotes.
- **Iterative bootstrap.** Each instrument contributes one pillar at its
  maturity; coupon flows that fall between the previous pillar and the new
  one depend on it through the interpolator, so each pillar is solved with a
  safeguarded Newton step. The same routine bootstraps a real curve from
  NTN-B cotações.
- **Everything an engine needs comes from a `MarketContext`.** Curves, indices
  and quotes are looked up by name in one immutable snapshot. Scenarios are
  new contexts, so the engines never know whether they see the real market or
  a bumped one.
- **Risk is bump-and-reprice (±1 bp, central differences).** It works the same
  way for every instrument, including those without a closed form. Key-rate
  buckets use tent weights that sum to one, so key-rate DV01s add up to the
  parallel DV01. Truncation error at 1 bp is below 10⁻⁶ in duration.
- **`decimal` where the market rounds, `double` where the maths lives.**
  ANBIMA-defined quantities (VNA, cotação, PU) use `decimal` with the
  published truncation rules; curves and risk use `double`.
- **One curve projects the forward and discounts the payoff.** The IDI is a
  money-market account, so by cash-and-carry its forward is
  `IDI₀ / P(0,T)` on the DI curve. The option is Black-76 on that forward,
  discounted on the same curve. The engine reads the curve from the
  `MarketContext` on every call, so `CurveRiskEngine` bumps move both the
  forward and the discount factor.
- **Volatility is quoted in log-moneyness and interpolated in total variance.**
  `w = σ²T` is linear between expiries and σ is linear in strike within one,
  flat outside the grid. The constructor rejects surfaces whose total
  variance falls with maturity (calendar arbitrage). Butterfly arbitrage is
  not checked.
- **Implied volatility by safeguarded Newton.** Vega is zero deep out of the
  money, so a Newton step can leave the domain; the solver keeps a bracket and
  falls back to bisection whenever the step lands outside it, and rejects
  premiums outside the no-arbitrage band instead of returning a number.
- **A double-precision normal CDF.** The textbook 10⁻⁷ approximation is too
  coarse for finite-difference Greek tests and 10⁻⁹ implied-vol round trips,
  so `NormalDistribution` uses Hart's rational approximation (West, 2005) and
  computes the tail from the small side.
- **Options are tested four independent ways.** Reference values, identities
  (put–call parity), finite differences for every Greek, and a Monte Carlo of
  the lognormal martingale that never touches the closed form.
- **CDB at accrual, not mark-to-market.** `%CDI` is applied to each daily rate
  (the B3 formula). Its DV01 is the sensitivity to the realised CDI path, and
  it reports no duration rather than a number that would mean nothing for a
  floating-rate deposit.

## Known limitations

- The CDB is valued at its accrued value. The projected CDI index exists (the
  swap uses it) but is not applied to the CDB's remaining life, and there is
  no credit spread.
- There is no IPCA-linked derivative yet (DAP, DI×IPCA swap) and no FX
  product.
- **Options:** the tests use a synthetic IDI level, so there is no source for
  the real index level and no reconciliation with B3's reference premiums or
  implied vols. The surface is linear in strike and does not check butterfly
  arbitrage; there is no smile model (SVI, SABR). Volatility is passed to the
  engine rather than stored in `MarketContext`. Theta and rho are not
  implemented.
- Holidays are generated from rules (fixed + Easter-based). They match the
  Banco Central publication calendar over 2021–2026, but an ad-hoc national
  holiday would need a code change.
- Market-validation tests download the latest ANBIMA and B3 files and fall
  back to a single committed snapshot in `data/` (see Running it).

## Roadmap

1. **REST API** (`FixedIncome.Api`, ASP.NET Core minimal API): a market
   snapshot service per date, endpoints for curves, prices, risk and portfolio
   reports, OpenAPI docs and integration tests.
2. **Derivatives**: DI1 futures, DI×Pré swaps and IDI options are done. Next:
   an IDI level source and reconciliation to B3 option premiums, an
   arbitrage-free smile (SVI or SABR) with calibration, vol as part of
   `MarketContext` with a vol-bump scenario, and a short-rate model
   (Hull–White) calibrated to the DI curve.

## Running it

Requires the .NET 10 SDK.

```
dotnet build FixedIncome.slnx
dotnet test FixedIncome.slnx
```

The market-validation tests fetch the latest ANBIMA secondary-market and VNA
files and B3 DI1 settlements (or reuse the cached ones) and reprice every LTN,
NTN-F and NTN-B, and rebuild the DI curve from them. If the download fails they
fall back to the snapshot committed in `data/` (2026-10-13). To run against that
snapshot only, with no network access:

```
FIXEDINCOME_OFFLINE=1 dotnet test FixedIncome.slnx
```

CI runs this way on every push. The Derivatives tests need no market data:
they build a small DI curve in code.

### Pricing-cycle sandbox

```
dotnet run --project src/FixedIncome.Sandbox -- [yyyy-MM-dd]
```

One file (`src/FixedIncome.Sandbox/Program.cs`) runs the daily cycle for a
sample book: downloads ANBIMA and BCB CDI data, bootstraps the pré and real
curves, then prints valuation and risk, key-rate DV01s, parallel-shift
scenarios and the P&L against the previous business day. Downloaded files are
cached next to the executable.

## License

[MIT](LICENSE). Market data downloaded at runtime belongs to its publishers
(ANBIMA, B3, Banco Central do Brasil) and is subject to their terms of use.
