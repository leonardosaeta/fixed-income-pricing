using System;
using System.Collections.Generic;
using fixed_income_pricing.CashFlows;
using fixed_income_pricing.Dates.Interface;
using fixed_income_pricing.Pricing;

namespace fixed_income_pricing.Instruments.Government;

public class NtnB:IInstrument
{
    public const decimal SemiAnnualCupon = 0.02956301m;

    public string Id {get;}
    public DateOnly IssueDate {get;}
    public DateOnly MaturityDate {get;}

    public NtnB(string id, DateOnly issueDate, DateOnly maturityDate)
    {
        if (maturityDate <= issueDate)
            throw new ArgumentException("maturityDate must be greater than issueDate");

        Id = id;
        IssueDate = issueDate;
        MaturityDate = maturityDate;
    }

    public IEnumerable<Cashflow> GenerateCashflows(DateOnly valuationDate)
    {
        if (valuationDate >= MaturityDate)
            yield break;

        var dates = new List<DateOnly>();
        for (var d = MaturityDate; d > valuationDate; d = d.AddMonths(-6))
            dates.Add(d);
        dates.Reverse();

        foreach (var date in dates)
        {
            var amount = SemiAnnualCupon + (date == MaturityDate ? 1m : 0m);
            yield return new Cashflow(date, (double)amount);
        }
    }

    public decimal Cotacao(DateOnly settlement, decimal realRate, IBusinessDayCalendar calendar)
    {
        var dates = CouponSchedule.SemiAnnual(settlement, MaturityDate, calendar);
        var onePlusR = 1.0 + (double)realRate;
        var sum = 0m;

        for (var i = 0; i < dates.Count; i++)
        {
            var flow = SemiAnnualCupon + (i == dates.Count - 1 ? 1m : 0m);
            var du = calendar.CountBusinessDaysBetween(settlement, dates[i]);
            sum+=flow / (decimal)Math.Pow(onePlusR, du/252.0);
        }
        return sum;
    }
    public decimal Price(DateOnly settlement, decimal realRate, decimal projectedVna, IBusinessDayCalendar calendar) =>
        Rounding.Truncate(projectedVna*Cotacao(settlement, realRate, calendar), 6);

}