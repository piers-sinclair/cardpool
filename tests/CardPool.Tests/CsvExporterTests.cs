namespace CardPool.Tests;

public class CsvExporterTests
{
    private static NormalizedRow MakeRow(string? materials = null) => new()
    {
        Id = 1,
        Name = "Test Card",
        Type = "Effect Monster",
        Race = "Warrior",
        Attribute = "FIRE",
        Level = 4,
        Atk = 1800,
        Def = 0,
        ShortestErrata = "shortest text",
        LatestErrata = "latest text",
        Desc = "desc text",
        Materials = materials,
        WordLimit = 20,
    };

    private static (string[] Header, string[] FirstRow) ExportAndParse(List<NormalizedRow> rows)
    {
        var path = Path.GetTempFileName();
        try
        {
            CsvExporter.Export(rows, path);
            var lines = File.ReadAllLines(path);
            return (lines[0].Split(','), lines.Length > 1 ? lines[1].Split(',') : []);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Export_WithMaterials_MaterialsColumnBeforeShortestErrata()
    {
        var (header, _) = ExportAndParse([MakeRow(materials: "1 Tuner + 1 non-Tuner")]);
        var materialsIdx = Array.IndexOf(header, "materials");
        var shortestIdx = Array.IndexOf(header, "shortest_errata");
        materialsIdx.ShouldNotBe(-1);
        shortestIdx.ShouldNotBe(-1);
        materialsIdx.ShouldBeLessThan(shortestIdx);
    }

    [Fact]
    public void Export_WithMaterials_MaterialsValueAlignedWithHeader()
    {
        var (header, data) = ExportAndParse([MakeRow(materials: "1 Tuner + 1 non-Tuner")]);
        data[Array.IndexOf(header, "materials")].ShouldBe("1 Tuner + 1 non-Tuner");
    }

    [Fact]
    public void Export_WithoutMaterials_NoMaterialsColumn()
    {
        var (header, _) = ExportAndParse([MakeRow(materials: null)]);
        Array.IndexOf(header, "materials").ShouldBe(-1);
    }

    [Fact]
    public void Export_ColumnHeaders_CardTypeNotType()
    {
        var (header, _) = ExportAndParse([MakeRow()]);
        Array.IndexOf(header, "card_type").ShouldNotBe(-1);
        Array.IndexOf(header, "type").ShouldBe(-1);
    }

    [Fact]
    public void Export_ColumnHeaders_SubtypeNotRace()
    {
        var (header, _) = ExportAndParse([MakeRow()]);
        Array.IndexOf(header, "subtype").ShouldNotBe(-1);
        Array.IndexOf(header, "race").ShouldBe(-1);
    }

    [Fact]
    public void Export_ColumnHeaders_NoDescColumn()
    {
        var (header, _) = ExportAndParse([MakeRow()]);
        Array.IndexOf(header, "desc").ShouldBe(-1);
    }

    [Fact]
    public void Export_ColumnHeaders_LatestErrataDatePresent()
    {
        var (header, _) = ExportAndParse([MakeRow()]);
        Array.IndexOf(header, "latest_errata_date").ShouldNotBe(-1);
    }

    [Fact]
    public void Export_ColumnHeaders_WordCountAfterShortestErrata()
    {
        var (header, _) = ExportAndParse([MakeRow()]);
        var shortestIdx = Array.IndexOf(header, "shortest_errata");
        var wordCountIdx = Array.IndexOf(header, "word_count");
        shortestIdx.ShouldNotBe(-1);
        wordCountIdx.ShouldNotBe(-1);
        shortestIdx.ShouldBeLessThan(wordCountIdx);
    }

    [Fact]
    public void Export_LatestErrataDateSet_FormattedAsIso()
    {
        var row = MakeRow();
        row.LatestErrataDate = new DateOnly(2002, 3, 8);
        var (header, data) = ExportAndParse([row]);
        data[Array.IndexOf(header, "latest_errata_date")].ShouldBe("2002-03-08");
    }

    [Fact]
    public void Export_LatestErrataDateNull_EmptyCell()
    {
        var row = MakeRow();
        row.LatestErrataDate = null;
        var (header, data) = ExportAndParse([row]);
        data[Array.IndexOf(header, "latest_errata_date")].ShouldBe("");
    }
}
