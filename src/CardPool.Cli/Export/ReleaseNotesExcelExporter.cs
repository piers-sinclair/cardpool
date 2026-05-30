namespace CardPool.Cli.Export;

public static class ReleaseNotesExcelExporter
{
    public static void Export(List<NormalizedRow> rows, DateOnly since, string path)
    {
        var newlyEligible = rows
            .Where(r => r.IsEligible && r.EligibleSince >= since)
            .ToList();

        var hasMaterials = newlyEligible.Any(r => r.Materials != null);
        var cols = hasMaterials ? NormalizedRow.OutputColumnsWithMaterials : NormalizedRow.OutputColumns;

        using var wb = new XLWorkbook();
        ExcelFormatter.AddSheet(
            wb,
            $"Eligible Since {since.ToString(AppConstants.IsoDateFormat, CultureInfo.InvariantCulture)}",
            cols,
            newlyEligible,
            r => r.GetValues(hasMaterials));
        wb.SaveAs(path);
    }
}
