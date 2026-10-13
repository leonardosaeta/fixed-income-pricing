using FixedIncome.MarketData.B3;
using FluentAssertions;

namespace FixedIncome.Core.Tests.MarketData;

public class B3Di1SettlementFileTests
{
    [Fact]
    public void Parse_ReadsContractsAndSettlementPus()
    {
        const string file = """
                            Mercadoria;Vencimento;Preço de ajuste anterior;Preço de ajuste Atual
                            DI1;F27;97.010,53;97.020,11
                            DI1;N27;90.100,00;90.112,48

                            """;

        var settlements = B3Di1SettlementFile.Parse(new StringReader(file));

        settlements.Should().Equal(new Di1Settlement("DI1F27", 97_020.11m), new Di1Settlement("DI1N27", 90_112.48m));
    }

    [Fact]
    public void Parse_Throws_WithoutASettlementColumn()
    {
        var act = () => B3Di1SettlementFile.Parse(new StringReader("Ticker;Volume\nDI1F27;10\n"));

        act.Should().Throw<InvalidDataException>();
    }
}
