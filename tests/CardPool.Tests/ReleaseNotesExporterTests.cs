using ClosedXML.Excel;

namespace CardPool.Tests;

public class ReleaseNotesExporterTests
{
    private static NormalizedRow MakeRow(int id, string name, int wordLimit = 25) => new()
    {
        Id = id,
        Name = name,
        Type = "Effect Monster",
        ShortestErrata = "Test errata text.",
        LatestErrata = "Test errata text.",
        WordLimit = wordLimit,
    };

    private static PreviousExportRecord MakePrev(int id, bool isEligible) =>
        new() { Id = id, Name = "Card", IsEligible = isEligible };

    private static XLWorkbook ExportAndOpen(List<NormalizedRow> current, List<PreviousExportRecord> previous)
    {
        var path = Path.GetTempFileName() + ".xlsx";
        try
        {
            ReleaseNotesExporter.Export(current, previous, path);
            return new XLWorkbook(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Export_CardBecomesEligible_AppearsInNewlyEligibleSheet()
    {
        var current = new List<NormalizedRow> { MakeRow(1, "Card A", wordLimit: 25) };
        var previous = new List<PreviousExportRecord> { MakePrev(1, isEligible: false) };

        using var wb = ExportAndOpen(current, previous);

        wb.Worksheet("Newly Eligible").RangeUsed()!.RowCount().ShouldBe(2);
    }

    [Fact]
    public void Export_CardBecomesIneligible_AppearsInNewlyIneligibleSheet()
    {
        var current = new List<NormalizedRow> { MakeRow(1, "Card A", wordLimit: 1) };
        var previous = new List<PreviousExportRecord> { MakePrev(1, isEligible: true) };

        using var wb = ExportAndOpen(current, previous);

        wb.Worksheet("Newly Ineligible").RangeUsed()!.RowCount().ShouldBe(2);
    }

    [Fact]
    public void Export_NewEligibleCard_AppearsInNewCardsEligibleSheet()
    {
        var current = new List<NormalizedRow> { MakeRow(99, "New Card", wordLimit: 25) };
        var previous = new List<PreviousExportRecord> { MakePrev(1, isEligible: true) };

        using var wb = ExportAndOpen(current, previous);

        wb.Worksheet("New Cards (Eligible)").RangeUsed()!.RowCount().ShouldBe(2);
    }

    [Fact]
    public void Export_UnchangedEligibleCard_DoesNotAppearInDataRows()
    {
        var current = new List<NormalizedRow> { MakeRow(1, "Card A", wordLimit: 25) };
        var previous = new List<PreviousExportRecord> { MakePrev(1, isEligible: true) };

        using var wb = ExportAndOpen(current, previous);

        wb.Worksheet("Newly Eligible").RangeUsed()!.RowCount().ShouldBe(1);
        wb.Worksheet("Newly Ineligible").RangeUsed()!.RowCount().ShouldBe(1);
        wb.Worksheet("New Cards (Eligible)").RangeUsed()!.RowCount().ShouldBe(1);
    }

    [Fact]
    public void Export_NewIneligibleCard_DoesNotAppearInNewCardsEligibleSheet()
    {
        var current = new List<NormalizedRow> { MakeRow(99, "New Ineligible", wordLimit: 1) };
        var previous = new List<PreviousExportRecord> { MakePrev(1, isEligible: true) };

        using var wb = ExportAndOpen(current, previous);

        wb.Worksheet("New Cards (Eligible)").RangeUsed()!.RowCount().ShouldBe(1);
    }

    [Fact]
    public void Export_AllThreeCategoriesPresent_CorrectSheetRowCounts()
    {
        var current = new List<NormalizedRow>
        {
            MakeRow(1, "Became Eligible", wordLimit: 25),
            MakeRow(2, "Became Ineligible", wordLimit: 1),
            MakeRow(3, "Totally New", wordLimit: 25),
        };
        var previous = new List<PreviousExportRecord>
        {
            MakePrev(1, isEligible: false),
            MakePrev(2, isEligible: true),
        };

        using var wb = ExportAndOpen(current, previous);

        wb.Worksheet("Newly Eligible").RangeUsed()!.RowCount().ShouldBe(2);
        wb.Worksheet("Newly Ineligible").RangeUsed()!.RowCount().ShouldBe(2);
        wb.Worksheet("New Cards (Eligible)").RangeUsed()!.RowCount().ShouldBe(2);
    }
}
