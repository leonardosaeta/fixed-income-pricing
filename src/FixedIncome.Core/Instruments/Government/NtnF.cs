using fixed_income_pricing.CashFlows;
using fixed_income_pricing.Dates.Interface;
using fixed_income_pricing.Pricing;

namespace fixed_income_pricing.Instruments.Government;

// Nota do Tesouro Nacional série F: nominal bond, 10% p.a. paid semiannually on R$ 1,000 (1 Jan / 1 Jul).
public class NtnF : ICashflowInstrument
{
    public const decimal FaceValue = 1000m;
    public const decimal SemiAnnualCoupon = 48.80885m; // 1000 · (1.10^(1/2) - 1), truncated at 5 decimals

    public string Id { get; }
    public DateOnly IssueDate { get; }
    public DateOnly MaturityDate { get; }

    public NtnF(string id, DateOnly issueDate, DateOnly maturityDate)
    {
        if (maturityDate <= issueDate)
            throw new ArgumentException("maturityDate must be greater than issueDate");

        Id = id;
        IssueDate = issueDate;
        MaturityDate = maturityDate;
    }

    public IEnumerable<Cashflow> GenerateCashflows(DateOnly valuationDate)
    {
        var dates = new List<DateOnly>();
        for (var d = MaturityDate; d > valuationDate; d = d.AddMonths(-6))
            dates.Add(d);
        dates.Reverse();

        foreach (var date in dates)
            yield return new Cashflow(date, (double)(SemiAnnualCoupon + (date == MaturityDate ? FaceValue : 0m)));
    }

    // ANBIMA: PU = Σ C_i / (1+y)^(DU_i/252), truncated at 6 decimals.
    public decimal Price(DateOnly settlement, decimal rate, IBusinessDayCalendar calendar)
    {
        var onePlusY = 1.0 + (double)rate;
        var sum = 0m;
        foreach (var cf in GenerateCashflows(settlement))
        {
            var du = calendar.CountBusinessDaysBetween(settlement, cf.PaymentDate);
            sum += Math.Round((decimal)(cf.Amount / Math.Pow(onePlusY, du / 252.0)), 9);
        }
        return Rounding.Truncate(sum, 6);
    }
}
