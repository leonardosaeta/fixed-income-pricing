namespace fixed_income_pricing.Curvers.Interface;

public interface IYieldCurve
{
   DateOnly ReferenceDate { get; }
   double DiscountFactor(DateOnly date);
   double ZeroRate(DateOnly date);
}
