using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using YgoDb.Cli.Models;

namespace YgoDb.Cli.Export;

public static class CsvExporter
{
    public static void Export(List<NormalizedRow> rows, string path)
    {
        using var writer = new StreamWriter(path, append: false, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        foreach (var col in NormalizedRow.OutputColumns)
            csv.WriteField(col);
        csv.NextRecord();

        foreach (var r in rows)
        {
            foreach (var value in GetRowValues(r))
                csv.WriteField(value);
            csv.NextRecord();
        }
    }

    private static object?[] GetRowValues(NormalizedRow r) =>
    [
        r.Name, r.WordCount, r.ShortestErrata,
        r.Type, r.Attribute, r.Race, r.Level, r.Atk, r.Def,
        r.Scale, r.LinkVal, r.LinkMarkers, r.Archetype, r.Materials,
        r.SetName, r.SetCode, r.SetRarity,
        r.BanTcg, r.BanOcg,
        r.LatestErrata, r.Desc, r.Id, r.ImageUrl, r.IsEligible
    ];
}
