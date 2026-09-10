using System.Xml.Serialization;
using fixed_income_pricing.Dates;
using fixed_income_pricing.MarketData;
using fixed_income_pricing.Indices;
using fixed_income_pricing.Instruments.Bank;
using fixed_income_pricing.MarketData.Interface;

var calendar = new B3Calendar(2020, 2030);
IRateDataSource test = new BcbApiSource();
var cdoRates = test.GetDailyRates(12, new DateTime(2025, 1,2), new  DateTime(2025, 12, 31));
CsvRateDataSource.WriteCsv(cdoRates, "cdi_rates.csv");