using System.Text.RegularExpressions;
using YgoDb.Cli.Models;
using YgoDb.Cli.WordCount;

namespace YgoDb.Cli.Pipeline;

public static partial class MaterialStripper
{
    private sealed record MaterialSplit(string? Material, string Effect);

    [GeneratedRegex(@"^(?:\d|""[A-Z]|Any )", RegexOptions.Compiled)]
    private static partial Regex MaterialStartRegex();

    private const string EffectStartersPattern =
        @"Once|If|When|While|Unless|You|This card|Cannot|Must|During|At the|Each|" +
        @"Neither|Both players|Negate|Target|Gains|Draw|Banish|Send|Add|Return|" +
        @"Reveal|Excavate|Take|Place|Equip|Activate";

    [GeneratedRegex(EffectStartersPattern, RegexOptions.Compiled)]
    private static partial Regex EffectStarterRegex();

    private static MaterialSplit SplitMaterialLine(string text)
    {
        if (string.IsNullOrEmpty(text)) return new(null, text);

        var newlineIdx = text.IndexOf('\n');
        if (newlineIdx >= 0)
        {
            var remainder = text[(newlineIdx + 1)..].Trim();
            return string.IsNullOrEmpty(remainder)
                ? new(null, text)
                : new(text[..newlineIdx].Trim(), remainder);
        }

        if (!MaterialStartRegex().IsMatch(text)) return new(null, text);

        var match = EffectStarterRegex().Match(text);
        if (!match.Success || match.Index == 0) return new(null, text);

        return new(text[..match.Index].Trim(), text[match.Index..]);
    }

    public static string StripMaterialLine(string text) => SplitMaterialLine(text).Effect;

    public static NormalizedRow PostprocessRow(NormalizedRow row, int wordLimit)
    {
        if (!row.Type.IsExtraDeckType())
            return row;

        var split = SplitMaterialLine(row.Desc);
        row.Materials = split.Material;
        row.Desc = split.Effect;
        row.ShortestErrata = StripMaterialLine(row.ShortestErrata);
        row.LatestErrata = StripMaterialLine(row.LatestErrata);

        var wordSource = string.IsNullOrEmpty(row.ShortestErrata) ? row.Desc : row.ShortestErrata;
        row.WordCount = WordCounter.CountEffectiveWords(wordSource, row.Type);
        row.IsEligible = row.WordCount <= wordLimit;

        return row;
    }
}
