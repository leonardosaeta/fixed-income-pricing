using FixedIncome.MarketData.Anbima;

const string csv = """
                    titulo,datareferencia,datavencimento,txIndicativas,pu
                    LTN,01/09/2026,01/01/2027,10,5000

                    """;

using var reader = new StringReader(csv);
var quotes = AnbimaSecondaryMarketFile.Parse(reader);

if (quotes.Count != 1)
    throw new Exception($"Expected 1 quote, got {quotes.Count}");

var q = quotes[0];
if (q.IndicativeRate != 0.10m)
    throw new Exception($"Expected rate 0.10, got {q.IndicativeRate}");

Console.WriteLine("AnbimaSecondaryMarketFile test passed.");
