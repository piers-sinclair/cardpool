namespace CardPool.Cli.Api;

public sealed class YgoProDeckClient(HttpClient http) : IDisposable
{
    private const string ApiUrl = "https://db.ygoprodeck.com/api/v7/cardinfo.php";
    private const string SetApiUrl = "https://db.ygoprodeck.com/api/v7/cardsets.php";
    private const string CacheFile = ".cardpool/cards_misc.json";
    private const string SetCacheFile = ".cardpool/sets.json";
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

    public async Task<Dictionary<string, string>> FetchSetDatesAsync()
    {
        List<CardSetInfo>? sets = null;

        if (File.Exists(SetCacheFile) && DateTime.UtcNow - new FileInfo(SetCacheFile).LastWriteTimeUtc < CacheMaxAge)
        {
            await using var stream = File.OpenRead(SetCacheFile);
            sets = await JsonSerializer.DeserializeAsync<List<CardSetInfo>>(stream, JsonOptions);
        }

        if (sets is null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SetCacheFile)!);
            using var response = await http.GetAsync(SetApiUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(SetCacheFile, bytes);

            sets = JsonSerializer.Deserialize<List<CardSetInfo>>(bytes, JsonOptions) ?? [];
        }

        return sets
            .Where(s => s.TcgDate is not null)
            .ToDictionary(s => s.SetName, s => s.TcgDate!, StringComparer.OrdinalIgnoreCase);
    }

    private sealed record CardsWrapper([property: JsonPropertyName("data")] List<YgoCard>? Data);

    private sealed record CardSetInfo(
        [property: JsonPropertyName("set_name")] string SetName,
        [property: JsonPropertyName("tcg_date")] string? TcgDate);

    public void Dispose() { }
}
