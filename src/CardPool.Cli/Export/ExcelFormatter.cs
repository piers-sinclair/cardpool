namespace CardPool.Cli.Export;

internal static class ExcelFormatter
{
    private static readonly Dictionary<string, double> ColumnWidths =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["name"]            = 35,
            ["card_type"]       = 28,
            ["attribute"]       = 12,
            ["subtype"]         = 15,
            ["level"]           =  8,
            ["atk"]             =  8,
            ["def"]             =  8,
            ["materials"]       = 35,
            ["shortest_errata"] = 65,
            ["eligible_since"]  = 18,
            ["word_count"]      = 12,
            ["archetype"]       = 22,
            ["scale"]           =  8,
            ["linkval"]         = 10,
            ["linkmarkers"]     = 15,
            ["latest_errata"]   = 50,
            ["id"]              = 10,
            ["image_url"]       = 12,
            ["is_eligible"]     = 13,
        };

    public static void AddSheet(
        XLWorkbook wb,
        string sheetName,
        string[] cols,
        List<NormalizedRow> rows,
        Func<NormalizedRow, object?[]> getValues)
    {
        var ws = wb.Worksheets.Add(sheetName);

        WriteHeaders(ws, cols);

        for (var rowIdx = 0; rowIdx < rows.Count; rowIdx++)
        {
            var values = getValues(rows[rowIdx]);
            for (var colIdx = 0; colIdx < values.Length; colIdx++)
                SetCell(ws.Cell(rowIdx + 2, colIdx + 1), values[colIdx]);
        }

        ws.RangeUsed()?.SetAutoFilter();
        ApplyColumnWidths(ws, cols);
        ApplyDataRowHeights(ws, rows.Count);
        StyleHeaderRow(ws, cols.Length);
        ws.SheetView.Freeze(1, 1);

        if (rows.Count > 0)
            ApplyDataCellFormatting(ws, rows.Count, cols);
    }

    public static void WriteHeaders(IXLWorksheet ws, string[] cols)
    {
        for (var i = 0; i < cols.Length; i++)
            ws.Cell(1, i + 1).Value = cols[i];
    }

    public static void StyleHeaderRow(IXLWorksheet ws, int colCount)
    {
        var headerRange = ws.Range(1, 1, 1, colCount);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#2F5597"));
        headerRange.Style.Font.SetFontColor(XLColor.White);
        headerRange.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        headerRange.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        ws.Row(1).Height = 20;
    }

    public static void SetCell(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null: break;
            case int i: cell.Value = i; break;
            case bool b: cell.Value = b; break;
            case string s: cell.Value = s; break;
            default: cell.Value = value.ToString(); break;
        }
    }

    private static void ApplyColumnWidths(IXLWorksheet ws, string[] cols)
    {
        for (var i = 0; i < cols.Length; i++)
            ws.Column(i + 1).Width = ColumnWidths.TryGetValue(cols[i], out var w) ? w : 15;
    }

    private static void ApplyDataRowHeights(IXLWorksheet ws, int rowCount)
    {
        for (var i = 2; i <= rowCount + 1; i++)
            ws.Row(i).Height = 28;
    }

    private static void ApplyDataCellFormatting(IXLWorksheet ws, int rowCount, string[] cols)
    {
        ws.Range(2, 1, rowCount + 1, cols.Length).Style
            .Alignment.SetWrapText(true)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Top);

        var isEligibleIdx = Array.IndexOf(cols, "is_eligible");
        if (isEligibleIdx >= 0)
            ApplyEligibilityConditionalFormatting(ws, rowCount, isEligibleIdx + 1);

        var wordCountIdx = Array.IndexOf(cols, "word_count");
        if (wordCountIdx >= 0)
            ws.Range(2, wordCountIdx + 1, rowCount + 1, wordCountIdx + 1).Style
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
    }

    private static void ApplyEligibilityConditionalFormatting(IXLWorksheet ws, int rowCount, int colIdx)
    {
        var colLetter = ws.Cell(2, colIdx).Address.ColumnLetter;
        var range = ws.Range(2, colIdx, rowCount + 1, colIdx);
        range.AddConditionalFormat()
            .WhenIsTrue($"${colLetter}2=TRUE")
            .Fill.SetBackgroundColor(XLColor.FromHtml("#C6EFCE"));
        range.AddConditionalFormat()
            .WhenIsTrue($"${colLetter}2=FALSE")
            .Fill.SetBackgroundColor(XLColor.FromHtml("#FFC7CE"));
    }
}
