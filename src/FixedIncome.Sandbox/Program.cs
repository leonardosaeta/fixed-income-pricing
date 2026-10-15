using System.Globalization;
using fixed_income_pricing.Curvers;
using fixed_income_pricing.dates;
using fixed_income_pricing.Dates;
using fixed_income_pricing.Indices;
using fixed_income_pricing.Instruments;
using fixed_income_pricing.Instruments.Bank;
using fixed_income_pricing.Instruments.Futures;
using fixed_income_pricing.Instruments.Government;
using fixed_income_pricing.Instruments.Swaps;
using fixed_income_pricing.Market;
using fixed_income_pricing.MarketData;
using fixed_income_pricing.Pricing;
using fixed_income_pricing.Risk;
using FixedIncome.MarketData.Anbima;
using FixedIncome.MarketData.B3;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

var calendar = new B3Calendar(2020, 2070);
var dayCount = new Bus252();
var bootstrapper = new IterativeCurveBootstrapper(calendar, dayCount, new FlatForwardInterpolator());
var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
Directory.CreateDirectory(dataDir);

Console.WriteLine("\n[1] Market data");

var today = args.Length > 0 ? DateOnly.ParseExact(args[0], "yyyy-MM-dd") : LatestAnbimaDate();
var yesterday = calendar.AddBusinessDay(today, -1);

var cdiPath = Path.Combine(dataDir, "cdi.csv");
if (!File.Exists(cdiPath))
    CsvRateDataSource.WriteCsv(new BcbApiSource().GetDailyRates(4389, today.AddYears(-2), today), cdiPath);
var cdi = new CDIIndex(new CsvRateDataSource(cdiPath).GetDailyRates(4389, DateOnly.MinValue, today), calendar);

Console.WriteLine($"  valuation date {today:yyyy-MM-dd}, previous {yesterday:yyyy-MM-dd}");

Console.WriteLine("\n[2] Curves");

var market = BuildMarket(today);
var previousMarket = Download(yesterday) ? BuildMarket(yesterday) : null;

bool hasDi = market.CurveNames.Contains(CurveNames.Di);
if (!hasDi) Console.WriteLine($"  no B3 DI1 settlements for {today:yyyy-MM-dd}, DI curve skipped");

Console.WriteLine($"  {"Tenor",-6} {"Pré",9} {"Real",9} {"Breakeven",10} {"DI",9} {"Pré-DI",8}");
foreach (var years in new[] { 0.5, 1, 2, 3, 5, 10, 20 })
{
    var date = calendar.AddBusinessDay(today, (int)(years * 252));
    double pre = market.Curve(CurveNames.Pre).ZeroRate(date);
    double real = market.Curve(CurveNames.IpcaReal).ZeroRate(date);
    var di = hasDi ? $"{market.Curve(CurveNames.Di).ZeroRate(date),9:P3} {(pre - market.Curve(CurveNames.Di).ZeroRate(date)) * 1e4,6:N1}bp" : "";
    Console.WriteLine($"  {years + "y",-6} {pre,9:P3} {real,9:P3} {(1 + pre) / (1 + real) - 1,10:P3} {di}");
}

Console.WriteLine("\n[3] Valuation & risk");

var quotes = AnbimaQuotes(today);
DateOnly Nearest(string type, double years) => quotes
    .Where(q => q.Instrument == type)
    .MinBy(q => Math.Abs((q.Maturity.DayNumber - today.DayNumber) / 365.25 - years))!.Maturity;

var book = new List<(IInstrument Instrument, double Quantity)>
{
    (new Ltn("LTN 1y", today.AddYears(-5), Nearest("LTN", 1)), 5_000),
    (new Ltn("LTN 5y", today.AddYears(-5), Nearest("LTN", 5)), 2_000),
    (new NtnF("NTN-F 5y", today.AddYears(-10), Nearest("NTN-F", 5)), 1_500),
    (new NtnB("NTN-B 10y", today.AddYears(-1), Nearest("NTN-B", 10)), 400),
    (new NtnB("NTN-B 30y", today.AddYears(-1), Nearest("NTN-B", 30)), -150),
    (new PostFixedCDB("CDB 105% CDI", today.AddYears(-1), today.AddYears(2), 5_000_000, 1.05), 1),
};

var preEngine = new CashflowPricingEngine();
var ntnbEngine = new NtnBPricingEngine();
var cdbEngine = new CdbPricingEngine();
var swapEngine = new DiPreSwapPricingEngine();

if (hasDi)
{
    DiPreSwap Swap(string id, int startBusinessDays, int tenorBusinessDays, SwapSide side)
    {
        var start = calendar.AddBusinessDay(today, startBusinessDays);
        var maturity = calendar.AddBusinessDay(today, tenorBusinessDays);
        var fixedRate = Math.Round(market.Curve(CurveNames.Di).ZeroRate(maturity), 3);
        return new DiPreSwap(id, start, maturity, side, 10_000_000, fixedRate);
    }

    book.Add((Swap("DI×Pré 2y pay", -63, 504, SwapSide.PayFixed), 1));
    book.Add((Swap("DI×Pré 5y rec", 0, 1260, SwapSide.ReceiveFixed), 1));
}

double Price(IInstrument instrument, MarketContext m) => instrument switch
{
    NtnB b => ntnbEngine.Price(b, m).presentValue,
    ICashflowInstrument b => preEngine.Price(b, m).presentValue,
    PostFixedCDB c => cdbEngine.Price(c, m).presentValue,
    DiPreSwap s => swapEngine.Price(s, m).presentValue,
    _ => throw new NotSupportedException(instrument.Id),
};

// A swap's PV is near zero, so a duration or convexity ratio against it would be meaningless.
(RiskMetric Risk, KeyRateRisk? KeyRates) Risk(IInstrument instrument) => instrument switch
{
    NtnB b => (new CurveRiskEngine<NtnB>(ntnbEngine).Compute(b, market), new CurveRiskEngine<NtnB>(ntnbEngine).KeyRates(b, market)),
    ICashflowInstrument b => (new CurveRiskEngine<ICashflowInstrument>(preEngine).Compute(b, market),
        new CurveRiskEngine<ICashflowInstrument>(preEngine).KeyRates(b, market)),
    PostFixedCDB c => (new CdbRiskEngine(cdbEngine).Compute(c, market), null),
    DiPreSwap s => (new CurveRiskEngine<DiPreSwap>(swapEngine).Compute(s, market) with { ModifiedDuration = null, Convexity = null },
        new CurveRiskEngine<DiPreSwap>(swapEngine).KeyRates(s, market)),
    _ => throw new NotSupportedException(instrument.Id),
};

bool Priceable(IInstrument instrument, MarketContext m) =>
    instrument is not DiPreSwap || m.CurveNames.Contains(CurveNames.Di);

var results = book.Select(p => (p.Instrument, p.Quantity, Pv: Price(p.Instrument, market), Risk(p.Instrument))).ToList();

Console.WriteLine($"  {"Instrument",-14} {"Maturity",-10} {"Qty",7} {"Unit PV",14} {"Market value",15} {"DV01",10} {"ModDur",7}");
foreach (var r in results)
    Console.WriteLine($"  {r.Instrument.Id,-14} {r.Instrument.MaturityDate:yyyy-MM-dd} {r.Quantity,7:N0} {r.Pv,14:N2} " +
                      $"{r.Quantity * r.Pv,15:N2} {r.Quantity * r.Item4.Risk.Dv01,10:N2} {r.Item4.Risk.ModifiedDuration,7:N2}");
Console.WriteLine($"  {"TOTAL",-14} {"",-10} {"",7} {"",14} {results.Sum(r => r.Quantity * r.Pv),15:N2} {results.Sum(r => r.Quantity * r.Item4.Risk.Dv01),10:N2}");

foreach (var swap in book.Select(p => p.Instrument).OfType<DiPreSwap>())
    Console.WriteLine($"  {swap.Id}: fixed {swap.FixedRate:P3}, par {swapEngine.ParRate(swap, market):P3}, " +
                      $"{swap.Side}, notional {swap.Notional:N0}");

Console.WriteLine("\n[4] Key-rate DV01");

var keyRates = KeyRateReport.Aggregate(results.Where(r => r.Item4.KeyRates is not null).Select(r => (r.Quantity, r.Item4.KeyRates!)));
foreach (var curve in keyRates.Keys.Select(k => k.Curve).Distinct())
{
    Console.WriteLine($"  {curve}");
    foreach (var tenor in KeyRateTenors.Default)
        Console.WriteLine($"    {tenor + "y",-6} {keyRates.GetValueOrDefault((curve, tenor)),12:N2}");
}

Console.WriteLine("\n[5] Scenarios");

var scenarios = new List<(string Name, string Curve, double Shift)>
{
    ("Pré +100bp", CurveNames.Pre, 0.01),
    ("Pré -100bp", CurveNames.Pre, -0.01),
    ("Real +50bp", CurveNames.IpcaReal, 0.005),
    ("Real -50bp", CurveNames.IpcaReal, -0.005),
};
if (hasDi)
{
    scenarios.Add(("DI +100bp", CurveNames.Di, 0.01));
    scenarios.Add(("DI -100bp", CurveNames.Di, -0.01));
}
foreach (var s in scenarios)
{
    var shocked = market.WithCurve(s.Curve, new ParallelShiftedCurve(market.Curve(s.Curve), s.Shift, dayCount, calendar));
    double pnl = results.Sum(r => r.Quantity * (Price(r.Instrument, shocked) - r.Pv));
    Console.WriteLine($"  {s.Name,-12} P&L {pnl,14:N2}");
}

Console.WriteLine("\n[6] Daily P&L");

if (previousMarket is null)
    Console.WriteLine($"  no ANBIMA data for {yesterday:yyyy-MM-dd}, skipped");
else
{
    var priceable = results.Where(r => Priceable(r.Instrument, previousMarket)).ToList();
    foreach (var r in results)
        Console.WriteLine(Priceable(r.Instrument, previousMarket)
            ? $"  {r.Instrument.Id,-14} {r.Quantity * (r.Pv - Price(r.Instrument, previousMarket)),14:N2}"
            : $"  {r.Instrument.Id,-14} {"n/a (no DI curve)",14}");
    Console.WriteLine($"  {"TOTAL",-14} {priceable.Sum(r => r.Quantity * (r.Pv - Price(r.Instrument, previousMarket))),14:N2}");
    Console.WriteLine("  (coupons paid overnight are not added back)");
}

return;

MarketContext BuildMarket(DateOnly date)
{
    var q = AnbimaQuotes(date);
    var ltns = q.Where(x => x.Instrument == "LTN").ToList();
    var ltnDates = ltns.Select(x => x.Maturity).ToHashSet();

    var preQuotes = ltns
        .Select(x => new BootstrapQuote(new Ltn("LTN", date.AddYears(-5), x.Maturity), (double)x.Pu))
        .Concat(q.Where(x => x.Instrument == "NTN-F" && !ltnDates.Contains(x.Maturity))
            .Select(x => new BootstrapQuote(new NtnF("NTN-F", date.AddYears(-10), x.Maturity), (double)x.Pu)));
    var realQuotes = q.Where(x => x.Instrument == "NTN-B")
        .Select(x => new RateQuote(new NtnB("NTN-B", date.AddYears(-1), x.Maturity), (double)x.IndicativeRate));

    var vna = AnbimaVnaFile.Parse(Path.Combine(dataDir, AnbimaClient.VnaFileName(date))).Single(v => v.Instrument == "NTN-B").Vna;

    var m = new MarketContext(date, calendar, dayCount)
        .WithCurve(CurveNames.Pre, bootstrapper.Bootstrap(date, preQuotes))
        .WithCurve(CurveNames.IpcaReal, bootstrapper.Bootstrap(date, realQuotes))
        .WithIndex(IndexNames.Cdi, cdi)
        .WithQuote(QuoteNames.NtnBVna, vna);

    var di1 = Di1Settlements(date)
        .Where(x => x.SettlementRate is not null)
        .Select(x => new RateQuote(Di1Future.FromTicker(x.Ticker, calendar), (double)x.SettlementRate!.Value))
        .Where(x => x.Instrument.MaturityDate > date)
        .ToList();
    return di1.Count > 0 ? m.WithCurve(CurveNames.Di, bootstrapper.Bootstrap(date, di1)) : m;
}

IReadOnlyList<Di1Settlement> Di1Settlements(DateOnly date)
{
    var path = Path.Combine(dataDir, B3Client.Di1SettlementFileName(date));
    return File.Exists(path) || B3Client.TryDownloadDi1SettlementsAsync(date, path).GetAwaiter().GetResult()
        ? B3Di1SettlementFile.Parse(path)
        : [];
}

List<AnbimaQuote> AnbimaQuotes(DateOnly date) =>
    AnbimaSecondaryMarketFile.Parse(Path.Combine(dataDir, AnbimaClient.SecondaryMarketFileName(date)))
        .Where(q => q.Maturity > date)
        .ToList();

bool Download(DateOnly date)
{
    var ms = Path.Combine(dataDir, AnbimaClient.SecondaryMarketFileName(date));
    var vna = Path.Combine(dataDir, AnbimaClient.VnaFileName(date));
    return (File.Exists(ms) || AnbimaClient.TryDownloadSecondaryMarketAsync(date, ms).GetAwaiter().GetResult())
           && (File.Exists(vna) || AnbimaClient.TryDownloadVnaAsync(date, vna).GetAwaiter().GetResult());
}

DateOnly LatestAnbimaDate()
{
    var date = calendar.AddBusinessDay(DateOnly.FromDateTime(DateTime.Today), -1);
    for (var i = 0; i < 10; i++, date = calendar.AddBusinessDay(date, -1))
        if (Download(date)) return date;
    throw new InvalidOperationException("No ANBIMA data found in the last 10 business days");
}
