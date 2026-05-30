namespace CardPool.Cli.Models;

public record ErrataLore(string Text, string? Date);
public record CardErrata(string Shortest, string Latest, IReadOnlyList<ErrataLore> AllLores);
