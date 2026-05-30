namespace CardPool.Cli.Export;

public static class ReleaseNotesCsvExporter
{
    public static void Export(List<NormalizedRow> rows, DateOnly since, string path)
    {
        var newlyEligible = rows
            .Where(r => r.IsEligible && r.EligibleSince >= since)
            .ToList();

        var hasMaterials = newlyEligible.Any(r => r.Materials != null);
        var cols = hasMaterials ? NormalizedRow.OutputColumnsWithMaterials : NormalizedRow.OutputColumns;
        CsvFormatter.Write(path, cols, newlyEligible, r => r.GetValues(hasMaterials));
    }
}
