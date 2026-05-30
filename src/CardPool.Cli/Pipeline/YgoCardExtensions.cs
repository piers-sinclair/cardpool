namespace CardPool.Cli.Pipeline;

internal record struct ResolvedErrata(string Shortest, string Latest);

internal static partial class YgoCardExtensions
{
    [GeneratedRegex(@"\[\s*Pendulum Effect\s*\]|Pendulum Effect\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex HasPendulumMarkerRegex();

    [GeneratedRegex(@"\[\s*Monster Effect\s*\]|Monster Effect\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex HasMonsterMarkerRegex();

    internal static ResolvedErrata GetCardErrata(this YgoCard card, CardErrata? errata)
    {
        if (errata is null)
            return new ResolvedErrata(card.Desc, card.Desc);

        var resolvedShortest = errata.Shortest;

        if (card.Type.IsPendulumEffectType())
        {
            var hasPend = HasPendulumMarkerRegex().IsMatch(resolvedShortest);
            var hasMons = HasMonsterMarkerRegex().IsMatch(resolvedShortest);
            if (hasPend != hasMons)
            {
                Console.WriteLine($"Warning: malformed Yugipedia pendulum errata for '{card.Name}' — falling back to YGOProDeck description.");
                resolvedShortest = card.Desc;
            }
        }

        return new ResolvedErrata(resolvedShortest, errata.Latest);
    }
}
