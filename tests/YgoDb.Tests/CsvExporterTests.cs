using YgoDb.Cli.Export;

namespace YgoDb.Tests;

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
        WordCount = 10,
        ShortestErrata = "shortest text",
        LatestErrata = "latest text",
        Desc = "desc text",
        Materials = materials,
        IsEligible = true,
    };

    [Fact]
    public void Export_WithMaterials_MaterialsColumnBeforeShortestErrata()
    {
        var rows = new List<NormalizedRow> { MakeRow(materials: "1 Tuner + 1 non-Tuner") };
        var path = Path.GetTempFileName();
        try
        {
            CsvExporter.Export(rows, path);
            var header = File.ReadLines(path).First().Split(',');
            var materialsIdx = Array.IndexOf(header, "materials");
            var shortestIdx = Array.IndexOf(header, "shortest_errata");
            materialsIdx.ShouldBeGreaterThan(-1);
            shortestIdx.ShouldBeGreaterThan(-1);
            materialsIdx.ShouldBeLessThan(shortestIdx);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Export_WithMaterials_MaterialsValueAlignedWithHeader()
    {
        var rows = new List<NormalizedRow> { MakeRow(materials: "1 Tuner + 1 non-Tuner") };
        var path = Path.GetTempFileName();
        try
        {
            CsvExporter.Export(rows, path);
            var lines = File.ReadAllLines(path);
            var header = lines[0].Split(',');
            var data = lines[1].Split(',');
            var materialsIdx = Array.IndexOf(header, "materials");
            data[materialsIdx].ShouldBe("1 Tuner + 1 non-Tuner");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Export_WithoutMaterials_NoMaterialsColumn()
    {
        var rows = new List<NormalizedRow> { MakeRow(materials: null) };
        var path = Path.GetTempFileName();
        try
        {
            CsvExporter.Export(rows, path);
            var header = File.ReadLines(path).First().Split(',');
            Array.IndexOf(header, "materials").ShouldBe(-1);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
