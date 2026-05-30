namespace CardPool.Cli.Pipeline;

public class ExportPipeline(YgoProDeckClient ygoDeck, YugipediaClient yugipedia)
{
    private const int BatchSize = 50;
    private const int MaxWorkers = 20;

    public async Task RunAsync(
        string outputXlsx,
        string outputCsv,
        int wordLimit = 20,
        bool latestOnly = false,
        bool stripMaterials = false,
        string[]? excludeTypes = null)
    {
        var allCards = await FetchPlayableCardsAsync();

        var rows = NeedsErrataFetch(latestOnly, wordLimit)
            ? await BuildShortestErrataRowsAsync(allCards, wordLimit, stripMaterials, excludeTypes)
            : BuildLatestErrataRows(allCards, wordLimit, stripMaterials, excludeTypes);

        Directory.CreateDirectory(Path.GetDirectoryName(outputXlsx) ?? ".");
        ExcelExporter.Export(rows, outputXlsx, wordLimit);
        CsvExporter.Export(rows, outputCsv);

        var eligible = rows.Count(r => r.IsEligible);
        Console.WriteLine($"Done. {eligible} eligible / {rows.Count} total → {outputXlsx}");
    }

    private async Task<List<YgoCard>> FetchPlayableCardsAsync()
    {
        Console.WriteLine("Fetching cards from YGOProDeck...");
        var cards = (await ygoDeck.FetchAllCardsAsync())
            .Where(c => !c.Type.IsToken() && !c.Type.IsSkillCard())
            .ToList();
        Console.WriteLine($"Fetched {cards.Count} cards.");
        return cards;
    }

    private async Task<List<NormalizedRow>> BuildShortestErrataRowsAsync(
        List<YgoCard> cards,
        int wordLimit,
        bool stripMaterials,
        string[]? excludeTypes)
    {
        var errataMap = await FetchErrataAsync(cards, wordLimit);
        Console.WriteLine("Normalizing...");
        return cards
            .Select(card => BuildRow(card, errataMap.GetValueOrDefault(card.Name), wordLimit, stripMaterials))
            .Where(row => IsTypeIncluded(row.Type, excludeTypes))
            .ToList();
    }

    private async Task<Dictionary<string, CardErrata>> FetchErrataAsync(
        List<YgoCard> cards,
        int wordLimit)
    {
        var candidates = cards
            .Where(c => CardNormalizer.NeedsErrataLookup(c, wordLimit))
            .ToList();
        Console.WriteLine($"{candidates.Count} cards need errata lookup.");

        var errataMap = new Dictionary<string, CardErrata>(StringComparer.OrdinalIgnoreCase);
        var processed = 0;

        await Parallel.ForEachAsync(
            candidates.Chunk(BatchSize),
            new ParallelOptions { MaxDegreeOfParallelism = MaxWorkers },
            async (batch, _) =>
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
            });

        Console.WriteLine("Errata fetch complete.");
        return errataMap;
    }

    private static bool NeedsErrataFetch(bool latestOnly, int wordLimit) =>
        !latestOnly && wordLimit != int.MaxValue;

    private static List<NormalizedRow> BuildLatestErrataRows(
        List<YgoCard> cards,
        int wordLimit,
        bool stripMaterials,
        string[]? excludeTypes)
    {
        Console.WriteLine("Normalizing...");
        return cards
            .Select(card => BuildRow(card, default, wordLimit, stripMaterials))
            .Where(row => IsTypeIncluded(row.Type, excludeTypes))
            .ToList();
    }

    private static bool IsTypeIncluded(string cardType, string[]? excludeTypes) =>
        excludeTypes is null or { Length: 0 }
        || excludeTypes.ContainsIgnoreCase("none")
        || excludeTypes.All(fragment => !cardType.ContainsIgnoreCase(fragment));

    private static NormalizedRow BuildRow(YgoCard card, CardErrata errata, int wordLimit, bool stripMaterials)
    {
        var row = CardNormalizer.Normalize(card, errata, wordLimit);
        return stripMaterials ? MaterialStripper.PostprocessRow(row) : row;
    }
}
