# Fixed Income Pricing — Brazilian Market

A small pricing and risk library for Brazilian fixed-income instruments, built up
from first principles: business-day calendar → day count → discount curve →
instrument cash flows → pricing engine → bump-and-reprice risk → portfolio
aggregation. No QuantLib, no external pricing lib — the point of the project is
the mechanics, not the shortcut.

## What it prices today

- **LTN** (Letra do Tesouro Nacional) — pré-fixado zero-coupon government bond.
  A single cash flow of face value (1000) at maturity, discounted off a
  bootstrapped curve.
- **CDB pós-fixado** — bank certificate of deposit paying a percentage of CDI,
  with daily compounding accrual.
- A **pré-fixado discount curve**, bootstrapped directly from LTN market
  prices (implied annualized yield per bond → curve pillar).
- An **IPCA index number builder** with disclosure-lag pro-rata interpolation —
  built as the foundation for an NTN-B (inflation-linked bond) instrument, not
  yet wired to a pricing engine. **SELIC** index is scaffolded the same way,
  for a future LFT.

## Architecture

```
Dates/       IBusinessDayCalendar, IDayCountConvention, B3Calendar, Bus252, Schedule
Curvers/     IYieldCurve, IInterpolator, DiscountCurve, ICurveBootstrapper, ParallelShiftedCurve
Indices/     IIndex, DailyCompoundingIndex (CDI/Selic), IPCAIndex, ShiftedIndex
Instruments/ IInstrument, Ltn, PostFixedCDB
CashFlows/   Cashflow record
Pricing/     IPricingEngine<T>, GovernmentBondPricingEngine, CdbPricingEngine
Risk/        IRiskEngine<T>, GovernmentBondRiskEngine, CdbRiskEngine, RiskMetric
Portfolio/   Position, PortfolioRiskReport, PositionFactory, Portfolio
MarketData/  IRateDataSource, BcbApiSource (BCB SGS API), CsvRateDataSource (local cache)
```

Everything upstream of `Instruments/` is convention-driven interfaces
(calendar, day count, curve, index) rather than concrete types. Pricing and
risk engines are generic per instrument (`IPricingEngine<Ltn>`,
`IRiskEngine<PostFixedCDB>`), so adding NTN-B means writing a new instrument +
engine pair, not touching the curve or date machinery.

## Technical decisions worth explaining

- **Bus/252 day count over B3Calendar.** This is the BR market's own
  convention — accrual is measured in *business days* / 252, not calendar
  days / 365. Getting this wrong is the single most common way to misprice a
  BR curve, so it's the first thing implemented (`Dates/Bus252.cs`).
- **Curve stored as (t, ln DF) pillars, interpolated in log space.** Guarantees
  discount factors stay positive and implies a piecewise-constant forward
  rate between pillars ("flat forward") — the standard way to avoid
  oscillating/negative forwards you'd get interpolating DF or zero rate
  directly.
- **LTN bootstrap is a closed-form yield solve, not an iterative bootstrap.**
  Because an LTN has exactly one cash flow, `price = face / (1+y)^t` inverts
  directly. A real multi-instrument bootstrap (coupon bonds, swaps) needs an
  iterative solve since each new pillar's PV depends on the curve being
  built — that's the natural next step here, not yet needed with LTN-only
  input.
- **Risk is bump-and-reprice (±1bp), not closed-form duration.** A parallel
  curve shift for the LTN, an index shift for the CDI-linked CDB. This is
  slower than analytic duration but it's the only approach that works
  *uniformly* across instrument types — including the CDB, where "modified
  duration" isn't a well-defined concept against a floating index. That's why
  `CdbRiskEngine` reports `ModifiedDuration: null` alongside a real DV01
  instead of forcing a number that would be a fiction.
- **Portfolio duration is PV-weighted, and only over positions that have
  one.** Averaging duration across a pré-fixado bond and a CDI-linked CDB
  would produce a number with no risk meaning, so `Portfolio.BuildReport`
  excludes positions with a null duration from that average rather than
  treating null as zero.
- **Market data has two sources behind one interface** (`IRateDataSource`):
  `BcbApiSource` pulls live series from the Banco Central SGS API,
  `CsvRateDataSource` reads/writes a local cache — so pricing runs are
  reproducible without depending on the API being up.

## Known limitations / in progress

- Curve is single-factor pré-fixado only; no OIS/CDI basis, no NTN-B pricing
  engine yet (the IPCA index builder is ready, the instrument + engine are
  the next piece).
- `FlatForwardInterpolator` pillar interpolation for interior points is still
  being finished — don't trust intra-pillar DFs yet, pillar-exact dates are
  correct.
- Holiday calendar is hardcoded (fixed nationals + Easter-derived movables +
  a couple of hardcoded São Paulo city holidays) rather than sourced from
  ANBIMA/B3 — good enough for demoing the pricing mechanics, not for a real
  book.
- No automated tests yet; correctness is currently checked by round-tripping
  an LTN's market price through the bootstrapped curve back to its own PV
  (see `Program.cs`).

## Running it

Requires the .NET 10 SDK.

```
dotnet run
```

This bootstraps a curve from two LTN quotes, prices an LTN and a CDB, computes
DV01/duration/convexity for both, and aggregates them into a portfolio. Sample
output:

```
Zero rate at 2028-01-01: 13.7383%
Interpolated DF at 2027-06-01: 0.920500
Round-trip check: 920.50 (input was 920.50)
LTN PV: 920.50
CDB value today: 100040.53
Portfolio PORT-001 as of 2026-09-10
Total PV: 560290.53
Total DV01: 10.6840
Weighted Avg Duration (bonds only): 0.2330
```
