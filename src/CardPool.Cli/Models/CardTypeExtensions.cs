namespace CardPool.Cli.Models;

internal static class CardTypeExtensions
{
    internal static bool IsPureNormalMonster(this string cardType) =>
        cardType.ContainsIgnoreCase("Normal")
        && cardType.ContainsIgnoreCase("Monster")
        && !cardType.ContainsIgnoreCase("Pendulum");

    internal static bool IsPendulumType(this string cardType) =>
        cardType.ContainsIgnoreCase("Pendulum");

    internal static bool IsPendulumNormalType(this string cardType) =>
        cardType.ContainsIgnoreCase("Pendulum")
        && cardType.ContainsIgnoreCase("Normal");

    internal static bool IsPendulumEffectType(this string cardType) =>
        cardType.ContainsIgnoreCase("Pendulum")
        && !cardType.ContainsIgnoreCase("Normal");

    internal static bool IsExtraDeckType(this string cardType) =>
        cardType.ContainsIgnoreCase("Fusion")
        || cardType.ContainsIgnoreCase("Synchro")
        || cardType.ContainsIgnoreCase("XYZ")
        || cardType.ContainsIgnoreCase("Link");

    internal static bool IsToken(this string cardType) =>
        cardType.ContainsIgnoreCase("Token");

    internal static bool IsSkillCard(this string cardType) =>
        cardType.ContainsIgnoreCase("Skill");
}
