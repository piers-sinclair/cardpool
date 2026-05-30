using System.Text.Json.Nodes;

namespace CardPool.Cli.Api;

public sealed class YugipediaClient : IDisposable
{
    private const string ApiUrl = "https://yugipedia.com/api.php";
    private const string UserAgent = "cardpool-errata-fetcher/1.0";
    private const string ErrataPagePrefix = "Card Errata:";
    private const double MinIntervalMs = 100.0;
    private const int MaxRetries = 3;
    private const int RetryBaseDelayMs = 1000;

    private const string JsonQuery = "query";
    private const string JsonPages = "pages";
    private const string JsonParse = "parse";
    private const string JsonText = "text";
    private const string JsonTitle = "title";
    private const string JsonMissing = "missing";
    private const string JsonRevisions = "revisions";
    private const string JsonWikitext = "*";
    private const string JsonError = "error";

    private readonly HttpClient _http;
    private readonly SemaphoreSlim _rateLock = new(1, 1);
    private DateTime _lastRequest = DateTime.MinValue;

    public YugipediaClient(HttpClient http)
    {
        _http = http;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
    }

    public async Task<Dictionary<string, CardErrata>> FetchErrataAsync(
        IReadOnlyList<string> cardNames)
    {
        var json = await ThrottledGetWithRetryAsync(BuildErrataQueryUrl(cardNames));
        var result = new Dictionary<string, CardErrata>(cardNames.Count, StringComparer.OrdinalIgnoreCase);

        var pages = json?[JsonQuery]?[JsonPages]?.AsObject();
        if (pages is null)
            return result;

        var pageMap = await ParsePageMapAsync(pages);
        foreach (var name in cardNames)
        {
            if (pageMap.TryGetValue(name, out var lores))
                result[name] = new(lores.MinBy(WordCounter.CountWords)!, lores[^1]);
        }

        return result;
    }

    public async Task<string?> FetchErrataHtmlAsync(string cardName)
    {
        var json = await ThrottledGetWithRetryAsync(BuildHtmlUrl(cardName));
        return json?[JsonParse]?[JsonText]?[JsonWikitext]?.GetValue<string>();
    }

    private static string BuildErrataQueryUrl(IReadOnlyList<string> cardNames)
    {
        var titles = string.Join("|", cardNames.Select(n => $"{ErrataPagePrefix}{n}"));
        return $"{ApiUrl}?action=query&prop=revisions&rvprop=content&titles={Uri.EscapeDataString(titles)}&format=json";
    }

    private static string BuildHtmlUrl(string cardName) =>
        $"{ApiUrl}?action=parse&page={Uri.EscapeDataString($"{ErrataPagePrefix}{cardName}")}&prop=text&format=json";

    private static async Task<Dictionary<string, List<string>>> ParsePageMapAsync(JsonObject pages)
    {
        var pageMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, page) in pages)
        {
            if (page is null || page[JsonMissing] is not null) continue;

            var title = page[JsonTitle]?.GetValue<string>() ?? "";
            var cardName = title.StartsWithIgnoreCase(ErrataPagePrefix)
                ? title[ErrataPagePrefix.Length..]
                : title;

            var wikitext = ExtractWikitext(page);
            if (wikitext is null) continue;

            var lores = await WikitextParser.ExtractEnglishLoresAsync(wikitext);
            if (lores.Count > 0)
                pageMap[cardName] = lores;
        }
        return pageMap;
    }

    private static string? ExtractWikitext(JsonNode page) =>
        page[JsonRevisions]?[0]?[JsonWikitext]?.GetValue<string>();

    private async Task<JsonNode?> ThrottledGetWithRetryAsync(string url)
    {
        for (var attempt = 0; attempt < MaxRetries; attempt++)
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

            if (json?[JsonError] is null)
                return json;

            if (attempt < MaxRetries - 1)
                await Task.Delay(RetryBaseDelayMs * (1 << attempt));
        }

        return null;
    }

    public void Dispose()
    {
        _rateLock.Dispose();
    }
}
