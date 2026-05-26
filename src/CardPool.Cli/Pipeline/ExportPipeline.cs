namespace CardPool.Cli.Pipeline;

public static class ExportPipeline
{
    private const int BatchSize = 50;
    private const int MaxWorkers = 20;

    public static async Task RunAsync(
        string outputXlsx,
        string outputCsv,
        int wordLimit = 20,
        bool latestOnly = false,
        Func<NormalizedRow, int, NormalizedRow>? rowPostprocess = null,
        Func<NormalizedRow, bool>? rowFilter = null)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        using var ygoDeck = new YgoProDeckClient(http);
        using var yugipedia = new YugipediaClient(http);

        var allCards = await FetchPlayableCardsAsync(ygoDeck);

        Dictionary<string, (string? Shortest, string? Latest)> errataMap =
            NeedsErrataFetch(latestOnly, wordLimit)
                ? await FetchErrataAsync(yugipedia, allCards, wordLimit)
                : new(StringComparer.OrdinalIgnoreCase);

        Console.WriteLine("Normalizing...");
        var rows = BuildRows(allCards, errataMap, wordLimit, rowPostprocess, rowFilter);

        Directory.CreateDirectory(Path.GetDirectoryName(outputXlsx) ?? ".");
        ExcelExporter.Export(rows, outputXlsx, wordLimit);
        CsvExporter.Export(rows, outputCsv);

        var eligible = rows.Count(r => r.IsEligible);
        Console.WriteLine($"Done. {eligible} eligible / {rows.Count} total → {outputXlsx}");
    }

    private static bool NeedsErrataFetch(bool latestOnly, int wordLimit) =>
        !latestOnly && wordLimit != int.MaxValue;

    private static async Task<List<YgoCard>> FetchPlayableCardsAsync(YgoProDeckClient ygoDeck)
    {
        Console.WriteLine("Fetching cards from YGOProDeck...");
        var cards = (await ygoDeck.FetchAllCardsAsync())
            .Where(c => !c.Type.IsToken() && !c.Type.IsSkillCard())
            .ToList();
        Console.WriteLine($"Fetched {cards.Count} cards.");
        return cards;
    }

    private static async Task<Dictionary<string, (string? Shortest, string? Latest)>> FetchErrataAsync(
        YugipediaClient yugipedia,
        List<YgoCard> cards,
        int wordLimit)
    {
        var candidates = cards
            .Where(c => CardNormalizer.NeedsErrataLookup(c, wordLimit))
            .ToList();
        Console.WriteLine($"{candidates.Count} cards need errata lookup.");

        var errataMap = new Dictionary<string, (string? Shortest, string? Latest)>(StringComparer.OrdinalIgnoreCase);
        var semaphore = new SemaphoreSlim(MaxWorkers);
        var processed = 0;

        await Task.WhenAll(candidates.Chunk(BatchSize).Select(async batch =>
        {
            await semaphore.WaitAsync();
            try
            {
                var result = await yugipedia.FetchErrataAsync(batch.Select(c => c.Name).ToList());
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
        }));

        Console.WriteLine("Errata fetch complete.");
        return errataMap;
    }

    private static List<NormalizedRow> BuildRows(
        List<YgoCard> cards,
        Dictionary<string, (string? Shortest, string? Latest)> errataMap,
        int wordLimit,
        Func<NormalizedRow, int, NormalizedRow>? rowPostprocess,
        Func<NormalizedRow, bool>? rowFilter)
    {
        var rows = new List<NormalizedRow>(cards.Count);
        foreach (var card in cards)
        {
            errataMap.TryGetValue(card.Name, out var errata);
            var row = CardNormalizer.Normalize(card, errata.Shortest, errata.Latest, wordLimit);

            if (rowPostprocess is not null)
                row = rowPostprocess(row, wordLimit);

            if (rowFilter is null || rowFilter(row))
                rows.Add(row);
        }
        return rows;
    }
}
