using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml;

namespace FixedIncome.MarketData.B3;

// B3's daily price report ("Boletim de preços", PRyyMMdd.zip from Pesquisa por pregão): a zip holding a zip of
// BVBG.086 XML files, one per republication of the day. Each derivative has a PricRpt with its settlement price
// (AdjstdQt) and, for rate contracts, the settlement rate in % (AdjstdQtTax).
public static partial class B3PriceReportFile
{
    [GeneratedRegex("^DI1[FGHJKMNQUVXZ][0-9]{2}$")]
    private static partial Regex Di1Ticker();

    public static IReadOnlyList<Di1Settlement> ReadDi1(string path)
    {
        using var stream = File.OpenRead(path);
        return ReadDi1(stream);
    }

    // Empty for a day without trading: B3 then serves an archive with no entries.
    public static IReadOnlyList<Di1Settlement> ReadDi1(Stream zip)
    {
        using var outer = new ZipArchive(zip, ZipArchiveMode.Read);
        if (outer.Entries.Count == 0) return [];

        using var innerStream = outer.Entries[0].Open();
        using var inner = new ZipArchive(innerStream, ZipArchiveMode.Read);

        // Later republications supersede earlier ones; their names carry an increasing sequence number.
        var latest = inner.Entries
            .Where(e => e.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .MaxBy(e => e.Name, StringComparer.Ordinal);
        if (latest is null) return [];

        using var xml = latest.Open();
        return ParseDi1(xml);
    }

    public static IReadOnlyList<Di1Settlement> ParseDi1(Stream xml)
    {
        var settlements = new List<Di1Settlement>();
        using var reader = XmlReader.Create(xml, new XmlReaderSettings { IgnoreWhitespace = true });

        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "PricRpt") continue;

            using var report = reader.ReadSubtree();
            if (ReadDi1Report(report) is { } settlement)
                settlements.Add(settlement);
        }

        return settlements;
    }

    private static Di1Settlement? ReadDi1Report(XmlReader report)
    {
        string? ticker = null;
        decimal? pu = null, rate = null;

        // ReadElementContentAsString leaves the reader on the next node, so only advance when nothing was read.
        report.Read();
        while (!report.EOF)
        {
            if (report.NodeType != XmlNodeType.Element)
            {
                report.Read();
                continue;
            }

            switch (report.LocalName)
            {
                case "TckrSymb":
                    ticker = report.ReadElementContentAsString();
                    if (!Di1Ticker().IsMatch(ticker)) return null;
                    break;
                case "AdjstdQt":
                    pu = decimal.Parse(report.ReadElementContentAsString(), CultureInfo.InvariantCulture);
                    break;
                case "AdjstdQtTax":
                    rate = decimal.Parse(report.ReadElementContentAsString(), CultureInfo.InvariantCulture) / 100m;
                    break;
                default:
                    report.Read();
                    break;
            }
        }

        return ticker is not null && pu is not null ? new Di1Settlement(ticker, pu.Value, rate) : null;
    }
}
