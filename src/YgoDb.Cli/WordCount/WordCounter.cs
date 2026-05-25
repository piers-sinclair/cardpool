using System.Text.RegularExpressions;

namespace YgoDb.Cli.WordCount;

public static partial class WordCounter
{
    // Match section headers like "[ Pendulum Effect ]" or "[Pendulum Effect]" and capture until the next "[" or end of string
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
        // Mirrors Python: is_normal = "Normal" in type and "Monster" in type
        bool isNormal = cardType.Contains("Normal", StringComparison.OrdinalIgnoreCase)
                        && cardType.Contains("Monster", StringComparison.OrdinalIgnoreCase);
        bool isPendulum = cardType.Contains("Pendulum", StringComparison.OrdinalIgnoreCase);

        if (isNormal && !isPendulum) return 0;

        if (isPendulum)
        {
            var pend = CountWords(PendulumEffectText(text ?? ""));
            return isNormal
                ? pend  // Pendulum Normal: flavour excluded
                : pend + CountWords(PendulumMonsterText(text ?? ""));
        }

        return CountWords(text);
    }
}
