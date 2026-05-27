namespace CardPool.Cli.Pipeline;

internal record struct ResolvedErrata(string Shortest, string Latest);

internal static partial class YgoCardExtensions
{
    [GeneratedRegex(@"\[\s*Pendulum Effect\s*\]|Pendulum Effect\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex HasPendulumMarkerRegex();

    [GeneratedRegex(@"\[\s*Monster Effect\s*\]|Monster Effect\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex HasMonsterMarkerRegex();

    internal static ResolvedErrata GetCardErrata(this YgoCard card, CardErrata errata)
    {
        var resolvedShortest = string.IsNullOrEmpty(errata.Shortest) ? card.Desc : errata.Shortest;
        var resolvedLatest = string.IsNullOrEmpty(errata.Latest) ? card.Desc : errata.Latest;

        if (card.Type.IsPendulumEffectType() && errata.Shortest is not null)
        {
            var hasPend = HasPendulumMarkerRegex().IsMatch(resolvedShortest);
            var hasMons = HasMonsterMarkerRegex().IsMatch(resolvedShortest);
            if (hasPend != hasMons)
            {
                Console.WriteLine($"Warning: malformed Yugipedia pendulum errata for '{card.Name}' — falling back to YGOProDeck description.");
                resolvedShortest = card.Desc;
            }
        }

        return new ResolvedErrata(resolvedShortest, resolvedLatest);
    }
}
