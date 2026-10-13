using fixed_income_pricing.CashFlows;
using fixed_income_pricing.Dates.Interface;
using fixed_income_pricing.Indices;

namespace fixed_income_pricing.Instruments.Futures;

// B3 one-day interbank deposit future (DI1): 100,000 points at maturity, the first business day of the contract
// month. Traded as a Bus/252 rate; the PU is that rate's discount of the 100,000 points. R$ 1.00 per point.
public class Di1Future : ICashflowInstrument
{
    public const decimal Notional = 100_000m;
    private const string MonthCodes = "FGHJKMNQUVXZ";

    public string Id { get; }
    public DateOnly MaturityDate { get; }

    public Di1Future(string id, DateOnly maturityDate)
    {
        Id = id;
        MaturityDate = maturityDate;
    }

    // "DI1F27" or "F27": January 2027, maturing on its first business day.
    public static Di1Future FromTicker(string ticker, IBusinessDayCalendar calendar)
    {
        var code = ticker.Trim().ToUpperInvariant();
        if (code.StartsWith("DI1")) code = code[3..];

        int month = code.Length == 3 ? MonthCodes.IndexOf(code[0]) + 1 : 0;
        if (month == 0 || !int.TryParse(code[1..], out int year))
            throw new ArgumentException($"Not a DI1 ticker: '{ticker}'");

        var first = new DateOnly(2000 + year, month, 1);
        return new Di1Future($"DI1{code}", calendar.IsBusinessDays(first) ? first : calendar.AddBusinessDay(first, 1));
    }

    public IEnumerable<Cashflow> GenerateCashflows(DateOnly valuationDate)
    {
        if (valuationDate >= MaturityDate)
            yield break;

        yield return new Cashflow(MaturityDate, (double)Notional);
    }

    // B3: PU = 100,000 / (1 + rate)^(DU/252), rounded to 2 decimals.
    public decimal Pu(DateOnly date, decimal rate, IBusinessDayCalendar calendar)
    {
        int du = calendar.CountBusinessDaysBetween(date, MaturityDate);
        return Math.Round(Notional / (decimal)Math.Pow(1 + (double)rate, du / 252.0), 2, MidpointRounding.AwayFromZero);
    }

    public double ImpliedRate(DateOnly date, decimal pu, IBusinessDayCalendar calendar)
    {
        int du = calendar.CountBusinessDaysBetween(date, MaturityDate);
        return Math.Pow((double)(Notional / pu), 252.0 / du) - 1.0;
    }

    // Daily settlement per contract, long in PU (i.e. short the rate): today's settlement PU against yesterday's
    // carried one overnight at the previous day's CDI, AD = PA_t - PA_{t-1} · (1 + TDI_{t-1}).
    public static decimal DailySettlement(decimal previousSettlementPu, decimal settlementPu, double previousCdi) =>
        Math.Round(settlementPu - previousSettlementPu * DailyRateAccrual.B3DailyFactor(previousCdi), 2,
            MidpointRounding.AwayFromZero);
}
