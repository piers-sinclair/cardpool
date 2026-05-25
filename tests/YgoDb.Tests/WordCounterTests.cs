namespace YgoDb.Tests;

public class WordCounterTests
{
    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("   ", 0)]
    [InlineData("hello world", 2)]
    [InlineData("1 Tuner + 1 non-Tuner", 5)]
    [InlineData("Summoned/Set", 1)]
    [InlineData("word,", 1)]
    public void CountWords_VariousInputs_ReturnsExpected(string? text, int expected) =>
        WordCounter.CountWords(text).ShouldBe(expected);

    [Fact]
    public void CountWords_ConsecutiveSpaces_TreatedAsSingleSeparator() =>
        WordCounter.CountWords("a  b   c").ShouldBe(3);

    [Fact]
    public void CountEffectiveWords_NormalMonster_ReturnsZero() =>
        WordCounter.CountEffectiveWords("This is flavour text.", "Normal Monster").ShouldBe(0);

    [Fact]
    public void CountEffectiveWords_PendulumNormal_CountsPendulumEffectOnly()
    {
        var text = "[Pendulum Effect] Scale 1 effect text here. [/Pendulum Effect] [Monster Effect] Flavour only. [/Monster Effect]";
        var wc = WordCounter.CountEffectiveWords(text, "Pendulum Normal Monster");
        wc.ShouldBe(5);
    }

    [Fact]
    public void CountEffectiveWords_PendulumEffect_SumsBothSections()
    {
        var text = "[Pendulum Effect] pend a b c [/Pendulum Effect] [Monster Effect] mon d e [/Monster Effect]";
        var wc = WordCounter.CountEffectiveWords(text, "Pendulum Effect Monster");
        wc.ShouldBe(7);
    }

    [Fact]
    public void CountEffectiveWords_EffectMonster_CountsFullText()
    {
        var text = "Once per turn you may draw one card.";
        WordCounter.CountEffectiveWords(text, "Effect Monster").ShouldBe(8);
    }

    [Fact]
    public void CountEffectiveWords_PendulumNormalSpacedBracketHeaders_CountsPendulumOnly()
    {
        var text = "[ Pendulum Effect ] \nscale a b c d\n\n[ Monster Effect ] \nFlavour text only.";
        WordCounter.CountEffectiveWords(text, "Pendulum Normal Monster").ShouldBe(5);
    }

    [Fact]
    public void CountEffectiveWords_PendulumEffectSpacedBracketHeaders_SumsBothSections()
    {
        var text = "[ Pendulum Effect ] \npend a b\n\n[ Monster Effect ] \nmon d e f";
        WordCounter.CountEffectiveWords(text, "Pendulum Effect Monster").ShouldBe(7);
    }

    [Fact]
    public void CountEffectiveWords_PendulumTunerEffectMonster_SumsBothSections()
    {
        var text = "[ Pendulum Effect ] \npend a b\n\n[ Monster Effect ] \nmon d e f";
        WordCounter.CountEffectiveWords(text, "Pendulum Tuner Effect Monster").ShouldBe(7);
    }

    [Fact]
    public void CountEffectiveWords_NormalTunerMonster_ReturnsZero() =>
        WordCounter.CountEffectiveWords("Flavour text here.", "Normal Tuner Monster").ShouldBe(0);
}
