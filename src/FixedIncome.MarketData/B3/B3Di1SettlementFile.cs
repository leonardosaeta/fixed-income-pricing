using System.Globalization;
using System.Text;

namespace FixedIncome.MarketData.B3;

public sealed record Di1Settlement(string Ticker, decimal SettlementPu);

// DI1 settlement prices (PU de ajuste) saved from B3's daily bulletin as a ';'- or ','-separated file with a
// header row. The contract column may hold "DI1F27" or the bare maturity code "F27"; values use pt-BR decimals.
// B3 retired the old "ajustes do pregão" page, so there is no downloader yet.
public static class B3Di1SettlementFile
{
    private static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly string[] TickerColumns = ["ticker", "contrato", "vencimento"];
    private static readonly string[] PuColumns = ["precodeajusteatual", "puajuste", "ajuste", "pu"];

    public static IReadOnlyList<Di1Settlement> Parse(string path)
    {
        using var reader = new StreamReader(path, Encoding.Latin1);
        return Parse(reader);
    }

    public static IReadOnlyList<Di1Settlement> Parse(TextReader reader)
    {
        var settlements = new List<Di1Settlement>();
        (int ticker, int pu)? cols = null;
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

            settlements.Add(new Di1Settlement(ticker, decimal.Parse(f[cols.Value.pu].Trim(), NumberStyles.Number, Br)));
        }

        if (cols is null)
            throw new InvalidDataException("No header row found");
        return settlements;
    }

    private static (int, int) FindColumns(string[] header)
    {
        int Find(string[] names, string what)
        {
            foreach (var name in names)
            {
                int i = Array.IndexOf(header, name);
                if (i >= 0) return i;
            }
            throw new InvalidDataException($"No {what} column; expected one of [{string.Join(", ", names)}]");
        }

        return (Find(TickerColumns, "contract"), Find(PuColumns, "settlement PU"));
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
