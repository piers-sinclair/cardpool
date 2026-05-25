using YgoDb.Cli.Api;
using YgoDb.Cli.Export;
using YgoDb.Cli.Models;

namespace YgoDb.Cli.Pipeline;

public static class ExportPipeline
{
    private const int BatchSize = 50;
    private const int MaxWorkers = 20;

    public static async Task RunAsync(
        string outputXlsx,
        string outputCsv,
        int wordLimit = 20,
        Func<NormalizedRow, int, NormalizedRow>? rowPostprocess = null)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        using var ygoDeck = new YgoProDeckClient(http);
        using var yugipedia = new YugipediaClient(http);

        Console.WriteLine("Fetching cards from YGOProDeck...");
        var allCards = await ygoDeck.FetchAllCardsAsync();
        Console.WriteLine($"Fetched {allCards.Count} cards.");

        var candidates = allCards
            .Where(c => CardNormalizer.NeedsErrataLookup(c, wordLimit))
            .ToList();
        Console.WriteLine($"{candidates.Count} cards need errata lookup.");

        // Batch errata fetches in parallel (capped at MaxWorkers)
        var batches = candidates.Chunk(BatchSize).ToArray();
        var errataMap = new Dictionary<string, (string? Shortest, string? Latest)>(
            candidates.Count, StringComparer.OrdinalIgnoreCase);
        var semaphore = new SemaphoreSlim(MaxWorkers);
        var processed = 0;

        var tasks = batches.Select(async batch =>
        {
            await semaphore.WaitAsync();
            try
            {
                var names = batch.Select(c => c.Name).ToList();
                var result = await yugipedia.FetchErrataAsync(names);
                lock (errataMap)
                {
                    foreach (var kvp in result)
                        errataMap[kvp.Key] = kvp.Value;

                    processed += batch.Length;
                    if (processed % 500 == 0 || processed == candidates.Count)
                        Console.WriteLine($"  Processed {processed}/{candidates.Count} errata lookups...");
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        Console.WriteLine("Errata fetch complete. Normalizing...");

        var rows = new List<NormalizedRow>(allCards.Count);
        foreach (var card in allCards)
        {
            errataMap.TryGetValue(card.Name, out var errata);
            var row = CardNormalizer.Normalize(card, errata.Shortest, errata.Latest, wordLimit);

            if (rowPostprocess is not null)
                row = rowPostprocess(row, wordLimit);

            rows.Add(row);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputXlsx) ?? ".");
        ExcelExporter.Export(rows, outputXlsx, wordLimit);
        CsvExporter.Export(rows, outputCsv);

        var eligible = rows.Count(r => r.IsEligible);
        Console.WriteLine($"Done. {eligible} eligible / {rows.Count} total → {outputXlsx}");
    }
}
