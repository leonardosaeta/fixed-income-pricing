using fixed_income_pricing.Curvers.Interface;
using fixed_income_pricing.Dates.Interface;

namespace fixed_income_pricing.Curvers;

public class DiscountCurve:IYieldCurve
{
   public DateOnly ReferenceDate { get; }

   private readonly IBusinessDayCalendar _calendar;
   private readonly IDayCountConvention _dayCountConvention;
   private readonly IInterpolator _interpolator;

   private readonly double[] _pillarT;
   private readonly double[] _pillarLnDf;

   public DiscountCurve(
       DateOnly referenceDate,
       IBusinessDayCalendar calendar,
       IDayCountConvention daycountConvention,
       IEnumerable<(DateOnly date, double zeroRate)> pillars,
       IInterpolator interpolator)
   {
       ReferenceDate =  referenceDate;
       _calendar = calendar;
       _dayCountConvention = daycountConvention;
       _interpolator = interpolator;
       
       var sorted = pillars.OrderBy(p => p.date).ToList();
       if (sorted.Count() == 0)
           throw new ArgumentException("pillars must contain at least one item");
       
       _pillarT = new double[sorted.Count()];
       _pillarLnDf = new double[sorted.Count()];
       

       for (int i = 0; i < sorted.Count(); i++)
       {
           double t = _dayCountConvention.YearFraction(referenceDate, sorted[i].date, calendar);
           _pillarT[i] = t;
           _pillarLnDf[i] = -t * Math.Log(1+sorted[i].zeroRate);
       }
   }


   public double DiscountFactor(DateOnly date)
   {
       double t = _dayCountConvention.YearFraction(ReferenceDate, date, _calendar);
       if(t<= 0)
           return 0;
       double lnDf = _interpolator.Interpolate(_pillarT, _pillarLnDf, t);
       return Math.Exp(lnDf);
   }


   public double ZeroRate(DateOnly date)
   {
       double t = _dayCountConvention.YearFraction(ReferenceDate, date, _calendar);
       if (t <= 0)
           throw new ArgumentException("date must be after the curve's reference date");

       double df = DiscountFactor(date);
       return Math.Pow(df, -1.0 / t) - 1.0;
   }
}