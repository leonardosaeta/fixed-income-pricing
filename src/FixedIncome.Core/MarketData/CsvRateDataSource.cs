using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using fixed_income_pricing.MarketData.Interface;

namespace fixed_income_pricing.MarketData;

public class CsvRateDataSource : IRateDataSource
{
    private readonly string _filePath;

    public CsvRateDataSource(string filePath)
    {
        _filePath = filePath;
    }

    public IReadOnlyDictionary<DateTime, double> GetDailyRates(int seriesCode, DateTime startDate, DateTime endDate)
    {
        var result = new Dictionary<DateTime, double>();
        foreach (var line in File.ReadLines(_filePath).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',');
            DateTime date = DateTime.ParseExact(parts[0], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            double rate = double.Parse(parts[1], CultureInfo.InvariantCulture);

            if (date >= startDate && date <= endDate) result[date] = rate;
        }

        return result;
    }
    
    public static void WriteCsv(IReadOnlyDictionary<DateTime, double> rates, string filePath)
    {
        using var writer = new StreamWriter(filePath);
        writer.WriteLine("date,rate");
        foreach (var kvp in rates.OrderBy(k=>k.Key))
            writer.WriteLine($"{kvp.Key:yyyy-MM-dd},{kvp.Value.ToString(CultureInfo.InvariantCulture)}");
    }
}