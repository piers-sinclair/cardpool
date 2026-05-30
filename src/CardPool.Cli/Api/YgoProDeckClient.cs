namespace CardPool.Cli.Api;

public sealed class YgoProDeckClient(HttpClient http) : IDisposable
{
    private const string ApiUrl = "https://db.ygoprodeck.com/api/v7/cardinfo.php";
    private const string CacheFile = ".cardpool/cards_misc.json";
    private static readonly TimeSpan CacheMaxAge = TimeSpan.FromHours(24);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<List<YgoCard>> FetchAllCardsAsync()
    {
        if (File.Exists(CacheFile) && DateTime.UtcNow - new FileInfo(CacheFile).LastWriteTimeUtc < CacheMaxAge)
        {
            await using var stream = File.OpenRead(CacheFile);
            var cached = await JsonSerializer.DeserializeAsync<CardsWrapper>(stream, JsonOptions);
            if (cached?.Data is { Count: > 0 } cachedData)
                return cachedData;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(CacheFile)!);
        using var response = await http.GetAsync($"{ApiUrl}?misc=yes", HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync();
        await File.WriteAllBytesAsync(CacheFile, bytes);

        var wrapper = JsonSerializer.Deserialize<CardsWrapper>(bytes, JsonOptions);
        return wrapper?.Data ?? [];
    }

    public async Task<YgoCard?> FetchCardByNameAsync(string cardName)
    {
        var url = $"{ApiUrl}?name={Uri.EscapeDataString(cardName)}&misc=yes";
        using var response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;

        var wrapper = await JsonSerializer.DeserializeAsync<CardsWrapper>(
            await response.Content.ReadAsStreamAsync(), JsonOptions);

        return wrapper?.Data?.FirstOrDefault();
    }

    private sealed record CardsWrapper([property: JsonPropertyName("data")] List<YgoCard>? Data);

    public void Dispose() { }
}
