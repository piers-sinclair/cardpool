namespace YgoDb.Tests;

public class WordCounterTests
{
    // ── count_words ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("   ", 0)]
    [InlineData("hello world", 2)]
    [InlineData("1 Tuner + 1 non-Tuner", 5)]
    [InlineData("Summoned/Set", 1)]
    [InlineData("word,", 1)]   // punctuation attached counts as part of the word
    public void CountWords_VariousInputs_ReturnsExpected(string? text, int expected) =>
        WordCounter.CountWords(text).ShouldBe(expected);

    [Fact]
    public void CountWords_CollapseMultipleSpaces_CountsCorrectly() =>
        WordCounter.CountWords("a  b   c").ShouldBe(3);

    // ── count_effective_words ──────────────────────────────────────────────

    [Fact]
    public void CountEffectiveWords_NormalMonster_ReturnsZero() =>
        WordCounter.CountEffectiveWords("This is flavour text.", "Normal Monster").ShouldBe(0);

    [Fact]
    public void CountEffectiveWords_PendulumNormal_CountsPendulumEffectOnly()
    {
        var text = "[Pendulum Effect] Scale 1 effect text here. [/Pendulum Effect] [Monster Effect] Flavour only. [/Monster Effect]";
        var wc = WordCounter.CountEffectiveWords(text, "Pendulum Normal Monster");
        // "Scale 1 effect text here." = 5 words
        wc.ShouldBe(5);
    }

    [Fact]
    public void CountEffectiveWords_PendulumEffect_SumsBothSections()
    {
        var text = "[Pendulum Effect] pend a b c [/Pendulum Effect] [Monster Effect] mon d e [/Monster Effect]";
        var wc = WordCounter.CountEffectiveWords(text, "Pendulum Effect Monster");
        // pend section: "pend a b c" = 4; mon section: "mon d e" = 3; total = 7
        wc.ShouldBe(7);
    }

    [Fact]
    public void CountEffectiveWords_EffectMonster_CountsFullText()
    {
        var text = "Once per turn you may draw one card.";
        WordCounter.CountEffectiveWords(text, "Effect Monster").ShouldBe(8);
    }

    // ── YGOProDeck space-in-bracket format: "[ Pendulum Effect ]" ─────────
    // Regression: original regex used [/Pendulum Effect] closing tag which doesn't
    // exist in YGOProDeck desc format; API uses "[ Pendulum Effect ]" headers only.

    [Fact]
    public void CountEffectiveWords_PendulumNormal_SpaceInBracketFormat_CountsPendulumOnly()
    {
        // Matches real YGOProDeck desc layout (spaces inside brackets, no closing tag)
        var text = "[ Pendulum Effect ] \nscale a b c d\n\n[ Monster Effect ] \nFlavour text only.";
        // "scale a b c d" = 5 words; monster section is flavour, excluded
        WordCounter.CountEffectiveWords(text, "Pendulum Normal Monster").ShouldBe(5);
    }

    [Fact]
    public void CountEffectiveWords_PendulumEffect_SpaceInBracketFormat_SumsBothSections()
    {
        // Matches real YGOProDeck desc layout
        var text = "[ Pendulum Effect ] \npend a b\n\n[ Monster Effect ] \nmon d e f";
        // "pend a b" = 3; "mon d e f" = 4; total = 7
        WordCounter.CountEffectiveWords(text, "Pendulum Effect Monster").ShouldBe(7);
    }

    // ── Pendulum Tuner Effect Monster type detection ───────────────────────
    // Regression: "Pendulum Tuner Effect Monster" contains "Pendulum" and "Effect"
    // but NOT the substring "Pendulum Effect" (separated by "Tuner").
    // CountEffectiveWords must treat it as a Pendulum Effect type (count both sections).

    [Fact]
    public void CountEffectiveWords_PendulumTunerEffectMonster_SumsBothSections()
    {
        var text = "[ Pendulum Effect ] \npend a b\n\n[ Monster Effect ] \nmon d e f";
        // Same as Pendulum Effect Monster — both sections counted
        WordCounter.CountEffectiveWords(text, "Pendulum Tuner Effect Monster").ShouldBe(7);
    }

    // ── Normal Tuner Monster returns 0 ────────────────────────────────────
    // Regression: Python uses "Normal" in type AND "Monster" in type (not Equals).
    // "Normal Tuner Monster" is also a pure Normal and must return 0.

    [Fact]
    public void CountEffectiveWords_NormalTunerMonster_ReturnsZero() =>
        WordCounter.CountEffectiveWords("Flavour text here.", "Normal Tuner Monster").ShouldBe(0);
}
