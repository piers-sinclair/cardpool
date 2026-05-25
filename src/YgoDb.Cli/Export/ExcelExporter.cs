using ClosedXML.Excel;
using YgoDb.Cli.Models;

namespace YgoDb.Cli.Export;

public static class ExcelExporter
{
    private static readonly Dictionary<string, double> ColumnWidths =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["name"]            = 35,
            ["word_count"]      = 12,
            ["shortest_errata"] = 65,
            ["type"]            = 28,
            ["attribute"]       = 12,
            ["race"]            = 15,
            ["level"]           =  8,
            ["atk"]             =  8,
            ["def"]             =  8,
            ["scale"]           =  8,
            ["linkval"]         = 10,
            ["linkmarkers"]     = 15,
            ["archetype"]       = 22,
            ["materials"]       = 35,
            ["set_name"]        = 30,
            ["set_code"]        = 12,
            ["set_rarity"]      = 15,
            ["ban_tcg"]         = 12,
            ["ban_ocg"]         = 12,
            ["latest_errata"]   = 50,
            ["desc"]            = 50,
            ["id"]              = 10,
            ["image_url"]       = 12,
            ["is_eligible"]     = 13,
        };

    private const double DataRowHeight = 35;

    public static void Export(List<NormalizedRow> rows, string path, int wordLimit)
    {
        using var wb = new XLWorkbook();

        var eligible = rows.Where(r => r.IsEligible).ToList();
        var notEligible = rows.Where(r => !r.IsEligible).ToList();

        AddSheet(wb, $"<={wordLimit} Words", eligible);
        AddSheet(wb, $">{wordLimit} Words", notEligible);

        wb.SaveAs(path);
    }

    private static void AddSheet(XLWorkbook wb, string sheetName, List<NormalizedRow> rows)
    {
        var ws = wb.Worksheets.Add(sheetName);
        var cols = NormalizedRow.OutputColumns;

        WriteHeaders(ws, cols);
        WriteDataRows(ws, rows);

        ws.RangeUsed()?.SetAutoFilter();
        ApplyColumnWidths(ws, cols);
        ApplyDefaultDataRowHeight(ws);
        StyleHeaderRow(ws, cols);
        FreezeHeaderAndNameColumn(ws);

        if (rows.Count > 0)
            ApplyDataCellFormatting(ws, rows.Count, cols);
    }

    private static void WriteHeaders(IXLWorksheet ws, string[] cols)
    {
        for (var i = 0; i < cols.Length; i++)
            ws.Cell(1, i + 1).Value = cols[i];
    }

    private static void WriteDataRows(IXLWorksheet ws, List<NormalizedRow> rows)
    {
        for (var rowIdx = 0; rowIdx < rows.Count; rowIdx++)
        {
            var values = GetRowValues(rows[rowIdx]);
            for (var colIdx = 0; colIdx < values.Length; colIdx++)
                SetCell(ws.Cell(rowIdx + 2, colIdx + 1), values[colIdx]);
        }
    }

    private static void ApplyColumnWidths(IXLWorksheet ws, string[] cols)
    {
        for (var i = 0; i < cols.Length; i++)
            ws.Column(i + 1).Width = ColumnWidths.TryGetValue(cols[i], out var w) ? w : 15;
    }

    private static void ApplyDefaultDataRowHeight(IXLWorksheet ws) =>
        ws.RowHeight = DataRowHeight;

    private static void StyleHeaderRow(IXLWorksheet ws, string[] cols)
    {
        var headerRange = ws.Range(1, 1, 1, cols.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#2F5597"));
        headerRange.Style.Font.SetFontColor(XLColor.White);
        headerRange.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        headerRange.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        ws.Row(1).Height = 20;
    }

    private static void FreezeHeaderAndNameColumn(IXLWorksheet ws) =>
        ws.SheetView.Freeze(1, 1);

    private static void ApplyDataCellFormatting(IXLWorksheet ws, int rowCount, string[] cols)
    {
        ws.Range(2, 1, rowCount + 1, cols.Length).Style
            .Alignment.SetWrapText(true)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Top);

        ApplyEligibilityConditionalFormatting(ws, rowCount, cols);
        CentreAlignColumn(ws, rowCount, cols, "word_count");
    }

    private static void ApplyEligibilityConditionalFormatting(IXLWorksheet ws, int rowCount, string[] cols)
    {
        var colIdx = Array.IndexOf(cols, "is_eligible") + 1;
        var colLetter = ws.Cell(2, colIdx).Address.ColumnLetter;
        var range = ws.Range(2, colIdx, rowCount + 1, colIdx);
        range.AddConditionalFormat()
            .WhenIsTrue($"${colLetter}2=TRUE")
            .Fill.SetBackgroundColor(XLColor.FromHtml("#C6EFCE"));
        range.AddConditionalFormat()
            .WhenIsTrue($"${colLetter}2=FALSE")
            .Fill.SetBackgroundColor(XLColor.FromHtml("#FFC7CE"));
    }

    private static void CentreAlignColumn(IXLWorksheet ws, int rowCount, string[] cols, string colName)
    {
        var colIdx = Array.IndexOf(cols, colName) + 1;
        ws.Range(2, colIdx, rowCount + 1, colIdx).Style
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
    }

    private static void SetCell(IXLCell cell, object? value)
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

    private static object?[] GetRowValues(NormalizedRow r) =>
    [
        r.Name, r.WordCount, r.ShortestErrata,
        r.Type, r.Attribute, r.Race, r.Level, r.Atk, r.Def,
        r.Scale, r.LinkVal, r.LinkMarkers, r.Archetype, r.Materials,
        r.SetName, r.SetCode, r.SetRarity,
        r.BanTcg, r.BanOcg,
        r.LatestErrata, r.Desc, r.Id, r.ImageUrl, r.IsEligible
    ];
}
