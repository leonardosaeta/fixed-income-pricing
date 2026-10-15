using fixed_income_pricing.Dates.Interface;

namespace fixed_income_pricing.Instruments.Swaps;

public class DiPreSwap:IInstrument
{
    public string Id {get; }
    public DateOnly StartDate {get; }
    public DateOnly MaturityDate { get; }
    public double Notional {get;}
    public double FixedRate {get;}
    public SwapSide Side {get;}

    public DiPreSwap(string id, DateOnly startDate, DateOnly maturityDate, SwapSide side, double notional,
        double fixedRate)
    {
        if (maturityDate <= startDate) throw new ArgumentException("maturityDate must be greater than or equal to startDate");
        if (notional <= 0) throw new ArgumentException("notional must be greater than zero");

        Id = id;
        StartDate = startDate;
        MaturityDate = maturityDate;
        Notional = notional;
        FixedRate = fixedRate;
        Side = side;
    }

    public int FloatingSign => Side == SwapSide.PayFixed ? 1 : -1;
    
    public double FixedLegAmount(IBusinessDayCalendar calendar) => FixedLegAmount(FixedRate, calendar);

    public double FixedLegAmount(double fixedRate, IBusinessDayCalendar calendar)
    {
        int du = calendar.CountBusinessDaysBetween(StartDate, MaturityDate);
        return Notional * Math.Pow(1 + fixedRate, du / 252.0);
    }
}