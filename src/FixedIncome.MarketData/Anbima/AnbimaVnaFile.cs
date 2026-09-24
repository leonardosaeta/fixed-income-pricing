using System.Globalization;
using System.Text;

namespace FixedIncome.MarketData.Anbima;

public sealed record AnbimaVna(
    string Instrument,
    DateOnly ReferenceDate,
    decimal Vna);

public static class AnbimaVnaFile
{
    private static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");

    public static IReadOnlyList<AnbimaVna> Parse(string path)
    {
        using var reader = new StreamReader(path, Encoding.Latin1);
        return Parse(reader);
    }

    public static IReadOnlyList<AnbimaVna> Parse(TextReader reader)
    {
        var vnas = new List<AnbimaVna>();
        DateOnly? referenceDate = null;
        var inTable = false;

        while (reader.ReadLine() is { } line)
        {
            if (line.StartsWith("Data de Refer", StringComparison.OrdinalIgnoreCase))
            {
                var value = line[(line.IndexOf(':') + 1)..].Trim();
                referenceDate = DateOnly.ParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                continue;
            }

            if (line.StartsWith("Titulo;", StringComparison.OrdinalIgnoreCase))
            {
                inTable = true;
                continue;
            }

            if (!inTable) continue;

            var f = line.Split(';');
            if (f.Length < 3 || f[0].Trim().Length == 0) continue;

            if (referenceDate is null)
                throw new InvalidDataException("VNA file has no reference date line");

            vnas.Add(new AnbimaVna(
                f[0].Trim(),
                referenceDate.Value,
                decimal.Parse(f[2].Trim(), NumberStyles.Number, Br)));
        }

        if (!inTable)
            throw new InvalidDataException("No VNA table found");
        return vnas;
    }
}
