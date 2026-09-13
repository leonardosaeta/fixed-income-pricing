namespace fixed_income_pricing.Portfolio;

public record Position(
    string InstrumentId,
    double Quantity,
    double UnitPresentValue,
    double? UnitModifiedDuration,
    double? UnitConvexity,
    double UnitDv01)
{
    public double PresentValue => Quantity * UnitPresentValue;
    public double Dv01 => Quantity * UnitDv01;
}