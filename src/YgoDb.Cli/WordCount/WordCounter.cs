using System.Text.RegularExpressions;

namespace YgoDb.Cli.WordCount;

public static partial class WordCounter
{
    [GeneratedRegex(@"\[\s*Pendulum Effect\s*\](.*?)(?=\[|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex PendulumEffectRegex();

    [GeneratedRegex(@"\[\s*Monster Effect\s*\](.*?)(?=\[|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex MonsterEffectRegex();

    public static int CountWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    public static string PendulumEffectText(string text)
    {
        var m = PendulumEffectRegex().Match(text);
        return m.Success ? m.Groups[1].Value.Trim() : text;
    }

    public static string PendulumMonsterText(string text)
    {
        var m = MonsterEffectRegex().Match(text);
        return m.Success ? m.Groups[1].Value.Trim() : string.Empty;
    }

    public static int CountEffectiveWords(string? text, string cardType)
    {
        if (cardType.IsPureNormalMonster()) return 0;

        if (cardType.IsPendulumType())
        {
            var pend = CountWords(PendulumEffectText(text ?? ""));
            return cardType.IsPendulumEffectType()
                ? pend + CountWords(PendulumMonsterText(text ?? ""))
                : pend;
        }

        return CountWords(text);
    }
}
