using System.Text.RegularExpressions;
using YgoDb.Cli.Models;
using YgoDb.Cli.WordCount;

namespace YgoDb.Cli.Pipeline;

public static partial class MaterialStripper
{
    [GeneratedRegex(@"^(?:\d|""[A-Z]|Any )", RegexOptions.Compiled)]
    private static partial Regex MaterialStartRegex();

    private const string EffectStartersPattern =
        @"Once|If|When|While|Unless|You|This card|Cannot|Must|During|At the|Each|" +
        @"Neither|Both players|Negate|Target|Gains|Draw|Banish|Send|Add|Return|" +
        @"Reveal|Excavate|Take|Place|Equip|Activate";

    [GeneratedRegex(EffectStartersPattern, RegexOptions.Compiled)]
    private static partial Regex EffectStarterRegex();

    private static (string? Material, string Effect) SplitMaterialLine(string text)
    {
        if (string.IsNullOrEmpty(text)) return (null, text);

        var newlineIdx = text.IndexOf('\n');
        if (newlineIdx >= 0)
        {
            var remainder = text[(newlineIdx + 1)..].Trim();
            return string.IsNullOrEmpty(remainder)
                ? (null, text)
                : (text[..newlineIdx].Trim(), remainder);
        }

        if (!MaterialStartRegex().IsMatch(text)) return (null, text);

        var match = EffectStarterRegex().Match(text);
        if (!match.Success || match.Index == 0) return (null, text);

        return (text[..match.Index].Trim(), text[match.Index..]);
    }

    public static string StripMaterialLine(string text) => SplitMaterialLine(text).Effect;

    public static NormalizedRow PostprocessRow(NormalizedRow row, int wordLimit)
    {
        if (!row.Type.IsExtraDeckType())
            return row;

        (row.Materials, row.Desc) = SplitMaterialLine(row.Desc);
        row.ShortestErrata = StripMaterialLine(row.ShortestErrata);
        row.LatestErrata = StripMaterialLine(row.LatestErrata);

        var wordSource = string.IsNullOrEmpty(row.ShortestErrata) ? row.Desc : row.ShortestErrata;
        row.WordCount = WordCounter.CountEffectiveWords(wordSource, row.Type);
        row.IsEligible = row.WordCount <= wordLimit;

        return row;
    }
}
