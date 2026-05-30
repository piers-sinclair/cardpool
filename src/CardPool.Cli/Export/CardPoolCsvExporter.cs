namespace CardPool.Cli.Export;

public static class CardPoolCsvExporter
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
            foreach (var value in r.GetValues(hasMaterials))
                csv.WriteField(value);
            csv.NextRecord();
        }
    }
}
