using System.Globalization;

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
        CardErrata? errata,
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
            LatestErrataDate = ParseDate(resolved.LatestDate),
            WordLimit = wordLimit,
            ImageUrl = card.CardImages?[0].ImageUrl
        };
    }

    private static DateOnly? ParseDate(string? raw)
    {
        if (raw is null) return null;
        var trimmed = raw.Trim();
        if (DateOnly.TryParseExact(trimmed, "MMMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        if (DateOnly.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
            return d;
        return null;
    }
}
