using System.Text.Json;
using System.Text.Json.Serialization;
using YgoDb.Cli.Models;

namespace YgoDb.Cli.Api;

public sealed class YgoProDeckClient(HttpClient http) : IDisposable
{
    private const string ApiUrl = "https://db.ygoprodeck.com/api/v7/cardinfo.php";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<List<YgoCard>> FetchAllCardsAsync()
    {
        using var response = await http.GetAsync(ApiUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var wrapper = await JsonSerializer.DeserializeAsync<CardsWrapper>(
            await response.Content.ReadAsStreamAsync(), JsonOptions);

        return wrapper?.Data ?? [];
    }

    public async Task<YgoCard?> FetchCardByNameAsync(string cardName)
    {
        var url = $"{ApiUrl}?name={Uri.EscapeDataString(cardName)}";
        using var response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;

        var wrapper = await JsonSerializer.DeserializeAsync<CardsWrapper>(
            await response.Content.ReadAsStreamAsync(), JsonOptions);

        return wrapper?.Data?.FirstOrDefault();
    }

    private sealed record CardsWrapper([property: JsonPropertyName("data")] List<YgoCard>? Data);

    public void Dispose() { }
}
