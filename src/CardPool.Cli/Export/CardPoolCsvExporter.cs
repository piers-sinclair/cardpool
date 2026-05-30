namespace CardPool.Cli.Export;

public static class CardPoolCsvExporter
{
    public static void Export(List<NormalizedRow> rows, string path)
    {
        var hasMaterials = rows.Any(r => r.Materials != null);
        var cols = hasMaterials ? NormalizedRow.OutputColumnsWithMaterials : NormalizedRow.OutputColumns;
        CsvFormatter.Write(path, cols, rows, r => r.GetValues(hasMaterials));
    }
}
