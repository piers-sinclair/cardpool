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
    private readonly DateOnly? _since;

    public CardPoolExporter(
        YgoProDeckClient ygoDeck,
        YugipediaClient yugipedia,
        int wordLimit,
        bool latestOnly,
        bool stripMaterials,
        string[]? excludeTypes,
        DateOnly? since = null)
    {
        _ygoDeck = ygoDeck;
        _yugipedia = yugipedia;
        _wordLimit = wordLimit;
        _latestOnly = latestOnly;
        _stripMaterials = stripMaterials;
        _excludeTypes = excludeTypes;
        _since = since;
    }

    public async Task ExportAsync(string outputDirectory)
    {
        var allCards = await FetchPlayableCardsAsync();

        var rows = (NeedsErrataFetch()
            ? await BuildShortestErrataRowsAsync(allCards)
            : BuildLatestErrataRows(allCards))
            .OrderByDescending(r => r.EligibleSince.HasValue)
            .ThenByDescending(r => r.EligibleSince)
            .ThenBy(r => r.Name)
            .ToList();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var todayStr = today.ToString(AppConstants.IsoDateFormat, CultureInfo.InvariantCulture);
        var runDir = Path.Combine(outputDirectory, todayStr);
        Directory.CreateDirectory(runDir);

        var prefix = BuildPrefix();
        var exportBase = Path.Combine(runDir, $"{prefix}_export");

        CardPoolExcelExporter.Export(rows, exportBase + ".xlsx", _wordLimit);
        CardPoolCsvExporter.Export(rows, exportBase + ".csv");

        if (_since is not null)
        {
            var rnBase = Path.Combine(runDir, $"{prefix}_release_notes");
            ReleaseNotesExcelExporter.Export(rows, _since.Value, rnBase + ".xlsx");
            ReleaseNotesCsvExporter.Export(rows, _since.Value, rnBase + ".csv");
            Console.WriteLine($"Release notes → {rnBase}.xlsx / .csv");
        }

        var eligible = rows.Count(r => r.IsEligible);
        Console.WriteLine($"Done. {eligible} eligible / {rows.Count} total → {exportBase}.xlsx");
    }

    private string BuildPrefix()
    {
        var materialsPart = _stripMaterials ? "no_materials" : "with_materials";
        var wordsPart = _wordLimit == AppConstants.DefaultWordLimit ? ""
            : _wordLimit == int.MaxValue ? "_all_words"
            : $"_{_wordLimit}words";
        var typesSuffix = _excludeTypes is null or { Length: 0 }
            ? "_all_types"
            : "_excl_" + string.Join("_", _excludeTypes.Order(StringComparer.OrdinalIgnoreCase));
        var errataSuffix = _latestOnly ? "_latest" : "";
        return $"{materialsPart}{wordsPart}{typesSuffix}{errataSuffix}";
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

        Console.WriteLine("Fetching set dates from YGOProDeck...");
        var setDates = await _ygoDeck.FetchSetDatesAsync();

        var errataMap = new Dictionary<string, CardErrata>(StringComparer.OrdinalIgnoreCase);
        var processed = 0;

        await Parallel.ForEachAsync(
            candidates.Chunk(BatchSize),
            new ParallelOptions { MaxDegreeOfParallelism = MaxWorkers },
            ProcessBatchAsync);

        Console.WriteLine("Errata fetch complete.");
        return errataMap;

        async ValueTask ProcessBatchAsync(YgoCard[] batch, CancellationToken _)
        {
            var results = await _yugipedia.FetchErrataAsync(batch.Select(c => c.Name).ToList(), setDates);
            int currentProcessed;
            lock (errataMap)
            {
                foreach (var kvp in results)
                    errataMap[kvp.Key] = kvp.Value;
                currentProcessed = processed += batch.Length;
            }
            if (currentProcessed % 500 == 0 || currentProcessed == candidates.Count)
                Console.WriteLine($"  Processed {currentProcessed}/{candidates.Count} errata lookups...");
        }
    }

    private NormalizedRow BuildRow(YgoCard card, CardErrata? errata)
    {
        var row = CardNormalizer.Normalize(card, errata, _wordLimit, _stripMaterials);
        return _stripMaterials ? MaterialStripper.PostprocessRow(row) : row;
    }

    private bool IsTypeIncluded(string cardType) =>
        _excludeTypes is null or { Length: 0 }
        || _excludeTypes.All(fragment => !cardType.ContainsIgnoreCase(fragment));
}
