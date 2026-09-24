namespace FixedIncome.MarketData.Anbima;

public static class AnbimaClient
{
    private const string SecondaryMarketUrl = "https://www.anbima.com.br/informacoes/merc-sec/arqs/";
    private const string VnaUrl = "https://www.anbima.com.br/informacoes/vna/vna-down.asp";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    public static string SecondaryMarketFileName(DateOnly date) => $"ms{date:yyMMdd}.txt";

    public static string VnaFileName(DateOnly date) => $"vna{date:yyMMdd}.txt";

    public static async Task<bool> TryDownloadSecondaryMarketAsync(DateOnly date, string path)
    {
        using var response = await Http.GetAsync(SecondaryMarketUrl + SecondaryMarketFileName(date));
        if (!response.IsSuccessStatusCode) return false;

        await File.WriteAllBytesAsync(path, await response.Content.ReadAsByteArrayAsync());
        return true;
    }

    public static async Task<bool> TryDownloadVnaAsync(DateOnly date, string path)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Data"] = date.ToString("ddMMyyyy"),
            ["escolha"] = "2",
            ["Idioma"] = "PT",
            ["saida"] = "txt",
        });

        using var response = await Http.PostAsync(VnaUrl, form);
        if (!response.IsSuccessStatusCode) return false;

        var bytes = await response.Content.ReadAsByteArrayAsync();
        if (!System.Text.Encoding.Latin1.GetString(bytes).Contains("NTN-B;")) return false;

        await File.WriteAllBytesAsync(path, bytes);
        return true;
    }
}
