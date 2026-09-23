namespace fixed_income_pricing.Portfolio;

public record PortfolioRiskReport (string PortfolioId, DateOnly ValuationDate, double TotalPresentValue, double TotalDv01, double? WeightedAverageModifiedDuration, IReadOnlyList<Position> Positions);
