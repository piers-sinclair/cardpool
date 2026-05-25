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

    public static string StripMaterialLine(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var newlineIdx = text.IndexOf('\n');
        if (newlineIdx >= 0)
        {
            var remainder = text[(newlineIdx + 1)..].Trim();
            return string.IsNullOrEmpty(remainder) ? text : remainder;
        }

        if (!MaterialStartRegex().IsMatch(text)) return text;

        var match = EffectStarterRegex().Match(text);
        if (!match.Success || match.Index == 0) return text;

        return text[match.Index..];
    }

    public static NormalizedRow PostprocessRow(NormalizedRow row, int wordLimit)
    {
        if (!row.Type.IsExtraDeckType())
            return row;

        row.Desc = StripMaterialLine(row.Desc);
        row.ShortestErrata = StripMaterialLine(row.ShortestErrata);
        row.LatestErrata = StripMaterialLine(row.LatestErrata);

        var wordSource = string.IsNullOrEmpty(row.ShortestErrata) ? row.Desc : row.ShortestErrata;
        row.WordCount = WordCounter.CountEffectiveWords(wordSource, row.Type);
        row.IsEligible = row.WordCount <= wordLimit;

        return row;
    }
}
