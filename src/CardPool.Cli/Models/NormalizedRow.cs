namespace CardPool.Cli.Models;

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
    public DateOnly? EligibleSince { get; set; }
    public int WordLimit { get; set; }
    public int WordCount => WordCounter.CountEffectiveWords(ShortestErrata, Type);
    public bool IsEligible => WordCount <= WordLimit;
    public string? ImageUrl { get; set; }

    public NormalizedRow Clone() => (NormalizedRow)MemberwiseClone();

    public static readonly string[] OutputColumns =
    [
        "name", "card_type", "attribute", "subtype", "level", "atk", "def",
        "shortest_errata", "eligible_since", "word_count", "archetype",
        "scale", "linkval", "linkmarkers",
        "latest_errata", "id", "image_url", "is_eligible"
    ];

    public static readonly string[] OutputColumnsWithMaterials =
    [
        "name", "card_type", "attribute", "subtype", "level", "atk", "def",
        "materials", "shortest_errata", "eligible_since", "word_count", "archetype",
        "scale", "linkval", "linkmarkers",
        "latest_errata", "id", "image_url", "is_eligible"
    ];
}
