namespace fixed_income_pricing.Risk;

public record RiskMetric(string InstrumentId, double? ModifiedDuration, double? Convexity, double Dv01);