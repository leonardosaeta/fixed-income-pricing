using System.Text;
using FixedIncome.MarketData.B3;
using FluentAssertions;

namespace FixedIncome.Core.Tests.MarketData;

public class B3PriceReportFileTests
{
    private const string Report = """
                                  <?xml version="1.0" encoding="utf-8"?>
                                  <Document xmlns="urn:bvmf.052.01.xsd">
                                    <PricRpt>
                                      <SctyId><TckrSymb>DI1F27</TckrSymb></SctyId>
                                      <FinInstrmAttrbts>
                                        <LastPric Ccy="BRL">13.467</LastPric>
                                        <AdjstdQt Ccy="BRL">97281.71</AdjstdQt>
                                        <AdjstdQtTax Ccy="BRL">13.459</AdjstdQtTax>
                                        <AdjstdQtStin>F</AdjstdQtStin>
                                      </FinInstrmAttrbts>
                                    </PricRpt>
                                    <PricRpt>
                                      <SctyId><TckrSymb>DOLX26</TckrSymb></SctyId>
                                      <FinInstrmAttrbts><AdjstdQt Ccy="BRL">5012.3</AdjstdQt></FinInstrmAttrbts>
                                    </PricRpt>
                                    <PricRpt>
                                      <SctyId><TckrSymb>DI1F33</TckrSymb></SctyId>
                                      <FinInstrmAttrbts><AdjstdQt Ccy="BRL">47110.05</AdjstdQt></FinInstrmAttrbts>
                                    </PricRpt>
                                  </Document>
                                  """;

    [Fact]
    public void ParseDi1_ReadsSettlementPuAndRate_AndSkipsOtherContracts()
    {
        var settlements = B3PriceReportFile.ParseDi1(new MemoryStream(Encoding.UTF8.GetBytes(Report)));

        settlements.Should().Equal(
            new Di1Settlement("DI1F27", 97_281.71m, 0.13459m),
            new Di1Settlement("DI1F33", 47_110.05m));
    }

    [Fact]
    public void Write_ThenParse_RoundTrips()
    {
        var settlements = new[] { new Di1Settlement("DI1F27", 97_281.71m, 0.13459m), new Di1Settlement("DI1F33", 47_110.05m) };
        var path = Path.GetTempFileName();

        try
        {
            B3Di1SettlementFile.Write(settlements, path);
            B3Di1SettlementFile.Parse(path).Should().Equal(settlements);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
