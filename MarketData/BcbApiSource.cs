using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using fixed_income_pricing.MarketData.Interface;

namespace fixed_income_pricing.MarketData;

public class BcbApiSource:IRateDataSource
{
    private static readonly HttpClient HttpClient = new HttpClient();
    
    public IReadOnlyDictionary<DateTime, double> GetDailyRates(int seriesCode, DateTime startDate, DateTime endDate)
    {
        if ((endDate - startDate).TotalDays > 365 * 10) throw new ArgumentException("BCB API max limit, split the request");
        
        string url = BuildUrl(seriesCode, startDate, endDate);
        string json = HttpClient.GetStringAsync(url).Result;
        return ParseResponse(json);
    }

    private IReadOnlyDictionary<DateTime, double> ParseResponse(string json)
    {
       var result = new Dictionary<DateTime, double>();
       if (result == null) throw new ArgumentNullException(nameof(result));
       
       using var document = JsonDocument.Parse(json);

       foreach (var element in document.RootElement.EnumerateArray())
       {
           string dateStr = element.GetProperty("data").GetString(); // Check warning 
           string valueStr = element.GetProperty("valor").GetString(); // Check warning
           
           DateTime date = DateTime.ParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture); // Check warning
           double rawValue = double.Parse(valueStr, CultureInfo.InstalledUICulture);

           result[date] = rawValue / 100.0;
       }
       return result;
    }

    private string BuildUrl(int seriesCode, DateTime startDate, DateTime endDate)
    {
        string start = startDate.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture);
        string end = endDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        
        return $"https://api.bcb.gov.br/dados/serie/bcdata.sgs.{seriesCode}/dados" +
                   $"?formato=json&dataInicial={start}&dataFinal={end}"; 
    }
}