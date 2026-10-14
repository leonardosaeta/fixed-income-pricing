using fixed_income_pricing.Dates;
using FixedIncome.MarketData.B3;

namespace FixedIncome.Core.Tests.Instruments;

internal static class B3TestData
{
    private const int MaxBusinessDaysBack = 10;

    public static readonly B3Calendar Calendar = new(2020, 2070);

    private static readonly Lazy<DateOnly> Latest = new(ResolveReferenceDate);

    private static string Dir => Path.Combine(AppContext.BaseDirectory, "data", "b3");

    public static DateOnly ReferenceDate => Latest.Value;

    public static IReadOnlyList<Di1Settlement> Di1Settlements() =>
        B3Di1SettlementFile.Parse(Path.Combine(Dir, B3Client.Di1SettlementFileName(ReferenceDate)));

    private static DateOnly ResolveReferenceDate()
    {
        Directory.CreateDirectory(Dir);

        var date = DateOnly.FromDateTime(DateTime.Today);
        if (!Calendar.IsBusinessDays(date)) date = Calendar.AddBusinessDay(date, -1);

        for (var i = 0; i < MaxBusinessDaysBack; i++, date = Calendar.AddBusinessDay(date, -1))
        {
            var path = Path.Combine(Dir, B3Client.Di1SettlementFileName(date));
            try
            {
                if (File.Exists(path) || B3Client.TryDownloadDi1SettlementsAsync(date, path).GetAwaiter().GetResult())
                    return date;
            }
            catch (HttpRequestException) { break; }
            catch (TaskCanceledException) { break; }
        }

        return LatestCachedDate()
               ?? throw new InvalidOperationException(
                   $"No B3 data: download failed and no DI1 settlement file cached in {Dir}.");
    }

    private static DateOnly? LatestCachedDate() =>
        Directory.GetFiles(Dir, "di1_??????.csv")
            .Select(p => DateOnly.ParseExact(Path.GetFileNameWithoutExtension(p)[4..], "yyMMdd"))
            .OrderDescending()
            .Cast<DateOnly?>()
            .FirstOrDefault();
}
