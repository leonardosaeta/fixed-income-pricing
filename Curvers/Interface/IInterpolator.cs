using System.Collections.Generic;

namespace fixed_income_pricing.Curvers.Interface;

public interface IInterpolator
{
    double Interpolate(IReadOnlyList<double>x, IReadOnlyList<double>y, double targetX);
}