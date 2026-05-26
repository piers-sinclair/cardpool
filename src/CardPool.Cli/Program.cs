var rootCommand = new RootCommand(
    "CardPool — trading card game pool analysis and export tool.\n" +
    "Fetches ~12,000 cards from YGOProDeck, applies errata history from Yugipedia,\n" +
    "and exports cards eligible under configurable criteria such as word count.");

var exportCommand = new Command("export",
    "Export all card data to Excel (.xlsx) and CSV.\n" +
    "Examples:\n" +
    "  cpool export                              # default: ≤25 words, no-materials, no-link, no-pendulum\n" +
    "  cpool export --words 20                   # ≤20 words\n" +
    "  cpool export --no-materials false         # include full material text in word count\n" +
    "  cpool export --extra-deck all             # include all Extra Deck types (including Link)\n" +
    "  cpool export --extra-deck none            # main-deck cards only\n" +
    "  cpool export --extra-deck fusion synchro  # Fusion + Synchro only\n" +
    "  cpool export --no-pendulum false          # include Pendulum monsters\n" +
    "  cpool export --words 30 --output ~/ygo    # ≤30 words, custom output dir");

var wordsOption = new Option<int>("--words")
{
    Description = "Word-count threshold — cards with any printing at or below this limit are included (default: 25)",
    DefaultValueFactory = _ => 25
};

var noMaterialsOption = new Option<bool>("--no-materials")
{
    Description = "Strip fusion/synchro/xyz/link material requirements from effect text before counting; original materials are preserved in a separate column (default: true)",
    DefaultValueFactory = _ => true
};

var extraDeckOption = new Option<string[]>("--extra-deck")
{
    Description = "Extra Deck types to include — valid values: all, none, fusion, synchro, xyz, link (repeatable, default: fusion synchro xyz)",
    AllowMultipleArgumentsPerToken = true,
    DefaultValueFactory = _ => ["fusion", "synchro", "xyz"]
};

var noPendulumOption = new Option<bool>("--no-pendulum")
{
    Description = "Exclude Pendulum monsters from output (default: true)",
    DefaultValueFactory = _ => true
};

var outputOption = new Option<string>("--output")
{
    Description = "Directory to write output files to (default: ./output)",
    DefaultValueFactory = _ => "output"
};

exportCommand.Add(wordsOption);
exportCommand.Add(noMaterialsOption);
exportCommand.Add(extraDeckOption);
exportCommand.Add(noPendulumOption);
exportCommand.Add(outputOption);

exportCommand.SetAction(async parseResult =>
{
    var words = parseResult.GetValue(wordsOption);
    var noMaterials = parseResult.GetValue(noMaterialsOption);
    var noPendulum = parseResult.GetValue(noPendulumOption);
    var outputDirectory = parseResult.GetValue(outputOption)!;
    var extraDeckTypes = new HashSet<string>(
        parseResult.GetValue(extraDeckOption) ?? ["all"],
        StringComparer.OrdinalIgnoreCase);

    Func<NormalizedRow, bool>? rowFilter = BuildExtraDeckFilter(extraDeckTypes);
    if (noPendulum)
    {
        var baseFilter = rowFilter;
        rowFilter = baseFilter is null
            ? row => !row.Type.IsPendulumType()
            : row => !row.Type.IsPendulumType() && baseFilter(row);
    }

    var defaultExtraDeckTypes = new HashSet<string>(["fusion", "synchro", "xyz"], StringComparer.OrdinalIgnoreCase);
    var extraDeckSuffix = extraDeckTypes.Contains("all") ? ""
        : extraDeckTypes.SetEquals(defaultExtraDeckTypes) ? "_no_link"
        : extraDeckTypes.Contains("none") ? "_no_extra"
        : "_" + string.Join("_", extraDeckTypes.Order());

    var pendulumSuffix = noPendulum ? "_no_pendulum" : "";

    var suffix = noMaterials
        ? words == 25 ? $"no_materials{extraDeckSuffix}{pendulumSuffix}_export" : $"no_materials_{words}words{extraDeckSuffix}{pendulumSuffix}_export"
        : words == 25 ? $"full{extraDeckSuffix}{pendulumSuffix}_export" : $"full_{words}words{extraDeckSuffix}{pendulumSuffix}_export";

    Directory.CreateDirectory(outputDirectory);
    await ExportPipeline.RunAsync(
        $"{outputDirectory}/{suffix}.xlsx",
        $"{outputDirectory}/{suffix}.csv",
        wordLimit: words,
        rowPostprocess: noMaterials
            ? (row, limit) => MaterialStripper.PostprocessRow(row, limit)
            : null,
        rowFilter: rowFilter);
});

static Func<NormalizedRow, bool>? BuildExtraDeckFilter(HashSet<string> types)
{
    if (types.Contains("all")) return null;
    return row =>
    {
        if (!row.Type.IsExtraDeckType()) return true;
        if (types.Contains("none")) return false;
        if (types.Contains("fusion") && row.Type.Contains("Fusion", StringComparison.OrdinalIgnoreCase)) return true;
        if (types.Contains("synchro") && row.Type.Contains("Synchro", StringComparison.OrdinalIgnoreCase)) return true;
        if (types.Contains("xyz") && row.Type.Contains("XYZ", StringComparison.OrdinalIgnoreCase)) return true;
        if (types.Contains("link") && row.Type.Contains("Link", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    };
}

rootCommand.Add(exportCommand);

var inspectCommand = new Command("inspect",
    "Show all errata versions for a single card with word counts.\n" +
    "Examples:\n" +
    "  cpool inspect \"Raiza the Storm Monarch\"\n" +
    "  cpool inspect \"Dark Magician\"");

var cardNameArg = new Argument<string>("card-name")
{
    Description = "Exact card name (case-insensitive)"
};
inspectCommand.Add(cardNameArg);

inspectCommand.SetAction(async parseResult =>
{
    var name = parseResult.GetValue(cardNameArg)!;

    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    var ygoDeck = new YgoProDeckClient(http);
    var yugipedia = new YugipediaClient(http);

    var card = await ygoDeck.FetchCardByNameAsync(name);
    if (card is null)
    {
        Console.WriteLine($"Card not found: {name}");
        return;
    }

    var html = await yugipedia.FetchErrataHtmlAsync(name);
    List<string> erratas = html is not null
        ? await HtmlErrataScraper.ParseErrataTableAsync(html)
        : [];

    if (erratas.Count == 0)
        erratas = [card.Desc];

    Console.WriteLine($"Card: {card.Name} ({card.Type})");
    Console.WriteLine($"Found {erratas.Count} errata version(s).\n");

    string? shortestText = null;
    int shortestWc = int.MaxValue;
    string latestText = erratas[^1];

    for (var i = 0; i < erratas.Count; i++)
    {
        var t = erratas[i];
        var wc = card.Type.IsPureNormalMonster()
            ? 0
            : WordCounter.CountWords(t);

        var markers = new List<string>();
        if (wc <= shortestWc)
        {
            shortestWc = wc;
            shortestText = t;
            markers.Add("S");
        }
        if (i == erratas.Count - 1) markers.Add("L");

        var tag = markers.Count > 0 ? $" [{string.Join("/", markers)}]" : "";
        Console.WriteLine($"Version {i}: {wc} words{tag}");
    }

    Console.WriteLine($"\n── Shortest ({shortestWc} words) ──");
    Console.WriteLine(shortestText);
    Console.WriteLine($"\n── Latest ──");
    Console.WriteLine(latestText);
});

rootCommand.Add(inspectCommand);

return await rootCommand.Parse(args).InvokeAsync();
