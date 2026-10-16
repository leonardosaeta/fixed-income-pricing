namespace FixedIncome.Derivatives.Models;

public static class NormalDistribution
{
    private const double InverseSqrtTwoPi = 0.3989422804014327;
    public static double Pdf(double x) => InverseSqrtTwoPi * Math.Exp(-0.5 * x * x);

    public static double Cdf(double x)
    {
        double abs = Math.Abs(x);
        if (abs > 37.0) return x > 0 ? 1.0 : 0.0;

        double exponential = Math.Exp(-0.5 * abs * abs);
        double tail;

        if (abs < 7.07106781186547)
        {
            double numerator = 3.52624965998911e-02 * abs + 0.700383064443688;
            numerator = numerator * abs + 6.37396220353165;
            numerator = numerator * abs + 33.912866078383;
            numerator = numerator * abs + 112.079291497871;
            numerator = numerator * abs + 221.213596169931;
            numerator = numerator * abs + 220.206867912376;

            double denominator = 8.83883476483184e-02 * abs + 1.75566716318264;
            denominator = denominator * abs + 16.064177579207;
            denominator = denominator * abs + 86.7807322029461;
            denominator = denominator * abs + 296.564248779674;
            denominator = denominator * abs + 637.333633378831;
            denominator = denominator * abs + 793.826512519948;
            denominator = denominator * abs + 440.413735824752;

            tail = exponential * numerator / denominator;
        }
        else
        {
            double build = abs + 0.65;
            build = abs + 4.0 / build;
            build = abs + 3.0 / build;
            build = abs + 2.0 / build;
            build = abs + 1.0 / build;
            tail = exponential / build / 2.506628274631;
        }

        return x > 0 ? 1.0 - tail : tail;
    }
}