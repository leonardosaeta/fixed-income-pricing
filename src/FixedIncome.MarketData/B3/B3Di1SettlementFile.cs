using System.Globalization;
using System.Text;

namespace FixedIncome.MarketData.B3;

public sealed record Di1Settlement(string Ticker, decimal SettlementPu, decimal? SettlementRate = null);

public static class B3Di1SettlementFile
{
    private static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly string[] TickerColumns = ["ticker", "contrato", "vencimento"];
    private static readonly string[] PuColumns = ["precodeajusteatual", "puajuste", "ajuste", "pu"];
    private static readonly string[] RateColumns = ["taxadeajusteatual", "taxaajuste"];

    public static IReadOnlyList<Di1Settlement> Parse(string path)
    {
        using var reader = new StreamReader(path, Encoding.Latin1);
        return Parse(reader);
    }

    public static IReadOnlyList<Di1Settlement> Parse(TextReader reader)
    {
        var settlements = new List<Di1Settlement>();
        (int ticker, int pu, int? rate)? cols = null;
        var delimiter = ';';

        while (reader.ReadLine() is { } line)
        {
            if (line.Trim().Length == 0) continue;

            if (cols is null)
            {
                delimiter = line.Contains(';') ? ';' : ',';
                cols = FindColumns(line.Split(delimiter).Select(Normalize).ToArray());
                continue;
            }

            var f = line.Split(delimiter);
            var ticker = f[cols.Value.ticker].Trim().ToUpperInvariant();
            if (ticker.Length == 0) continue;
            if (!ticker.StartsWith("DI1")) ticker = "DI1" + ticker;

            decimal? rate = cols.Value.rate is { } r && f[r].Trim().Length > 0
                ? decimal.Parse(f[r].Trim(), NumberStyles.Number, Br) / 100m
                : null;
            settlements.Add(new Di1Settlement(ticker, decimal.Parse(f[cols.Value.pu].Trim(), NumberStyles.Number, Br), rate));
        }

        if (cols is null)
            throw new InvalidDataException("No header row found");
        return settlements;
    }

    // Same layout Parse reads: ticker, settlement PU and settlement rate in %, pt-BR decimals.
    public static void Write(IEnumerable<Di1Settlement> settlements, string path)
    {
        using var writer = new StreamWriter(path, false, Encoding.Latin1);
        writer.WriteLine("Ticker;PU Ajuste;Taxa Ajuste");
        foreach (var s in settlements)
            writer.WriteLine($"{s.Ticker};{s.SettlementPu.ToString("0.00", Br)};{(s.SettlementRate * 100m)?.ToString("0.000###", Br)}");
    }

    private static (int, int, int?) FindColumns(string[] header)
    {
        int? TryFind(string[] names) =>
            names.Select(name => Array.IndexOf(header, name)).Where(i => i >= 0).Cast<int?>().FirstOrDefault();

        int Find(string[] names, string what) =>
            TryFind(names) ?? throw new InvalidDataException($"No {what} column; expected one of [{string.Join(", ", names)}]");

        return (Find(TickerColumns, "contract"), Find(PuColumns, "settlement PU"), TryFind(RateColumns));
    }

    private static string Normalize(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s.Trim().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
        }
        return sb.ToString();
    }
}
