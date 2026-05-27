var rootCommand = new RootCommand(
    "CardPool — trading card game pool analysis and export tool.\n" +
    "Fetches ~12,000 cards from YGOProDeck, applies errata history from Yugipedia,\n" +
    "and exports cards eligible under configurable criteria such as word count.");

var exportCommand = new Command("export",
    "Export all card data to Excel (.xlsx) and CSV.\n" +
    "Examples:\n" +
    "  cpool export                                         # default: ≤25 words, no-materials, exclude pendulum link\n" +
    "  cpool export --words 20                              # ≤20 words\n" +
    "  cpool export --no-materials false                    # include full material text in word count\n" +
    "  cpool export --exclude-types none                    # include all card types\n" +
    "  cpool export --exclude-types fusion synchro xyz link # main-deck cards only\n" +
    "  cpool export --exclude-types pendulum link flip      # also exclude Flip monsters\n" +
    "  cpool export --errata-mode latest                    # use current text only (fast — no Yugipedia fetch)\n" +
    "  cpool export --words -1                              # no word limit — export all cards\n" +
    "  cpool export --words 30 --output ~/ygo               # ≤30 words, custom output dir");

var wordsOption = new Option<int>("--words")
{
    Description = "Word-count threshold — cards with any printing at or below this limit are included (default: 25, use -1 for no limit)",
    DefaultValueFactory = _ => 25
};

var noMaterialsOption = new Option<bool>("--no-materials")
{
    Description = "Strip fusion/synchro/xyz/link material requirements from effect text before counting; original materials are preserved in a separate column (default: true)",
    DefaultValueFactory = _ => true
};

var excludeTypesOption = new Option<string[]>("--exclude-types")
{
    Description = "Exclude cards whose type contains any of these fragments (case-insensitive, repeatable). Use 'none' to include all types. Examples: pendulum, link, tuner, flip, ritual, fusion, synchro, xyz (default: pendulum link)",
    AllowMultipleArgumentsPerToken = true,
    DefaultValueFactory = _ => ["pendulum", "link"]
};

var errataModeOption = new Option<string>("--errata-mode")
{
    Description = "Which card text version to evaluate for eligibility: 'shortest' = any historical printing (default), 'latest' = current printing only (skips Yugipedia fetch)",
    DefaultValueFactory = _ => "shortest"
};

var outputOption = new Option<string>("--output")
{
    Description = "Directory to write output files to (default: ./output)",
    DefaultValueFactory = _ => "output"
};

exportCommand.Add(wordsOption);
exportCommand.Add(noMaterialsOption);
exportCommand.Add(excludeTypesOption);
exportCommand.Add(errataModeOption);
exportCommand.Add(outputOption);

exportCommand.SetAction(async parseResult =>
{
    var words = parseResult.GetValue(wordsOption);
    var noMaterials = parseResult.GetValue(noMaterialsOption);
    var excludeTypes = parseResult.GetValue(excludeTypesOption) ?? [];
    var errataMode = parseResult.GetValue(errataModeOption)!;
    var outputDirectory = parseResult.GetValue(outputOption)!;
    var latestOnly = errataMode.EqualsIgnoreCase("latest");
    var wordLimit = words == -1 ? int.MaxValue : words;

    var typesSuffix = excludeTypes.Length == 0 || excludeTypes.ContainsIgnoreCase("none")
        ? "_all_types"
        : "_excl_" + string.Join("_", excludeTypes.Order(StringComparer.OrdinalIgnoreCase));
    var errataSuffix = latestOnly ? "_latest" : "";
    var materialsPart = noMaterials ? "no_materials" : "with_materials";
    var wordsPart = words == 25 ? "" : words == -1 ? "_all_words" : $"_{words}words";
    var suffix = $"{materialsPart}{wordsPart}{typesSuffix}{errataSuffix}_export";

    Directory.CreateDirectory(outputDirectory);
    await ExportPipeline.RunAsync(
        $"{outputDirectory}/{suffix}.xlsx",
        $"{outputDirectory}/{suffix}.csv",
        wordLimit: wordLimit,
        latestOnly: latestOnly,
        stripMaterials: noMaterials,
        excludeTypes: excludeTypes);
});

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
        var wc = WordCounter.CountEffectiveWords(t, card.Type);

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
