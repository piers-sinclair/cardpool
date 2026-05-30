namespace CardPool.Cli.Export;

public static class PreviousExportReader
{
    public static List<PreviousExportRecord> Read(string csvPath)
    {
        using var reader = new StreamReader(csvPath, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        });
        csv.Context.RegisterClassMap<PreviousExportRecordMap>();
        return csv.GetRecords<PreviousExportRecord>().ToList();
    }
}

internal sealed class PreviousExportRecordMap : ClassMap<PreviousExportRecord>
{
    public PreviousExportRecordMap()
    {
        Map(m => m.Id).Name("id");
        Map(m => m.Name).Name("name");
        Map(m => m.IsEligible).Name("is_eligible");
    }
}
