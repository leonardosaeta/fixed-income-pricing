using System.Xml.Serialization;
using fixed_income_pricing.Dates;
using fixed_income_pricing.Indices;
var calendar = new B3Calendar(2020, 2030);
Console.WriteLine(calendar.IsHoliday(new DateTime(2026, 2, 17)));

Console.WriteLine(calendar.IsBusinessDays(new DateTime(2026, 8, 26)));

int bd = calendar.CountBusinessDaysBetween(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
Console.WriteLine(bd);
