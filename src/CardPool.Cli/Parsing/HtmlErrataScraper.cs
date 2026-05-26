using AngleSharp;
using AngleSharp.Dom;

namespace CardPool.Cli.Parsing;

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
                if (!string.IsNullOrEmpty(normalised) && !ContainsJapaneseCharacters(normalised))
                    results.Add(normalised);
            }
        }
        return results;
    }

    private static bool ContainsJapaneseCharacters(string text) =>
        text.Any(c => IsHiragana(c) || IsKatakana(c) || IsKanji(c));

    private static bool IsHiragana(char c) => c is >= '぀' and <= 'ゟ';
    private static bool IsKatakana(char c) => c is >= '゠' and <= 'ヿ';
    private static bool IsKanji(char c) => c is >= '一' and <= '鿿';
}
