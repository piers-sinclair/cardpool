using AngleSharp;
using AngleSharp.Dom;

namespace YgoDb.Cli.Parsing;

public static class HtmlErrataScraper
{
    private const string EnglishSectionHeading = "English";

    private static readonly IBrowsingContext BrowsingContext =
        AngleSharp.BrowsingContext.New(Configuration.Default);

    public static async Task<List<string>> ParseErrataTableAsync(string html)
    {
        var doc = await BrowsingContext.OpenAsync(req => req.Content(html));

        var englishHeading = doc.QuerySelectorAll("h2, h3")
            .FirstOrDefault(h => h.TextContent.Trim().Equals(EnglishSectionHeading, StringComparison.OrdinalIgnoreCase));

        IElement? table = null;
        if (englishHeading != null)
        {
            var sibling = englishHeading.NextElementSibling;
            while (sibling != null)
            {
                if (sibling.TagName.Equals("TABLE", StringComparison.OrdinalIgnoreCase)) { table = sibling; break; }
                sibling = sibling.NextElementSibling;
            }
        }

        table ??= doc.QuerySelector("tr.lores")?.Closest("table");
        if (table is null) return [];

        var results = new List<string>();
        foreach (var row in table.QuerySelectorAll("tr.lores"))
        {
            foreach (var td in row.QuerySelectorAll("td"))
            {
                foreach (var del in td.QuerySelectorAll("del").ToList())
                    del.Remove();

                var text = td.TextContent;
                var normalised = string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                if (!string.IsNullOrEmpty(normalised) && !ContainsCjk(normalised))
                    results.Add(normalised);
            }
        }
        return results;
    }

    // Hiragana U+3040–U+309F, Katakana U+30A0–U+30FF, CJK Unified Ideographs U+4E00–U+9FFF
    private static bool ContainsCjk(string text) =>
        text.Any(c => c is (>= '぀' and <= 'ゟ') or (>= '゠' and <= 'ヿ') or (>= '一' and <= '鿿'));
}
