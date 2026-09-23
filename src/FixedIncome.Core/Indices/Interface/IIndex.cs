namespace fixed_income_pricing.Indices;

public interface IIndex
{
        string Name { get; }
        double AccrualFactor ( DateOnly startDate,  DateOnly endDate );
}