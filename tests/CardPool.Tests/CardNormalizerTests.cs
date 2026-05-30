namespace CardPool.Tests;

public class CardNormalizerTests
{
    private static YgoCard MakeCard(string type, string desc, string? tcgDate = null) =>
        new(Id: 1, Name: "Test", Type: type, Race: null, Attribute: null,
            Level: null, Atk: null, Def: null, Scale: null, LinkVal: null,
            LinkMarkers: null, Archetype: null, Desc: desc,
            CardSets: null, BanlistInfo: null, CardImages: null,
            MiscInfo: tcgDate is null ? null : [new MiscInfo(tcgDate)]);

    [Fact]
    public void Normalize_PendulumTunerEffectMonstersOnlyErrata_FallsBackToDesc()
    {
        var desc = "[ Pendulum Effect ] \npend a b\n\n[ Monster Effect ] \nmon d e f";
        var card = MakeCard("Pendulum Tuner Effect Monster", desc);

        const string incompleteErrata = "Monster Effect: mon d e f";

        var row = CardNormalizer.Normalize(card, new CardErrata(incompleteErrata, incompleteErrata, null), wordLimit: 20);

        row.WordCount.ShouldBe(7);
        row.IsEligible.ShouldBeTrue();
    }

    [Fact]
    public void Normalize_PendulumTunerEffectPendulumOnlyErrata_FallsBackToDesc()
    {
        var desc = "[ Pendulum Effect ] \npend a b\n\n[ Monster Effect ] \nmon d e f";
        var card = MakeCard("Pendulum Tuner Effect Monster", desc);

        const string incompleteErrata = "[Pendulum Effect] pend a b";

        var row = CardNormalizer.Normalize(card, new CardErrata(incompleteErrata, incompleteErrata, null), wordLimit: 20);

        row.WordCount.ShouldBe(7);
    }

    [Fact]
    public void Normalize_PendulumEffectCompleteErrata_UsesErrata()
    {
        var desc = "[ Pendulum Effect ] \npend a b c d e f\n\n[ Monster Effect ] \nmon g h i j k l";
        var card = MakeCard("Pendulum Effect Monster", desc);

        const string completeErrata = "[Pendulum Effect] pend a b [Monster Effect] mon c d";

        var row = CardNormalizer.Normalize(card, new CardErrata(completeErrata, completeErrata, null), wordLimit: 20);

        row.WordCount.ShouldBe(6);
    }

    [Fact]
    public void Normalize_PendulumNormalNoErrataPage_UsesDesc()
    {
        var desc = "[ Pendulum Effect ] \npend a b c\n\n[ Monster Effect ] \nFlavour only.";
        var card = MakeCard("Pendulum Normal Monster", desc);

        var row = CardNormalizer.Normalize(card, errata: null, wordLimit: 20);

        row.WordCount.ShouldBe(4);
    }

    [Fact]
    public void Normalize_EffectMonsterDescOverWordLimit_IsNotEligible()
    {
        var desc = string.Join(" ", Enumerable.Repeat("word", 30));
        var card = MakeCard("Effect Monster", desc);

        var row = CardNormalizer.Normalize(card, errata: null, wordLimit: 20);

        row.WordCount.ShouldBe(30);
        row.IsEligible.ShouldBeFalse();
    }

    [Fact]
    public void Normalize_EffectMonsterDescOverLimitShortestErrataUnderLimit_IsEligible()
    {
        var desc = string.Join(" ", Enumerable.Repeat("word", 30));
        var card = MakeCard("Effect Monster", desc);
        const string shortErrata = "Once per turn: Draw 1 card.";

        var row = CardNormalizer.Normalize(card, new CardErrata(shortErrata, desc, null), wordLimit: 20);

        row.WordCount.ShouldBe(WordCounter.CountWords(shortErrata));
        row.IsEligible.ShouldBeTrue();
    }

    [Fact]
    public void Normalize_NoErrataPageWithTcgDate_LatestErrataDateEqualsTcgDate()
    {
        var card = MakeCard("Effect Monster", "text", tcgDate: "2002-03-08");

        var row = CardNormalizer.Normalize(card, errata: null, wordLimit: 20);

        row.LatestErrataDate.ShouldBe(new DateOnly(2002, 3, 8));
    }

    [Fact]
    public void Normalize_NoErrataPageNullTcgDate_LatestErrataDateIsNull()
    {
        var card = MakeCard("Effect Monster", "text", tcgDate: null);

        var row = CardNormalizer.Normalize(card, errata: null, wordLimit: 20);

        row.LatestErrataDate.ShouldBeNull();
    }

    [Fact]
    public void Normalize_ErrataWithYugipediaDate_LatestErrataDateFromYugipedia()
    {
        var card = MakeCard("Effect Monster", "text", tcgDate: "2002-03-08");

        var row = CardNormalizer.Normalize(card, new CardErrata("text", "text", "September 13, 2003"), wordLimit: 20);

        row.LatestErrataDate.ShouldBe(new DateOnly(2003, 9, 13));
    }

    [Fact]
    public void Normalize_ErrataNoYugipediaDate_LatestErrataDateEqualsTcgDate()
    {
        var card = MakeCard("Effect Monster", "text", tcgDate: "2002-03-08");

        var row = CardNormalizer.Normalize(card, new CardErrata("text", "text", null), wordLimit: 20);

        row.LatestErrataDate.ShouldBe(new DateOnly(2002, 3, 8));
    }
}
