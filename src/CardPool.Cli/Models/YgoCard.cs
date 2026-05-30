namespace CardPool.Cli.Models;

public record YgoCard(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("race")] string? Race,
    [property: JsonPropertyName("attribute")] string? Attribute,
    [property: JsonPropertyName("level")] int? Level,
    [property: JsonPropertyName("atk")] int? Atk,
    [property: JsonPropertyName("def")] int? Def,
    [property: JsonPropertyName("scale")] int? Scale,
    [property: JsonPropertyName("linkval")] int? LinkVal,
    [property: JsonPropertyName("linkmarkers")] string[]? LinkMarkers,
    [property: JsonPropertyName("archetype")] string? Archetype,
    [property: JsonPropertyName("desc")] string Desc,
    [property: JsonPropertyName("card_sets")] CardSet[]? CardSets,
    [property: JsonPropertyName("banlist_info")] BanlistInfo? BanlistInfo,
    [property: JsonPropertyName("card_images")] CardImage[]? CardImages,
    [property: JsonPropertyName("misc_info")] MiscInfo[]? MiscInfo
)
{
    public string? TcgDate => MiscInfo is { Length: > 0 } ? MiscInfo[0].TcgDate : null;
    public bool IsTcgLegal => TcgDate is not null;
}

public record CardSet(
    [property: JsonPropertyName("set_name")] string SetName,
    [property: JsonPropertyName("set_code")] string SetCode,
    [property: JsonPropertyName("set_rarity")] string SetRarity
);

public record BanlistInfo(
    [property: JsonPropertyName("ban_tcg")] string? BanTcg,
    [property: JsonPropertyName("ban_ocg")] string? BanOcg
);

public record CardImage(
    [property: JsonPropertyName("image_url")] string ImageUrl
);

public record MiscInfo(
    [property: JsonPropertyName("tcg_date")] string? TcgDate
);
