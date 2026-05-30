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
            .FirstOrDefault(h => h.TextContent.Trim().EqualsIgnoreCase(EnglishSectionHeading));

        IElement? table = null;
        if (englishHeading != null)
        {
            var sibling = englishHeading.NextElementSibling;
            while (sibling != null)
            {
                if (sibling.TagName.EqualsIgnoreCase("TABLE")) { table = sibling; break; }
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

                var normalised = td.TextContent.NormalizeWhitespace();
                if (!string.IsNullOrEmpty(normalised) && !normalised.ContainsJapanese())
                    results.Add(normalised);
            }
        }
        return results;
    }
}
