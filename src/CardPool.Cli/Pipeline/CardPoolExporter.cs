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
        var (allCards, setDates) = await FetchPlayableCardsAsync();

        var rows = (NeedsErrataFetch()
            ? await BuildShortestErrataRowsAsync(allCards, setDates)
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
        var exportBase = Path.Combine(runDir, $"cardpool_{prefix}");

        CardPoolExcelExporter.Export(rows, exportBase + ".xlsx", _wordLimit);
        CardPoolCsvExporter.Export(rows, exportBase + ".csv");

        if (_since is not null)
        {
            var sinceStr = _since.Value.ToString(AppConstants.IsoDateFormat, CultureInfo.InvariantCulture);
            var rnBase = Path.Combine(runDir, $"release_notes_{sinceStr}_{prefix}");
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

    private async Task<(List<YgoCard> Cards, IReadOnlyDictionary<string, string> SetDates)> FetchPlayableCardsAsync()
    {
        Console.WriteLine("Fetching cards and set dates from YGOProDeck...");
        var cardsTask = _ygoDeck.FetchAllCardsAsync();
        var setDatesTask = _ygoDeck.FetchSetDatesAsync();
        await Task.WhenAll(cardsTask, setDatesTask);

        var setDates = await setDatesTask;
        var cards = (await cardsTask)
            .Where(c => !c.Type.IsToken() && !c.Type.IsSkillCard())
            .Where(c => c.IsTcgLegal)
            .Select(c => c with { EarliestSetDate = ResolveEarliestSetDate(c, setDates) })
            .ToList();

        Console.WriteLine($"Fetched {cards.Count} cards.");
        return (cards, setDates);
    }

    private static string? ResolveEarliestSetDate(YgoCard card, IReadOnlyDictionary<string, string> setDates)
    {
        if (card.CardSets is null) return null;
        return card.CardSets
            .Select(s => setDates.GetValueOrDefault(s.SetName))
            .OfType<string>()
            .OrderBy(d => d)
            .FirstOrDefault();
    }

    private async Task<List<NormalizedRow>> BuildShortestErrataRowsAsync(List<YgoCard> cards, IReadOnlyDictionary<string, string> setDates)
    {
        var errataMap = await FetchErrataAsync(cards, setDates);
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

    private async Task<Dictionary<string, CardErrata>> FetchErrataAsync(List<YgoCard> cards, IReadOnlyDictionary<string, string> setDates)
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
