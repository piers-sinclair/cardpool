namespace CardPool.Cli.Export;

internal static class CsvFormatter
{
    public static void Write(string path, string[] cols, List<NormalizedRow> rows, Func<NormalizedRow, object?[]> getValues)
    {
        using var writer = new StreamWriter(path, append: false, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        foreach (var col in cols)
            csv.WriteField(col);
        csv.NextRecord();

        foreach (var r in rows)
        {
            foreach (var value in getValues(r))
                csv.WriteField(value);
            csv.NextRecord();
        }
    }
}
