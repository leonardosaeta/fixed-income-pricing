using System.Xml.Serialization;
using fixed_income_pricing;

var service = new fixed_income_pricing.Bus252();
//double result = service.Fator(0.1180f, 126);
double result = service.Fator(0.1180, 126);

Console.WriteLine(result);