namespace CardPool.Cli.Pipeline;

internal record struct ResolvedErrata(string Shortest, string Latest);

internal static partial class YgoCardExtensions
{
    [GeneratedRegex(@"\[\s*Pendulum Effect\s*\]|Pendulum Effect\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex HasPendulumMarkerRegex();

    [GeneratedRegex(@"\[\s*Monster Effect\s*\]|Monster Effect\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex HasMonsterMarkerRegex();

    internal static bool IsWellFormedLore(string text, string cardType) =>
        !cardType.IsPendulumEffectType()
        || (HasPendulumMarkerRegex().IsMatch(text) && HasMonsterMarkerRegex().IsMatch(text));

    internal static ResolvedErrata GetCardErrata(this YgoCard card, CardErrata? errata)
    {
        if (errata is null)
            return new ResolvedErrata(card.Desc, card.Desc);

        if (!card.Type.IsPendulumEffectType())
            return new ResolvedErrata(errata.Shortest, errata.Latest);

        var validLores = errata.AllLores
            .Where(l => IsWellFormedLore(l.Text, card.Type))
            .ToList();

        if (validLores.Count == 0)
        {
            Console.WriteLine($"Warning: malformed Yugipedia pendulum errata for '{card.Name}' — falling back to YGOProDeck description.");
            return new ResolvedErrata(card.Desc, card.Desc);
        }

        return new ResolvedErrata(
            validLores.MinBy(l => WordCounter.CountWords(l.Text))!.Text,
            validLores[^1].Text);
    }
}
