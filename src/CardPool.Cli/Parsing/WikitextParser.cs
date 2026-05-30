namespace CardPool.Cli.Parsing;

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

    [GeneratedRegex(@"\|\s*cap(\d+)\s*=\s*([^|]+?)(?=\s*\||\}\}|\z)", RegexOptions.Singleline)]
    private static partial Regex CapFieldRegex();

    [GeneratedRegex(@"\[\[([^\]]+)\]\]")]
    private static partial Regex WikiLinkTargetRegex();

    private static readonly IBrowsingContext BrowsingContext =
        AngleSharp.BrowsingContext.New(Configuration.Default);

    public static async Task<List<LoreEntry>> ExtractEnglishLoresAsync(string wikitext)
    {
        var sectionMatch = EnglishSectionRegex().Match(wikitext);
        if (!sectionMatch.Success) return [];

        var section = sectionMatch.Groups[1].Value;

        var lores = LoreFieldRegex().Matches(section)
            .Select(m => (index: int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), raw: m.Groups[2].Value))
            .OrderBy(x => x.index)
            .ToList();

        var caps = CapFieldRegex().Matches(section)
            .ToDictionary(
                m => int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture),
                m => ExtractSetName(m.Groups[2].Value));

        var results = new List<LoreEntry>(lores.Count);
        foreach (var (index, raw) in lores)
        {
            var text = await LoreFullAsync(raw);
            if (!string.IsNullOrWhiteSpace(text) && !text.ContainsJapanese())
                results.Add(new LoreEntry(text, caps.GetValueOrDefault(index)));
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
        return text.NormalizeWhitespace();
    }

    private static string? ExtractSetName(string cap)
    {
        var matches = WikiLinkTargetRegex().Matches(cap);
        if (matches.Count < 2) return null;
        var raw = matches[^1].Groups[1].Value;
        var pipeIdx = raw.IndexOf('|');
        return (pipeIdx >= 0 ? raw[..pipeIdx] : raw).Trim();
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
}
