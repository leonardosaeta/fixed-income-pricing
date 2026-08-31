using System;
namespace fixed_income_pricing.Indices;

public interface IIndex
{
        string Name { get; }
        double AccrualFactor ( DateTime startDate,  DateTime endDate );
}