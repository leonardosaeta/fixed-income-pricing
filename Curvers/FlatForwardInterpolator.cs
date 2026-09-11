using fixed_income_pricing.Curvers.Interface;

namespace fixed_income_pricing.Curvers;

public class FlatForwardInterpolator:IInterpolator
{
   public double Interpolate(IReadOnlyList<double> x, IReadOnlyList<double> y, double targetX)
   {
      if (x.Count != y.Count || x.Count == 0)
         throw new ArgumentException("x and y must be non-empty and the same length");

      if (x.Count == 1 || targetX <= x[0])
         return y[0];

      if (targetX >= x[x.Count - 1])
         return y[x.Count - 1];

      int upperIndex = 0;
      while (upperIndex < x.Count && x[upperIndex] < targetX)
         upperIndex++;

      int lowerIndex = upperIndex - 1;
      double x0 = x[lowerIndex], x1 = x[upperIndex];
      double y0 = y[lowerIndex], y1 = y[lowerIndex];

      double weight = (targetX - x0) / (x1 - x0);
      return y0 + weight * (y1 - y0);
   }
}