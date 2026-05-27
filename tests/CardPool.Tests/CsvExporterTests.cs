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
}
