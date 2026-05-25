using ClosedXML.Excel;
using YgoDb.Cli.Models;

namespace YgoDb.Cli.Export;

public static class ExcelExporter
{
    private static readonly HashSet<string> WideColumns =
        new(["name", "materials", "shortest_errata", "latest_errata"], StringComparer.OrdinalIgnoreCase);

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

        for (var i = 0; i < cols.Length; i++)
            ws.Cell(1, i + 1).Value = cols[i];

        for (var rowIdx = 0; rowIdx < rows.Count; rowIdx++)
        {
            var r = rows[rowIdx];
            var values = GetRowValues(r);
            for (var colIdx = 0; colIdx < values.Length; colIdx++)
                SetCell(ws.Cell(rowIdx + 2, colIdx + 1), values[colIdx]);
        }

        ws.RangeUsed()?.SetAutoFilter();

        for (var i = 0; i < cols.Length; i++)
        {
            var col = ws.Column(i + 1);
            col.Width = WideColumns.Contains(cols[i]) ? 45 : 15;
        }

        ws.RangeUsed()?.Style
            .Alignment.SetWrapText(true)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Top);
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
        r.Id, r.Name, r.Type, r.Race, r.Attribute, r.Level, r.Atk, r.Def,
        r.Scale, r.LinkVal, r.LinkMarkers, r.Archetype, r.Desc, r.Materials,
        r.ShortestErrata, r.LatestErrata, r.WordCount, r.IsEligible,
        r.SetName, r.SetCode, r.SetRarity, r.BanTcg, r.BanOcg, r.ImageUrl
    ];
}
