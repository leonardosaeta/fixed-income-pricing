using System;
namespace fixed_income_pricing.Pricing;

public record PricingResult(string InstrumnetId, DateTime ValuationDate, double presentValue);
