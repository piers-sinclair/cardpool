namespace CardPool.Tests;

public class PreviousExportReaderTests
{
    private static List<PreviousExportRecord> ReadFromString(string csvContent)
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, csvContent, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return PreviousExportReader.Read(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Read_ValidCsv_ReturnsCorrectRecords()
    {
        var csv = "id,name,is_eligible\n1,Dark Magician,True\n2,Blue-Eyes White Dragon,False";

        var records = ReadFromString(csv);

        records.Count.ShouldBe(2);
        records[0].Id.ShouldBe(1);
        records[0].Name.ShouldBe("Dark Magician");
        records[0].IsEligible.ShouldBeTrue();
        records[1].Id.ShouldBe(2);
        records[1].Name.ShouldBe("Blue-Eyes White Dragon");
        records[1].IsEligible.ShouldBeFalse();
    }

    [Fact]
    public void Read_CsvWithExtraColumns_IgnoresExtraColumns()
    {
        var csv = "id,name,card_type,word_count,is_eligible\n42,Test Card,Effect Monster,15,True";

        var records = ReadFromString(csv);

        records.Count.ShouldBe(1);
        records[0].Id.ShouldBe(42);
        records[0].Name.ShouldBe("Test Card");
        records[0].IsEligible.ShouldBeTrue();
    }

    [Fact]
    public void Read_EmptyCsv_ReturnsEmptyList()
    {
        var csv = "id,name,is_eligible\n";

        var records = ReadFromString(csv);

        records.ShouldBeEmpty();
    }

    [Fact]
    public void Read_CsvWithMaterialsColumn_DoesNotThrow()
    {
        var csv = "id,name,materials,shortest_errata,is_eligible\n10,Stardust Dragon,1 Tuner + 1 non-Tuner,short text,True";

        var records = ReadFromString(csv);

        records.Count.ShouldBe(1);
        records[0].Id.ShouldBe(10);
        records[0].IsEligible.ShouldBeTrue();
    }
}
