using fixed_income_pricing.Dates;
using FixedIncome.MarketData.Anbima;

namespace FixedIncome.Core.Tests.Instruments;

internal static class AnbimaTestData
{
    private const int MaxBusinessDaysBack = 10;

    public static readonly B3Calendar Calendar = new(2020, 2070);

    private static readonly Lazy<DateOnly> Latest = new(ResolveReferenceDate);

    private static string Dir => Path.Combine(AppContext.BaseDirectory, "data", "anbima");

    public static DateOnly ReferenceDate => Latest.Value;

    public static IReadOnlyList<AnbimaQuote> Quotes(string instrument) =>
        AnbimaSecondaryMarketFile.Parse(Path.Combine(Dir, AnbimaClient.SecondaryMarketFileName(ReferenceDate)))
            .Where(q => q.Instrument.Equals(instrument, StringComparison.OrdinalIgnoreCase))
            .ToList();

    public static decimal Vna(string instrument) =>
        AnbimaVnaFile.Parse(Path.Combine(Dir, AnbimaClient.VnaFileName(ReferenceDate)))
            .Single(v => v.Instrument.Equals(instrument, StringComparison.OrdinalIgnoreCase))
            .Vna;

    private static DateOnly ResolveReferenceDate()
    {
        Directory.CreateDirectory(Dir);

        var date = DateOnly.FromDateTime(DateTime.Today);
        if (!Calendar.IsBusinessDays(date)) date = Calendar.AddBusinessDay(date, -1);

        for (var i = 0; i < MaxBusinessDaysBack; i++, date = Calendar.AddBusinessDay(date, -1))
        {
            try
            {
                if (EnsureDownloaded(date)) return date;
            }
            catch (HttpRequestException) { break; }
            catch (TaskCanceledException) { break; }
        }

        return LatestCachedDate()
               ?? throw new InvalidOperationException(
                   $"No ANBIMA data: download failed and no ms/vna pair cached in {Dir}.");
    }

    private static bool EnsureDownloaded(DateOnly date)
    {
        var ms = Path.Combine(Dir, AnbimaClient.SecondaryMarketFileName(date));
        var vna = Path.Combine(Dir, AnbimaClient.VnaFileName(date));

        if (!File.Exists(ms) && !AnbimaClient.TryDownloadSecondaryMarketAsync(date, ms).GetAwaiter().GetResult())
            return false;

        return File.Exists(vna) || AnbimaClient.TryDownloadVnaAsync(date, vna).GetAwaiter().GetResult();
    }

    private static DateOnly? LatestCachedDate() =>
        Directory.GetFiles(Dir, "ms??????.txt")
            .Select(p => DateOnly.ParseExact(Path.GetFileNameWithoutExtension(p)[2..], "yyMMdd"))
            .Where(d => File.Exists(Path.Combine(Dir, AnbimaClient.VnaFileName(d))))
            .OrderDescending()
            .Cast<DateOnly?>()
            .FirstOrDefault();
}
