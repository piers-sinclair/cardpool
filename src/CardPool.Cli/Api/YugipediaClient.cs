using System.Text.Json.Nodes;

namespace CardPool.Cli.Api;

public sealed class YugipediaClient : IDisposable
{
    private const string ApiUrl = "https://yugipedia.com/api.php";
    private const string UserAgent = "cardpool-errata-fetcher/1.0";
    private const string ErrataPagePrefix = "Card Errata:";
    private const double MinIntervalMs = 100.0;

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
        var titles = string.Join("|", cardNames.Select(n => $"{ErrataPagePrefix}{n}"));
        var url = $"{ApiUrl}?action=query&prop=revisions&rvprop=content&titles={Uri.EscapeDataString(titles)}&format=json";

        var json = await ThrottledGetWithRetryAsync(url);
        var result = new Dictionary<string, CardErrata>(cardNames.Count, StringComparer.OrdinalIgnoreCase);

        var pages = json?["query"]?["pages"]?.AsObject();
        if (pages is null)
            return result;

        var pageMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, page) in pages)
        {
            if (page is null || page["missing"] is not null) continue;

            var title = page["title"]?.GetValue<string>() ?? "";
            var cardName = title.StartsWithIgnoreCase(ErrataPagePrefix)
                ? title[ErrataPagePrefix.Length..]
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
                result[name] = new(lores.MinBy(WordCounter.CountWords)!, lores[^1]);
        }

        return result;
    }

    public async Task<string?> FetchErrataHtmlAsync(string cardName)
    {
        var url = $"{ApiUrl}?action=parse&page={Uri.EscapeDataString($"{ErrataPagePrefix}{cardName}")}&prop=text&format=json";
        var json = await ThrottledGetWithRetryAsync(url);
        return json?["parse"]?["text"]?["*"]?.GetValue<string>();
    }

    private async Task<JsonNode?> ThrottledGetWithRetryAsync(string url)
    {
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
