namespace fixed_income_pricing.Curvers.Interface;

public interface IYieldCurve
{
   DateTime ReferenceDate { get; }
   double DiscountFactor(DateTime date);
   double ZeroRate(DateTime date);
}