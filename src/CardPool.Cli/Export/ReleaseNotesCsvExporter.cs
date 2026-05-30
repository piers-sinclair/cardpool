namespace CardPool.Cli.Export;

public static class ReleaseNotesCsvExporter
{
    public static void Export(List<NormalizedRow> rows, DateOnly since, string path)
    {
        var newlyEligible = rows
            .Where(r => r.IsEligible && r.EligibleSince >= since)
            .ToList();

        using var writer = new StreamWriter(path, append: false, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        foreach (var col in NormalizedRow.ReleaseNotesColumns)
            csv.WriteField(col);
        csv.NextRecord();

        foreach (var r in newlyEligible)
        {
            foreach (var value in r.GetReleaseNotesValues())
                csv.WriteField(value);
            csv.NextRecord();
        }
    }
}
