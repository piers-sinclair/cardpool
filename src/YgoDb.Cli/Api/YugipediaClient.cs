using System.Text.Json;
using System.Text.Json.Nodes;
using YgoDb.Cli.Parsing;

namespace YgoDb.Cli.Api;

public sealed class YugipediaClient : IDisposable
{
    private const string ApiUrl = "https://yugipedia.com/api.php";
    private const string UserAgent = "ygodb-errata-fetcher/1.0";
    private const double MinIntervalMs = 100.0;

    private readonly HttpClient _http;
    private readonly SemaphoreSlim _rateLock = new(1, 1);
    private DateTime _lastRequest = DateTime.MinValue;

    public YugipediaClient(HttpClient http)
    {
        _http = http;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
    }

    /// <summary>
    /// Fetches errata for up to 50 card names in a single API call.
    /// Returns a dict of card name → (shortestErrata, latestErrata), or null values if no page exists.
    /// </summary>
    public async Task<Dictionary<string, (string? Shortest, string? Latest)>> FetchErrataAsync(
        IReadOnlyList<string> cardNames)
    {
        var titles = string.Join("|", cardNames.Select(n => $"Card Errata:{n}"));
        var url = $"{ApiUrl}?action=query&prop=revisions&rvprop=content&titles={Uri.EscapeDataString(titles)}&format=json";

        var json = await ThrottledGetAsync(url);
        var result = new Dictionary<string, (string?, string?)>(cardNames.Count, StringComparer.OrdinalIgnoreCase);

        var pages = json?["query"]?["pages"]?.AsObject();
        if (pages is null)
        {
            foreach (var n in cardNames) result[n] = (null, null);
            return result;
        }

        // Build lookup from page title → lores
        var pageMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, page) in pages)
        {
            if (page is null || page["missing"] is not null) continue;

            var title = page["title"]?.GetValue<string>() ?? "";
            var cardName = title.StartsWith("Card Errata:", StringComparison.OrdinalIgnoreCase)
                ? title["Card Errata:".Length..]
                : title;

            var wikitext = page["revisions"]?[0]?["*"]?.GetValue<string>();
            if (wikitext is null) continue;

            var lores = await WikitextParser.ExtractEnglishLoresAsync(wikitext);
            if (lores.Count > 0)
                pageMap[cardName] = lores;
        }

        foreach (var name in cardNames)
        {
            if (pageMap.TryGetValue(name, out var lores))
            {
                var shortest = lores.MinBy(t => t.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length);
                var latest = lores[^1];
                result[name] = (shortest, latest);
            }
            else
            {
                result[name] = (null, null);
            }
        }

        return result;
    }

    /// <summary>
    /// Fetches the rendered HTML errata table for a single card (used by inspect command).
    /// </summary>
    public async Task<string?> FetchErrataHtmlAsync(string cardName)
    {
        var url = $"{ApiUrl}?action=parse&page={Uri.EscapeDataString($"Card Errata:{cardName}")}&prop=text&format=json";
        var json = await ThrottledGetAsync(url);
        return json?["parse"]?["text"]?["*"]?.GetValue<string>();
    }

    private async Task<JsonNode?> ThrottledGetAsync(string url)
    {
        // Retry up to 3 times for transient Yugipedia DB errors (returned as HTTP 200 with error JSON)
        for (var attempt = 0; attempt < 3; attempt++)
        {
            await _rateLock.WaitAsync();
            JsonNode? json;
            try
            {
                var wait = MinIntervalMs - (DateTime.UtcNow - _lastRequest).TotalMilliseconds;
                if (wait > 0) await Task.Delay((int)wait);
                _lastRequest = DateTime.UtcNow;

                using var response = await _http.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                json = JsonNode.Parse(content);
            }
            finally
            {
                _rateLock.Release();
            }

            if (json?["error"] is null)
                return json;

            // Transient error — back off before retrying
            if (attempt < 2)
                await Task.Delay(1000 * (1 << attempt));
        }

        return null;
    }

    public void Dispose()
    {
        _rateLock.Dispose();
    }
}
