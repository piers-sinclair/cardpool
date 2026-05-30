namespace CardPool.Cli.Models;

internal static class CardTypeExtensions
{
    private const string Normal = "Normal";
    private const string Monster = "Monster";
    private const string Pendulum = "Pendulum";
    private const string Fusion = "Fusion";
    private const string Synchro = "Synchro";
    private const string Xyz = "XYZ";
    private const string Link = "Link";
    private const string Token = "Token";
    private const string Skill = "Skill";

    internal static bool IsPureNormalMonster(this string cardType) =>
        cardType.ContainsIgnoreCase(Normal)
        && cardType.ContainsIgnoreCase(Monster)
        && !cardType.ContainsIgnoreCase(Pendulum);

    internal static bool IsPendulumType(this string cardType) =>
        cardType.ContainsIgnoreCase(Pendulum);

    internal static bool IsPendulumNormalType(this string cardType) =>
        cardType.ContainsIgnoreCase(Pendulum)
        && cardType.ContainsIgnoreCase(Normal);

    internal static bool IsPendulumEffectType(this string cardType) =>
        cardType.ContainsIgnoreCase(Pendulum)
        && !cardType.ContainsIgnoreCase(Normal);

    internal static bool IsExtraDeckType(this string cardType) =>
        cardType.ContainsIgnoreCase(Fusion)
        || cardType.ContainsIgnoreCase(Synchro)
        || cardType.ContainsIgnoreCase(Xyz)
        || cardType.ContainsIgnoreCase(Link);

    internal static bool IsToken(this string cardType) =>
        cardType.ContainsIgnoreCase(Token);

    internal static bool IsSkillCard(this string cardType) =>
        cardType.ContainsIgnoreCase(Skill);
}
