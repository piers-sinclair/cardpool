using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;

namespace YgoDb.Cli.Parsing;

public static partial class WikitextParser
{
    [GeneratedRegex(@"'{2,3}")]
    private static partial Regex BoldItalicRegex();

    [GeneratedRegex(@"\[\[File:[^\]]+\]\]")]
    private static partial Regex FileLinksRegex();

    [GeneratedRegex(@"\[\[(?:[^\]|]+\|)?([^\]]+)\]\]")]
    private static partial Regex WikiLinksRegex();

    [GeneratedRegex(@"\{\{[^}]+\}\}")]
    private static partial Regex TemplatesRegex();

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex BrTagRegex();

    [GeneratedRegex(@"==\s*English\s*==\s*([\s\S]*?)(?:\n==\s|\z)")]
    private static partial Regex EnglishSectionRegex();

    [GeneratedRegex(@"\|\s*lore(\d+)\s*=([^|]+?)(?=\s*\||\}\}|\z)", RegexOptions.Singleline)]
    private static partial Regex LoreFieldRegex();

    private static readonly IBrowsingContext BrowsingContext =
        AngleSharp.BrowsingContext.New(Configuration.Default);

    public static async Task<List<string>> ExtractEnglishLoresAsync(string wikitext)
    {
        var sectionMatch = EnglishSectionRegex().Match(wikitext);
        if (!sectionMatch.Success) return [];

        var section = sectionMatch.Groups[1].Value;

        var lores = LoreFieldRegex().Matches(section)
            .Select(m => (index: int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), raw: m.Groups[2].Value))
            .OrderBy(x => x.index)
            .ToList();

        var results = new List<string>(lores.Count);
        foreach (var (_, raw) in lores)
        {
            var text = await LoreFullAsync(raw);
            if (!string.IsNullOrWhiteSpace(text) && !ContainsCjk(text))
                results.Add(text);
        }
        return results;
    }

    public static async Task<string> LoreFullAsync(string raw)
    {
        var preamble = ApplyPreamble(raw);
        var doc = await BrowsingContext.OpenAsync(req => req.Content(preamble));

        foreach (var tag in doc.QuerySelectorAll("del, ins").ToList())
            tag.ReplaceWith(tag.ChildNodes.ToArray());

        var text = doc.Body?.TextContent ?? "";
        return NormaliseWhitespace(text);
    }

    private static string ApplyPreamble(string raw)
    {
        var text = BoldItalicRegex().Replace(raw, "");
        text = FileLinksRegex().Replace(text, "");
        text = WikiLinksRegex().Replace(text, "$1");
        text = TemplatesRegex().Replace(text, "");
        text = BrTagRegex().Replace(text, " ");
        return text;
    }

    private static string NormaliseWhitespace(string text) =>
        string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    // Hiragana U+3040–U+309F, Katakana U+30A0–U+30FF, CJK Unified Ideographs U+4E00–U+9FFF
    private static bool ContainsCjk(string text) =>
        text.Any(c => c is (>= '぀' and <= 'ゟ') or (>= '゠' and <= 'ヿ') or (>= '一' and <= '鿿'));
}
