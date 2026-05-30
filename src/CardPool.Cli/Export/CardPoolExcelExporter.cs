namespace CardPool.Cli.Export;

public static class CardPoolExcelExporter
{
    public static void Export(List<NormalizedRow> rows, string path, int wordLimit)
    {
        using var wb = new XLWorkbook();
        AddSheet(wb, $"<={wordLimit} Words", rows.Where(r => r.IsEligible).ToList());
        AddSheet(wb, $">{wordLimit} Words", rows.Where(r => !r.IsEligible).ToList());
        wb.SaveAs(path);
    }

    private static void AddSheet(XLWorkbook wb, string sheetName, List<NormalizedRow> rows)
    {
        var hasMaterials = rows.Any(r => r.Materials != null);
        var cols = hasMaterials ? NormalizedRow.OutputColumnsWithMaterials : NormalizedRow.OutputColumns;
        XlsxSheetWriter.AddSheet(wb, sheetName, cols, rows, r => r.GetValues(hasMaterials));
    }
}
