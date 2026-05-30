using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace CardPool.Cli.Export;

public static class CsvExporter
{
    public static void Export(List<NormalizedRow> rows, string path)
    {
        using var writer = new StreamWriter(path, append: false, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        var hasMaterials = rows.Any(r => r.Materials != null);
        var cols = hasMaterials ? NormalizedRow.OutputColumnsWithMaterials : NormalizedRow.OutputColumns;

        foreach (var col in cols)
            csv.WriteField(col);
        csv.NextRecord();

        foreach (var r in rows)
        {
            foreach (var value in GetRowValues(r, hasMaterials))
                csv.WriteField(value);
            csv.NextRecord();
        }
    }

    private static object?[] GetRowValues(NormalizedRow r, bool includeMaterials) =>
        includeMaterials
        ? [
            r.Name, r.Type, r.Attribute, r.Race, r.Level, r.Atk, r.Def,
            r.Materials, r.ShortestErrata, r.EligibleSinceText, r.WordCount, r.Archetype,
            r.Scale, r.LinkVal, r.LinkMarkers,
            r.LatestErrata, r.Id, r.ImageUrl, r.IsEligible
          ]
        : [
            r.Name, r.Type, r.Attribute, r.Race, r.Level, r.Atk, r.Def,
            r.ShortestErrata, r.EligibleSinceText, r.WordCount, r.Archetype,
            r.Scale, r.LinkVal, r.LinkMarkers,
            r.LatestErrata, r.Id, r.ImageUrl, r.IsEligible
          ];
}
