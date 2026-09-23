using System;
using fixed_income_pricing.Curvers;
using fixed_income_pricing.Curvers.Interface;
using fixed_income_pricing.Instruments;

namespace fixed_income_pricing.Pricing.Interface;

public interface IPricingEngine<in TIntrument> where TIntrument :IInstrument
{
    PricingResult Price(TIntrument instrument, IYieldCurve curve, DateOnly valuationDate);
}