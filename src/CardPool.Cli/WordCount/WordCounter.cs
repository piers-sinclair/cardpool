namespace CardPool.Cli.WordCount;

public static partial class WordCounter
{
    [GeneratedRegex(@"\[\s*Pendulum Effect\s*\](.*?)(?=\[|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex PendulumScaleRegex();

    [GeneratedRegex(@"\[\s*Monster Effect\s*\](.*?)(?=\[|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex MonsterEffectRegex();

    public static int CountWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    public static string GetPendulumScaleText(string text)
    {
        var m = PendulumScaleRegex().Match(text);
        return m.Success ? m.Groups[1].Value.Trim() : text;
    }

    public static string GetMonsterEffectText(string text)
    {
        var m = MonsterEffectRegex().Match(text);
        return m.Success ? m.Groups[1].Value.Trim() : string.Empty;
    }

    public static int CountEffectiveWords(string text, string cardType)
    {
        if (cardType.IsPureNormalMonster()) return 0;
        if (cardType.IsPendulumNormalType()) return CountWords(GetPendulumScaleText(text));
        if (cardType.IsPendulumEffectType()) return CountWords(GetPendulumScaleText(text)) + CountWords(GetMonsterEffectText(text));
        return CountWords(text);
    }
}
