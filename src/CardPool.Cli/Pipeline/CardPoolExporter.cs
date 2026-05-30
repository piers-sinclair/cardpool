namespace CardPool.Cli.Pipeline;

public class CardPoolExporter
{
    private const int BatchSize = 50;
    private const int MaxWorkers = 20;

    private readonly YgoProDeckClient _ygoDeck;
    private readonly YugipediaClient _yugipedia;
    private readonly int _wordLimit;
    private readonly bool _latestOnly;
    private readonly bool _stripMaterials;
    private readonly string[]? _excludeTypes;

    public CardPoolExporter(
        YgoProDeckClient ygoDeck,
        YugipediaClient yugipedia,
        int wordLimit,
        bool latestOnly,
        bool stripMaterials,
        string[]? excludeTypes)
    {
        _ygoDeck = ygoDeck;
        _yugipedia = yugipedia;
        _wordLimit = wordLimit;
        _latestOnly = latestOnly;
        _stripMaterials = stripMaterials;
        _excludeTypes = excludeTypes;
    }

    public async Task ExportAsync(string outputXlsx, string outputCsv)
    {
        var allCards = await FetchPlayableCardsAsync();

        var rows = NeedsErrataFetch()
            ? await BuildShortestErrataRowsAsync(allCards)
            : BuildLatestErrataRows(allCards);

        Directory.CreateDirectory(Path.GetDirectoryName(outputXlsx) ?? ".");
        ExcelExporter.Export(rows, outputXlsx, _wordLimit);
        CsvExporter.Export(rows, outputCsv);

        var eligible = rows.Count(r => r.IsEligible);
        Console.WriteLine($"Done. {eligible} eligible / {rows.Count} total → {outputXlsx}");
    }

    private bool NeedsErrataFetch() => !_latestOnly && _wordLimit != int.MaxValue;

    private async Task<List<YgoCard>> FetchPlayableCardsAsync()
    {
        Console.WriteLine("Fetching cards from YGOProDeck...");
        var cards = (await _ygoDeck.FetchAllCardsAsync())
            .Where(c => !c.Type.IsToken() && !c.Type.IsSkillCard())
            .ToList();
        Console.WriteLine($"Fetched {cards.Count} cards.");
        return cards;
    }

    private async Task<List<NormalizedRow>> BuildShortestErrataRowsAsync(List<YgoCard> cards)
    {
        var errataMap = await FetchErrataAsync(cards);
        Console.WriteLine("Normalizing...");
        return cards
            .Select(card => BuildRow(card, errataMap.GetValueOrDefault(card.Name)))
            .Where(row => IsTypeIncluded(row.Type))
            .ToList();
    }

    private List<NormalizedRow> BuildLatestErrataRows(List<YgoCard> cards)
    {
        Console.WriteLine("Normalizing...");
        return cards
            .Select(card => BuildRow(card, null))
            .Where(row => IsTypeIncluded(row.Type))
            .ToList();
    }

    private async Task<Dictionary<string, CardErrata>> FetchErrataAsync(List<YgoCard> cards)
    {
        var candidates = cards
            .Where(c => CardNormalizer.NeedsErrataLookup(c, _wordLimit))
            .ToList();
        Console.WriteLine($"{candidates.Count} cards need errata lookup.");

        var errataMap = new Dictionary<string, CardErrata>(StringComparer.OrdinalIgnoreCase);
        var processed = 0;

        await Parallel.ForEachAsync(
            candidates.Chunk(BatchSize),
            new ParallelOptions { MaxDegreeOfParallelism = MaxWorkers },
            async (batch, _) =>
            {
                var result = await _yugipedia.FetchErrataAsync(batch.Select(c => c.Name).ToList());
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

    private NormalizedRow BuildRow(YgoCard card, CardErrata? errata)
    {
        var row = CardNormalizer.Normalize(card, errata, _wordLimit);
        return _stripMaterials ? MaterialStripper.PostprocessRow(row) : row;
    }

    private bool IsTypeIncluded(string cardType) =>
        _excludeTypes is null or { Length: 0 }
        || _excludeTypes.ContainsIgnoreCase("none")
        || _excludeTypes.All(fragment => !cardType.ContainsIgnoreCase(fragment));
}
