namespace fixed_income_pricing.Pricing;

public record PricingResult(string InstrumnetId, DateOnly ValuationDate, double presentValue);
