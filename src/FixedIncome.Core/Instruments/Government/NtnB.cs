using fixed_income_pricing.Dates.Interface;

namespace fixed_income_pricing.Instruments.Government;

public class NtnB
{
    public const decimal SemiAnnualCupon = 0.02956301m;
    public DateOnly Maturity { get; }
    public NtnB(DateOnly maturity) => Maturity = maturity;

    public decimal Cotacao(DateOnly settlement, decimal realRate, IBusinessDayCalendar calendar)
    {
        var dates = CouponSchedule.SemiAnnual(settlement, Maturity,  calendar);
        var onePlusR = 1.0 + (double)realRate;
        var sum = 0m;

        for (var i = 0; i < dates.Count; i++)
        {
            var flow = SemiAnnualCupon + (i == dates.Count - 1 ? 1m : 0m);
            var du = calendar.CountBusinessDaysBetween(settlement, dates[i]);
        }
    }
    
}