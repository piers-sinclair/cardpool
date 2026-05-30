namespace CardPool.Tests;

public class ReleaseNotesExporterTests
{
    private static NormalizedRow MakeRow(int id, DateOnly? eligibleSince, int wordLimit = 25) => new()
    {
        Id = id,
        Name = $"Card {id}",
        Type = "Effect Monster",
        ShortestErrata = "Test errata text.",
        LatestErrata = "Test errata text.",
        EligibleSince = eligibleSince,
        WordLimit = wordLimit,
    };

    private static XLWorkbook ExportAndOpen(List<NormalizedRow> rows, DateOnly since)
    {
        var path = Path.GetTempFileName() + ".xlsx";
        try
        {
            ReleaseNotesExporter.Export(rows, since, path);
            return new XLWorkbook(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static int DataRowCount(XLWorkbook wb) =>
        wb.Worksheets.First().RangeUsed()?.RowCount() - 1 ?? 0;

    [Fact]
    public void Export_EligibleCardWithEligibleSinceOnCutoff_AppearsInSheet()
    {
        var since = new DateOnly(2026, 1, 1);
        var rows = new List<NormalizedRow> { MakeRow(1, eligibleSince: since) };

        using var wb = ExportAndOpen(rows, since);

        DataRowCount(wb).ShouldBe(1);
    }

    [Fact]
    public void Export_EligibleCardWithEligibleSinceAfterCutoff_AppearsInSheet()
    {
        var since = new DateOnly(2026, 1, 1);
        var rows = new List<NormalizedRow> { MakeRow(1, eligibleSince: new DateOnly(2026, 6, 1)) };

        using var wb = ExportAndOpen(rows, since);

        DataRowCount(wb).ShouldBe(1);
    }

    [Fact]
    public void Export_EligibleCardWithEligibleSinceBeforeCutoff_ExcludedFromSheet()
    {
        var since = new DateOnly(2026, 1, 1);
        var rows = new List<NormalizedRow> { MakeRow(1, eligibleSince: new DateOnly(2025, 12, 31)) };

        using var wb = ExportAndOpen(rows, since);

        DataRowCount(wb).ShouldBe(0);
    }

    [Fact]
    public void Export_IneligibleCardWithEligibleSinceAfterCutoff_ExcludedFromSheet()
    {
        var since = new DateOnly(2026, 1, 1);
        var rows = new List<NormalizedRow> { MakeRow(1, eligibleSince: new DateOnly(2026, 6, 1), wordLimit: 1) };

        using var wb = ExportAndOpen(rows, since);

        DataRowCount(wb).ShouldBe(0);
    }

    [Fact]
    public void Export_EligibleCardWithNullEligibleSince_ExcludedFromSheet()
    {
        var since = new DateOnly(2026, 1, 1);
        var rows = new List<NormalizedRow> { MakeRow(1, eligibleSince: null) };

        using var wb = ExportAndOpen(rows, since);

        DataRowCount(wb).ShouldBe(0);
    }

    [Fact]
    public void Export_MixedCards_OnlyNewlyEligibleAppear()
    {
        var since = new DateOnly(2026, 1, 1);
        var rows = new List<NormalizedRow>
        {
            MakeRow(1, eligibleSince: new DateOnly(2026, 3, 1)),
            MakeRow(2, eligibleSince: new DateOnly(2025, 6, 1)),
            MakeRow(3, eligibleSince: new DateOnly(2026, 6, 1)),
            MakeRow(4, eligibleSince: new DateOnly(2026, 6, 1), wordLimit: 1),
        };

        using var wb = ExportAndOpen(rows, since);

        DataRowCount(wb).ShouldBe(2);
    }
}
