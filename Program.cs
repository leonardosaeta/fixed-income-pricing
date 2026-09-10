using System.Xml.Serialization;
using fixed_income_pricing.Dates;
using fixed_income_pricing.MarketData;
using fixed_income_pricing.Indices;
using fixed_income_pricing.Instruments.Bank;
using fixed_income_pricing.MarketData.Interface;

var calendar = new B3Calendar(2015, 2035);
var source = new BcbApiSource();

var cdiRates = source.GetDailyRates(12, new DateTime(2025, 1, 1), new DateTime(2027, 1, 4));
CsvRateDataSource.WriteCsv(cdiRates, "cdi_cache.csv");
var cdi = new CDIIndex(cdiRates, calendar);

var selicRates = source.GetDailyRates(11, new DateTime(2025, 1, 1), new DateTime(2027, 1, 4));
var selic = new SelicIndex(selicRates, calendar);

var ipcaVariations = source.GetDailyRates(433, new DateTime(2020, 1, 1), new DateTime(2027, 1, 4));
var ipcaIndexNumbers = IpcaIndexNumberBuilder.BuildIndexNumber(ipcaVariations);
var ipca = new IPCAIndex(ipcaIndexNumbers);

var cdb = new PostFixedCDB("CDB-001", new DateTime(2026, 1, 2), new DateTime(2027, 1, 4), 100_000, 0.98, cdi);
Console.WriteLine(cdb.RedemptionValue());