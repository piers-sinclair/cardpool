using System.Text.RegularExpressions;
using CardPool.Cli.Models;
using CardPool.Cli.WordCount;

namespace CardPool.Cli.Pipeline;

public static partial class CardNormalizer
{
    [GeneratedRegex(@"\[\s*Pendulum Effect\s*\]|Pendulum Effect\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex HasPendulumMarkerRegex();

    [GeneratedRegex(@"\[\s*Monster Effect\s*\]|Monster Effect\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex HasMonsterMarkerRegex();

    public static bool NeedsErrataLookup(YgoCard card, int wordLimit)
    {
        if (card.Type.IsPureNormalMonster())
            return false;

        return WordCounter.CountEffectiveWords(card.Desc, card.Type) > wordLimit;
    }

    public static NormalizedRow Normalize(
        YgoCard card,
        string? shortestErrata,
        string? latestErrata,
        int wordLimit)
    {
        var resolvedShortest = ResolveErrata(card, shortestErrata);
        var resolvedLatest = ResolveErrata(card, latestErrata);

        var wordSource = ResolveWordSource(card, shortestErrata, resolvedShortest);
        var wordCount = WordCounter.CountEffectiveWords(wordSource, card.Type);

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
            ShortestErrata = resolvedShortest,
            LatestErrata = resolvedLatest,
            WordCount = wordCount,
            IsEligible = wordCount <= wordLimit,
            SetName = card.CardSets?[0].SetName,
            SetCode = card.CardSets?[0].SetCode,
            SetRarity = card.CardSets?[0].SetRarity,
            BanTcg = card.BanlistInfo?.BanTcg,
            BanOcg = card.BanlistInfo?.BanOcg,
            ImageUrl = card.CardImages?[0].ImageUrl
        };
    }

    private static string ResolveWordSource(YgoCard card, string? shortestErrata, string resolvedShortest)
    {
        if (!card.Type.IsPendulumEffectType() || shortestErrata is null)
            return resolvedShortest;

        var hasPend = HasPendulumMarkerRegex().IsMatch(resolvedShortest);
        var hasMons = HasMonsterMarkerRegex().IsMatch(resolvedShortest);
        return hasPend != hasMons ? card.Desc : resolvedShortest;
    }

    private static string ResolveErrata(YgoCard card, string? errata) =>
        string.IsNullOrEmpty(errata) ? card.Desc : errata;
}
