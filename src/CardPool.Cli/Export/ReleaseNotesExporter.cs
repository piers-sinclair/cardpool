namespace CardPool.Cli.Export;

public static class ReleaseNotesExporter
{
    private static readonly string[] Columns =
        ["name", "card_type", "word_count", "shortest_errata", "eligible_since", "is_eligible"];

    private static readonly Dictionary<string, double> ColumnWidths =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["name"]            = 35,
            ["card_type"]       = 28,
            ["word_count"]      = 12,
            ["shortest_errata"] = 65,
            ["eligible_since"]  = 18,
            ["is_eligible"]     = 13,
        };

    public static void Export(List<NormalizedRow> rows, DateOnly since, string path)
    {
        var newlyEligible = rows
            .Where(r => r.IsEligible && r.EligibleSince >= since)
            .ToList();

        using var wb = new XLWorkbook();
        AddSheet(wb, $"Eligible Since {since.ToString(AppConstants.IsoDateFormat, CultureInfo.InvariantCulture)}", newlyEligible);
        wb.SaveAs(path);
    }

    private static void AddSheet(XLWorkbook wb, string sheetName, List<NormalizedRow> rows)
    {
        var ws = wb.Worksheets.Add(sheetName);

        WorksheetFormatter.WriteHeaders(ws, Columns);
        WriteDataRows(ws, rows);

        ws.RangeUsed()?.SetAutoFilter();
        ApplyColumnWidths(ws);
        WorksheetFormatter.StyleHeaderRow(ws, Columns.Length);
        ws.SheetView.Freeze(1, 1);

        if (rows.Count > 0)
            ApplyDataFormatting(ws, rows.Count);
    }

    private static void WriteDataRows(IXLWorksheet ws, List<NormalizedRow> rows)
    {
        for (var rowIdx = 0; rowIdx < rows.Count; rowIdx++)
        {
            var r = rows[rowIdx];
            object?[] values = [r.Name, r.Type, r.WordCount, r.ShortestErrata, r.EligibleSinceText, r.IsEligible];
            for (var colIdx = 0; colIdx < values.Length; colIdx++)
                WorksheetFormatter.SetCell(ws.Cell(rowIdx + 2, colIdx + 1), values[colIdx]);
        }
    }

    private static void ApplyColumnWidths(IXLWorksheet ws)
    {
        for (var i = 0; i < Columns.Length; i++)
            ws.Column(i + 1).Width = ColumnWidths.TryGetValue(Columns[i], out var w) ? w : 15;
    }

    private static void ApplyDataFormatting(IXLWorksheet ws, int rowCount)
    {
        ws.Range(2, 1, rowCount + 1, Columns.Length).Style
            .Alignment.SetWrapText(true)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Top);

        for (var i = 2; i <= rowCount + 1; i++)
            ws.Row(i).Height = 28;

        var wordCountColIdx = Array.IndexOf(Columns, "word_count") + 1;
        ws.Range(2, wordCountColIdx, rowCount + 1, wordCountColIdx).Style
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
    }
}
