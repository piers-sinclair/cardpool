namespace CardPool.Cli.Export;

internal static class WorksheetFormatter
{
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
}
