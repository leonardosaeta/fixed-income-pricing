using fixed_income_pricing.Instruments;
using fixed_income_pricing.Market;

namespace fixed_income_pricing.Pricing.Interface;

public interface IPricingEngine<in TIntrument> where TIntrument :IInstrument
{
    PricingResult Price(TIntrument instrument, MarketContext market);
}

public interface ICurvePricingEngine<in TInstrument> : IPricingEngine<TInstrument> where TInstrument : IInstrument
{
    string CurveName { get; }
}
