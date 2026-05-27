namespace CardPool.Cli.Pipeline;

public static class CardNormalizer
{
    public static bool NeedsErrataLookup(YgoCard card, int wordLimit)
    {
        if (card.Type.IsPureNormalMonster())
            return false;

        return WordCounter.CountEffectiveWords(card.Desc, card.Type) > wordLimit;
    }

    public static NormalizedRow Normalize(
        YgoCard card,
        CardErrata errata,
        int wordLimit)
    {
        var resolved = card.GetCardErrata(errata);

        return new NormalizedRow
        {
            Id = card.Id,
            Name = card.Name,
            Type = card.Type,
            Race = card.Race,
            Attribute = card.Attribute,
            Level = card.Level,
            Atk = card.Atk,
            Def = card.Def,
            Scale = card.Scale,
            LinkVal = card.LinkVal,
            LinkMarkers = card.LinkMarkers is { Length: > 0 }
                ? string.Join(",", card.LinkMarkers)
                : null,
            Archetype = card.Archetype,
            Desc = card.Desc,
            ShortestErrata = resolved.Shortest,
            LatestErrata = resolved.Latest,
            WordLimit = wordLimit,
            SetName = card.CardSets?[0].SetName,
            SetCode = card.CardSets?[0].SetCode,
            SetRarity = card.CardSets?[0].SetRarity,
            BanTcg = card.BanlistInfo?.BanTcg,
            BanOcg = card.BanlistInfo?.BanOcg,
            ImageUrl = card.CardImages?[0].ImageUrl
        };
    }
}
