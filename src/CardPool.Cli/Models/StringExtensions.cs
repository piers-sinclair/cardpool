namespace CardPool.Cli.Models;

internal static class StringExtensions
{
    internal static bool ContainsIgnoreCase(this string source, string value) =>
        source.Contains(value, StringComparison.OrdinalIgnoreCase);

    internal static bool EqualsIgnoreCase(this string source, string value) =>
        source.Equals(value, StringComparison.OrdinalIgnoreCase);

    internal static bool StartsWithIgnoreCase(this string source, string value) =>
        source.StartsWith(value, StringComparison.OrdinalIgnoreCase);

    internal static bool ContainsIgnoreCase(this IEnumerable<string> source, string value) =>
        source.Contains(value, StringComparer.OrdinalIgnoreCase);

    internal static string NormalizeWhitespace(this string text) =>
        string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    internal static bool ContainsJapanese(this string text) =>
        text.Any(c => IsHiragana(c) || IsKatakana(c) || IsKanji(c));

    private static bool IsHiragana(char c) => c is >= '぀' and <= 'ゟ';
    private static bool IsKatakana(char c) => c is >= '゠' and <= 'ヿ';
    private static bool IsKanji(char c) => c is >= '一' and <= '鿿';
}
