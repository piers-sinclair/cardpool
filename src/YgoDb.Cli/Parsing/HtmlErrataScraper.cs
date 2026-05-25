using AngleSharp;
using AngleSharp.Dom;

namespace YgoDb.Cli.Parsing;

/// <summary>
/// Parses Yugipedia errata HTML (action=parse endpoint) for the inspect command.
/// Uses the simpler td_to_text approach: decompose del tags only (keeps ins content).
/// </summary>
public static class HtmlErrataScraper
{
    private static readonly IBrowsingContext BrowsingContext =
        AngleSharp.BrowsingContext.New(Configuration.Default);

    public static async Task<List<string>> ParseErrataTableAsync(string html)
    {
        var doc = await BrowsingContext.OpenAsync(req => req.Content(html));

        var englishHeading = doc.QuerySelectorAll("h2, h3")
            .FirstOrDefault(h => h.TextContent.Trim().Equals("English", StringComparison.OrdinalIgnoreCase));

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
                if (!string.IsNullOrEmpty(normalised))
                    results.Add(normalised);
            }
        }
        return results;
    }
}
