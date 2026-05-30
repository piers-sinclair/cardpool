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
        int wordLimit,
        bool stripMaterials = false)
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
            EligibleSince = ComputeEligibleSince(card, errata, resolved, wordLimit, stripMaterials),
            WordLimit = wordLimit,
            ImageUrl = card.CardImages?[0].ImageUrl
        };
    }

    private static DateOnly? ComputeEligibleSince(YgoCard card, CardErrata? errata, ResolvedErrata resolved, int wordLimit, bool stripMaterials)
    {
        if (errata is null)
            return ParseDate(card.TcgDate);

        var firstEligibleLore = errata.AllLores
            .FirstOrDefault(l => CountWords(l.Text, card.Type, stripMaterials) <= wordLimit);

        if (firstEligibleLore is not null)
            return ParseDate(firstEligibleLore.Date ?? card.TcgDate);

        // firstEligibleLore is only null when resolved.Shortest is card.Desc due to the
        // pendulum fallback — that text is not in AllLores. Use tcg_date if it's eligible,
        // null if the card is ineligible entirely.
        return CountWords(resolved.Shortest, card.Type, stripMaterials) <= wordLimit
            ? ParseDate(card.TcgDate)
            : null;
    }

    private static int CountWords(string text, string type, bool stripMaterials)
    {
        var effective = stripMaterials ? MaterialStripper.StripMaterialLine(text) : text;
        return WordCounter.CountEffectiveWords(effective, type);
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
