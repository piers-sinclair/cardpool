namespace YgoDb.Cli.Models;

public class NormalizedRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string? Race { get; set; }
    public string? Attribute { get; set; }
    public int? Level { get; set; }
    public int? Atk { get; set; }
    public int? Def { get; set; }
    public int? Scale { get; set; }
    public int? LinkVal { get; set; }
    public string? LinkMarkers { get; set; }
    public string? Archetype { get; set; }
    public string Desc { get; set; } = "";
    public string? Materials { get; set; }
    public string ShortestErrata { get; set; } = "";
    public string LatestErrata { get; set; } = "";
    public int WordCount { get; set; }
    public bool IsEligible { get; set; }
    public string? SetName { get; set; }
    public string? SetCode { get; set; }
    public string? SetRarity { get; set; }
    public string? BanTcg { get; set; }
    public string? BanOcg { get; set; }
    public string? ImageUrl { get; set; }

    public NormalizedRow Clone() => (NormalizedRow)MemberwiseClone();

    public static readonly string[] OutputColumns =
    [
        "name", "type", "attribute", "race", "level", "atk", "def",
        "word_count", "shortest_errata",
        "scale", "linkval", "linkmarkers", "archetype",
        "set_name", "set_code", "set_rarity",
        "ban_tcg", "ban_ocg",
        "latest_errata", "desc", "id", "image_url", "is_eligible"
    ];

    public static readonly string[] OutputColumnsWithMaterials =
    [
        "name", "type", "attribute", "race", "level", "atk", "def",
        "word_count", "shortest_errata", "materials",
        "scale", "linkval", "linkmarkers", "archetype",
        "set_name", "set_code", "set_rarity",
        "ban_tcg", "ban_ocg",
        "latest_errata", "desc", "id", "image_url", "is_eligible"
    ];
}
