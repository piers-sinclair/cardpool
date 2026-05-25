using System.Text.RegularExpressions;
using YgoDb.Cli.Models;
using YgoDb.Cli.WordCount;

namespace YgoDb.Cli.Pipeline;

public static partial class CardNormalizer
{
    [GeneratedRegex(@"\[\s*Pendulum Effect\s*\]|Pendulum Effect\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex HasPendulumMarkerRegex();

    [GeneratedRegex(@"\[\s*Monster Effect\s*\]|Monster Effect\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex HasMonsterMarkerRegex();

    public static bool NeedsErrataLookup(YgoCard card, int wordLimit)
    {
        if (card.Type.Equals("Normal Monster", StringComparison.OrdinalIgnoreCase))
            return false;

        return WordCounter.CountEffectiveWords(card.Desc, card.Type) > wordLimit;
    }

    public static NormalizedRow Normalize(
        YgoCard card,
        string? shortestErrata,
        string? latestErrata,
        int wordLimit)
    {
        // Resolve errata — use API desc as fallback when no Yugipedia page
        var resolvedShortest = ResolveErrata(card, shortestErrata);
        var resolvedLatest = ResolveErrata(card, latestErrata);

        // Incomplete Pendulum errata detection: if only one section marker present, fall back to desc
        var wordSource = resolvedShortest;
        // Match Python: "Pendulum" in type and "Normal" not in type — catches Pendulum Tuner Effect Monster etc.
        if (card.Type.Contains("Pendulum", StringComparison.OrdinalIgnoreCase)
            && !card.Type.Contains("Normal", StringComparison.OrdinalIgnoreCase)
            && shortestErrata is not null)
        {
            bool hasPend = HasPendulumMarkerRegex().IsMatch(resolvedShortest);
            bool hasMons = HasMonsterMarkerRegex().IsMatch(resolvedShortest);
            if (hasPend != hasMons)
                wordSource = card.Desc;
        }

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

    private static string ResolveErrata(YgoCard card, string? errata) =>
        string.IsNullOrEmpty(errata) ? card.Desc : errata;
}
