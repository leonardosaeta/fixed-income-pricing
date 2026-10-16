namespace fixed_income_pricing.Pricing;

public record PricingResult(string InstrumentId, DateOnly ValuationDate, double PresentValue);
