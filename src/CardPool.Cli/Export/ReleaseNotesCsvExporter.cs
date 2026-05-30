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

        using var writer = new StreamWriter(path, append: false, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        foreach (var col in cols)
            csv.WriteField(col);
        csv.NextRecord();

        foreach (var r in newlyEligible)
        {
            foreach (var value in r.GetValues(hasMaterials))
                csv.WriteField(value);
            csv.NextRecord();
        }
    }
}
