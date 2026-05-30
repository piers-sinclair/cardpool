namespace CardPool.Cli.Export;

public static class ReleaseNotesExcelExporter
{
    public static void Export(List<NormalizedRow> rows, DateOnly since, string path)
    {
        var newlyEligible = rows
            .Where(r => r.IsEligible && r.EligibleSince >= since)
            .ToList();

        using var wb = new XLWorkbook();
        XlsxSheetWriter.AddSheet(
            wb,
            $"Eligible Since {since.ToString(AppConstants.IsoDateFormat, CultureInfo.InvariantCulture)}",
            NormalizedRow.ReleaseNotesColumns,
            newlyEligible,
            r => r.GetReleaseNotesValues());
        wb.SaveAs(path);
    }
}
