namespace FixedIncome.MarketData.B3;

public static class B3Client
{
    private const string PriceReportUrl = "https://www.b3.com.br/pesquisapregao/download?filelist=";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(120) };

    public static string PriceReportFileName(DateOnly date) => $"PR{date:yyMMdd}.zip";

    public static string Di1SettlementFileName(DateOnly date) => $"di1_{date:yyMMdd}.csv";

    // Downloads the day's price report (~12 MB) and keeps only the DI1 settlements, written where
    // B3Di1SettlementFile.Parse reads them. False when B3 has no report for the date (holiday, not yet published).
    public static async Task<bool> TryDownloadDi1SettlementsAsync(DateOnly date, string path)
    {
        using var response = await Http.GetAsync(PriceReportUrl + PriceReportFileName(date));
        if (!response.IsSuccessStatusCode) return false;

        using var zip = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        var settlements = B3PriceReportFile.ReadDi1(zip);
        if (settlements.Count == 0) return false;

        B3Di1SettlementFile.Write(settlements, path);
        return true;
    }
}
