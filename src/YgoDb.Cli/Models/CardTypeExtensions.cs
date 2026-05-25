namespace YgoDb.Cli.Models;

internal static class CardTypeExtensions
{
    internal static bool IsPureNormalMonster(this string cardType) =>
        cardType.Contains("Normal", StringComparison.OrdinalIgnoreCase)
        && cardType.Contains("Monster", StringComparison.OrdinalIgnoreCase)
        && !cardType.Contains("Pendulum", StringComparison.OrdinalIgnoreCase);

    internal static bool IsPendulumType(this string cardType) =>
        cardType.Contains("Pendulum", StringComparison.OrdinalIgnoreCase);

    internal static bool IsPendulumEffectType(this string cardType) =>
        cardType.Contains("Pendulum", StringComparison.OrdinalIgnoreCase)
        && !cardType.Contains("Normal", StringComparison.OrdinalIgnoreCase);

    internal static bool IsExtraDeckType(this string cardType) =>
        cardType.Contains("Fusion", StringComparison.OrdinalIgnoreCase)
        || cardType.Contains("Synchro", StringComparison.OrdinalIgnoreCase)
        || cardType.Contains("XYZ", StringComparison.OrdinalIgnoreCase)
        || cardType.Contains("Link", StringComparison.OrdinalIgnoreCase);
}
