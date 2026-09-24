using System.Globalization;
using System.Text;

namespace FixedIncome.MarketData.Anbima;


public sealed record AnbimaQuote(
    string Instrument,
    DateOnly ReferenceDate,
    DateOnly Maturity,
    decimal IndicativeRate,
    decimal Pu);

public static class AnbimaSecondaryMarketFile
{
    private static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly string[] DateFormats = ["yyyyMMdd", "dd/MM/yyyy"];
    
    public static IReadOnlyList<AnbimaQuote> Parse(string path)
    {
        using var reader = new StreamReader(path, Encoding.Latin1);
        return Parse(reader);
    }

    public static IReadOnlyList<AnbimaQuote> Parse(TextReader reader)
    {
        var quotes = new List<AnbimaQuote>();
        Dictionary<string, int>? cols = null;
        var delimiter = ',';

        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0) continue;

            if (cols is null)
            {
                var d = line.Contains('@') ? '@' : ',';
                var h = line.Split(d);
                if (h.Length >= 5 && h.Any(x => Normalize(x) == "titulo"))
                {
                    delimiter = d;
                    cols = BuildColumnMap(h);
                }
                continue;
            }

            var f = line.Split(delimiter);
            if (f.Length < 5) continue;
            
            var titulo = f[cols["titulo"]].Trim();
            if(titulo.Length==0) continue;
            
            quotes.Add(new AnbimaQuote(
                titulo,
                ParseDate(f[cols["datareferencia"]]),
                ParseDate(f[cols["datavencimento"]]),
                ParseDecimal(f[cols["txindicativas"]]) /100m,
                ParseDecimal(f[cols["pu"]])));
        }
        if (cols is null)
            throw new InvalidDataException("No header row found");
        return quotes;
    }

    private static decimal ParseDecimal(string s)=>decimal.Parse(s.Trim(),NumberStyles.Number, Br);

    private static DateOnly ParseDate(string s) =>  DateOnly.ParseExact(s.Trim(), DateFormats, CultureInfo.InvariantCulture);

    private static Dictionary<string, int>? BuildColumnMap(string[] header)
    {
        var map  = new Dictionary<string, int>();
        for (var i = 0; i < header.Length; i++)
        {
            var key = Normalize(header[i]);
            if (key.Length > 0) map.TryAdd(key, i);
        }

        foreach (var required in new[]
                     { "titulo", "datareferencia", "datavencimento", "txindicativas", "pu" })
        {
            if (!map.ContainsKey(required))
            {
                throw new InvalidDataException($"Column {required} not found");
            }
        }

        return map;
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