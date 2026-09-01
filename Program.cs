using System.Xml.Serialization;
using fixed_income_pricing.Dates;
using fixed_income_pricing.Indices;
using fixed_income_pricing.Instruments.Bank;

var calendar = new B3Calendar(2020, 2030);

//Needs to be implemented
var cdiRates = LoadCdiRatesFromCsv("cdi.csv");
var cdi = new CDIIndex(cdiRates, calendar);

var cdb = new PostFixedCDB(
    id: "CDB-001",
    issueDate: new DateTime(2026, 1, 2),
    maturityDate: new DateTime(2027, 1, 4),
    notional: 100_000,
    percentualCdi: 0.98,
    cdiIndex: cdi);

double redemption = cdb.RedemptionValue();
Console.WriteLine($"Redemption value: {redemption:F2}");