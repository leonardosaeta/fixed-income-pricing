using System.Xml.Serialization;
using fixed_income_pricing.Dates;
using fixed_income_pricing.Indices;

// Has nos being created yet.
var cdiRates = LoadCdiRatesFromCsv("cdi.csv"); 
var ipcaNumbers = LoadIpcaNumbersFromCsv("ipca.csv");

var calendar = new B3Calendar(2020, 2030);
var cdi = new CDIIndex(cdiRates, calendar);

double cdiGrowth = cdi.AccrualFactor(new DateTime(2026, 1, 2), new DateTime(2026, 7, 1));
Console.WriteLine($"CDI growth over H1 2026: {cdiGrowth:F6}");

var ipca = new IPCAIndex(ipcaNumbers);

double ipcaGrowth = ipca.AccrualFactor(new DateTime(2026, 1, 1), new DateTime(2026, 8, 20));
Console.WriteLine($"IPCA growth: {ipcaGrowth:F6}");