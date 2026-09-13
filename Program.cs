using System.Xml.Serialization;
using fixed_income_pricing.Curvers;
using fixed_income_pricing.Curvers.Interface;
using fixed_income_pricing.dates;
using fixed_income_pricing.Dates;
using fixed_income_pricing.MarketData;
using fixed_income_pricing.Indices;
using fixed_income_pricing.Instruments.Bank;
using fixed_income_pricing.Instruments.Government;
using fixed_income_pricing.MarketData.Interface;
using fixed_income_pricing.Pricing;
using fixed_income_pricing.Risk;

var calendar = new B3Calendar(2020, 2030);
var dayCount = new Bus252();
var interpolator = new FlatForwardInterpolator();
var bootstrapper = new ICurveBootstrapper.PreFixedCurveBootstrapper(calendar, dayCount, interpolator);

var referenceDate = new DateTime(2026, 9, 10);
var ltn1 = new Ltn("LTN-2027", referenceDate, new DateTime(2027, 1, 1));
var ltn2 = new Ltn("LTN-2028", referenceDate, new DateTime(2028, 1, 1));

var quotes = new List<ICurveBootstrapper.LtnQuote>
{
    new ICurveBootstrapper.LtnQuote(ltn1, MarketPrice: 920.50),
    new ICurveBootstrapper.LtnQuote(ltn2, MarketPrice: 845.30)
};

var curve = bootstrapper.Bootstrap(referenceDate, quotes);

Console.WriteLine($"Zero rate at {ltn2.MaturityDate:yyyy-MM-dd}: {curve.ZeroRate(ltn2.MaturityDate):P4}");
Console.WriteLine($"Interpolated DF at 2027-06-01: {curve.DiscountFactor(new DateTime(2027, 6, 1)):F6}");

double impliedPrice = ltn1.FaceValue * curve.DiscountFactor(ltn1.MaturityDate);
Console.WriteLine($"Round-trip check: {impliedPrice:F2} (input was 920.50)");

var bondEngine = new GovernmentBondPricingEngine();
var bondResult = bondEngine.Price(ltn1, curve, referenceDate);
Console.WriteLine($"LTN PV: {bondResult.presentValue:F2}");

var cdiRates = new Dictionary<DateTime, double> { [calendar.AddBusinessDay(referenceDate, 1)] = 0.1075 };
var cdiIndex = new CDIIndex(cdiRates, calendar);
var cdb = new PostFixedCDB("CDB-2027", referenceDate, new DateTime(2027, 1, 1), notional: 100000, percentualCdi: 1.0, cdiIndex);

var cdbEngine = new CdbPricingEngine();
var cdbResult = cdbEngine.Price(cdb, curve, referenceDate);
Console.WriteLine($"CDB value today: {cdbResult.presentValue:F2}");


var bondRisk = new GovernmentBondRiskEngine(bondEngine, dayCount, calendar);
var ltnRisk = bondRisk.Compute(ltn1, curve, referenceDate);
Console.WriteLine($"LTN — Mod. Duration: {ltnRisk.ModifiedDuration:F4}, Convexity: {ltnRisk.Convexity:F4}, DV01: {ltnRisk.Dv01:F4}");

var cdbRisk = new CdbRiskEngine(cdbEngine, cdiIndex, dayCount, calendar);
var cdbRiskResult = cdbRisk.Compute(cdb, curve, referenceDate);
Console.WriteLine($"CDB — CDI DV01: {cdbRiskResult.Dv01:F4}");