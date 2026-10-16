# Fixed Income Pricing — Brazilian Market

[![CI](https://github.com/leonardosaeta/fixed-income-pricing/actions/workflows/ci.yml/badge.svg)](https://github.com/leonardosaeta/fixed-income-pricing/actions/workflows/ci.yml)

A pricing and risk library for Brazilian fixed income, built from first
principles: business-day calendar → Bus/252 day count → discount curve →
instrument cash flows → pricing engines → bump-and-reprice risk → portfolio
aggregation. No QuantLib, no external pricing library — the point of the
project is the mechanics, and every result is validated against the market's
official numbers (ANBIMA, B3, Banco Central).

## What it prices today

| Instrument | What it is | Validation |
|---|---|---|
| **LTN** | Pré-fixado zero-coupon government bond, R$ 1,000 at maturity | Every LTN in the ANBIMA file reprices to R$ 10⁻⁶ |
| **NTN-F** | Pré-fixado bond, 10% p.a. paid semiannually | Every NTN-F reprices to R$ 10⁻⁶ |
| **NTN-B** | IPCA-linked bond, 6% p.a. real coupon on the VNA | Every NTN-B reprices to R$ 10⁻⁶ given ANBIMA's VNA |
| **CDB pós-fixado** | Bank deposit paying a percentage of CDI | Matches the B3 "DI percentual" accrual formula |
| **DI1 future** | B3 one-day interbank deposit future, 100,000 points at maturity | DI curve reprices every settlement PU; PU and daily-adjustment rounding follow B3 |
| **DI×Pré swap** | Fixed rate against CDI, one settlement at maturity, on the DI curve | Par rate equals the DI curve rate / forward; prices to zero at par |

Plus the market objects behind them:

- **Pré curve** bootstrapped from LTN and NTN-F prices (to 2037), flat-forward
  interpolation; pillar rates equal ANBIMA's indicative rates.
- **CDI / Selic indices** compounded daily from Banco Central fixings.
- **VNA projection** for NTN-B with ANBIMA's business-day pro rata, and an IPCA
  index-number builder with daily interpolation.
- **Risk**: DV01, modified duration, convexity and **key-rate DV01s** by
  bump-and-reprice, aggregated by curve and tenor across a portfolio.

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
  FixedIncome.MarketData   ANBIMA and B3 download clients and file parsers
  FixedIncome.Sandbox      end-to-end pricing cycle on one date (see below)
tests/
  FixedIncome.Core.Tests   xUnit + FluentAssertions, 126 tests
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
- **CDB at accrual, not mark-to-market.** `%CDI` is applied to each daily rate
  (the B3 formula). Its DV01 is the sensitivity to the realised CDI path, and
  it reports no duration rather than a number that would mean nothing for a
  floating-rate deposit.

## Known limitations

- No real (IPCA) curve yet: NTN-Bs are priced on a flat curve at their own
  yield. The bootstrapper supports it; it is not wired up.
- The CDB is valued at its accrued value; there is no projected CDI or credit
  spread yet.
- Holidays are generated from rules (fixed + Easter-based). They match the
  Banco Central publication calendar over 2021–2026, but an ad-hoc national
  holiday would need a code change.
- Market-validation tests download the latest ANBIMA and B3 files and fall
  back to a single committed snapshot in `data/` (see Running it).

## Roadmap

1. **REST API** (`FixedIncome.Api`, ASP.NET Core minimal API): a market
   snapshot service per date, endpoints for curves, prices, risk and portfolio
   reports, OpenAPI docs and integration tests.
2. **Derivatives**: DI1 futures and DI×Pré swaps are in Core; next, a
   `FixedIncome.Derivatives` project for options (models, volatility
   surfaces, calibration), validated against B3 settlement data.

## Running it

Requires the .NET 10 SDK.

```
dotnet build FixedIncome.slnx
dotnet test tests/FixedIncome.Core.Tests
```

The market-validation tests fetch the latest ANBIMA secondary-market and VNA
files and B3 DI1 settlements (or reuse the cached ones) and reprice every LTN,
NTN-F and NTN-B, and rebuild the DI curve from them. If the download fails they
fall back to the snapshot committed in `data/` (2026-10-13). To run against that
snapshot only, with no network access:

```
FIXEDINCOME_OFFLINE=1 dotnet test tests/FixedIncome.Core.Tests
```

CI runs this way on every push.

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
