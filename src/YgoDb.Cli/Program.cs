using System.CommandLine;
using YgoDb.Cli.Api;
using YgoDb.Cli.Parsing;
using YgoDb.Cli.Pipeline;
using YgoDb.Cli.WordCount;

var rootCommand = new RootCommand("YgoDb — Yu-Gi-Oh! card database export tool");

var exportCommand = new Command("export", "Export card data to Excel/CSV");

var wordsOption = new Option<int>("--words")
{
    Description = "Word-count threshold for eligibility",
    DefaultValueFactory = _ => 20
};

var noMaterialsOption = new Option<bool>("--no-materials")
{
    Description = "Strip material requirements from Extra Deck monsters"
};

var extraDeckOption = new Option<string[]>("--extra-deck")
{
    Description = "Extra Deck types to include: all, none, fusion, synchro, xyz, link (repeatable). Default: all.",
    AllowMultipleArgumentsPerToken = true,
    DefaultValueFactory = _ => ["all"]
};

exportCommand.Add(wordsOption);
exportCommand.Add(noMaterialsOption);
exportCommand.Add(extraDeckOption);

const string OutputDirectory = "output";
exportCommand.SetAction(async parseResult =>
{
    var words = parseResult.GetValue(wordsOption);
    var noMaterials = parseResult.GetValue(noMaterialsOption);
    var extraDeckTypes = new HashSet<string>(
        parseResult.GetValue(extraDeckOption) ?? ["all"],
        StringComparer.OrdinalIgnoreCase);

    Func<NormalizedRow, bool>? rowFilter = BuildExtraDeckFilter(extraDeckTypes);

    var extraDeckSuffix = extraDeckTypes.Contains("all") ? ""
        : extraDeckTypes.Contains("none") ? "_no_extra"
        : "_" + string.Join("_", extraDeckTypes.Order());

    var suffix = noMaterials
        ? words == 20 ? $"no_materials{extraDeckSuffix}_export" : $"no_materials_{words}words{extraDeckSuffix}_export"
        : words == 20 ? $"full{extraDeckSuffix}_export" : $"full_{words}words{extraDeckSuffix}_export";

    Directory.CreateDirectory(OutputDirectory);
    await ExportPipeline.RunAsync(
        $"{OutputDirectory}/{suffix}.xlsx",
        $"{OutputDirectory}/{suffix}.csv",
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

var inspectCommand = new Command("inspect", "Show errata history for a single card");

var cardNameArg = new Argument<string>("card-name")
{
    Description = "Name of the card to inspect"
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
    List<string> erratas;
    if (html is not null)
        erratas = await HtmlErrataScraper.ParseErrataTableAsync(html);
    else
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
