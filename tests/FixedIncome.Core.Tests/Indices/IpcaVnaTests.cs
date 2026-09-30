using fixed_income_pricing.Dates;
using fixed_income_pricing.Indices;
using FluentAssertions;

namespace FixedIncome.Core.Tests.Indices;

public class IpcaVnaTests
{
    private static readonly B3Calendar Calendar = new(2020, 2070);
    private static readonly DateOnly Last15th = new(2026, 9, 15);
    private const decimal ProjectedIpca = 0.0056m;

    private const decimal VnaAt15th = 4727.5705729763m;

    [Theory]
    [InlineData(2026, 9, 23, "4735.119605")]
    [InlineData(2026, 9, 25, "4737.638628")]
    public void Project_ReproducesAnbimaPublishedVna(int y, int m, int d, string published)
    {
        var vna = IpcaVna.Project(VnaAt15th, ProjectedIpca, Last15th, new DateOnly(y, m, d), Calendar);

        vna.Should().BeApproximately(decimal.Parse(published, System.Globalization.CultureInfo.InvariantCulture), 0.000002m);
    }

    [Fact]
    public void Project_ReachesTheFullMonthlyFactorOnTheNext15th()
    {
        var vna = IpcaVna.Project(VnaAt15th, ProjectedIpca, Last15th, Last15th.AddMonths(1), Calendar);

        vna.Should().BeApproximately(VnaAt15th * (1 + ProjectedIpca), 0.000002m);
    }
}
