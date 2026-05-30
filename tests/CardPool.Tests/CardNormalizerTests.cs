namespace CardPool.Tests;

public class CardNormalizerTests
{
    private static YgoCard MakeCard(string type, string desc, string? tcgDate = null) =>
        new(Id: 1, Name: "Test", Type: type, Race: null, Attribute: null,
            Level: null, Atk: null, Def: null, Scale: null, LinkVal: null,
            LinkMarkers: null, Archetype: null, Desc: desc,
            CardSets: null, BanlistInfo: null, CardImages: null,
            MiscInfo: tcgDate is null ? null : [new MiscInfo(tcgDate)]);

    private static CardErrata SingleLoreErrata(string text, string? date = null) =>
        new(text, text, [new ErrataLore(text, date)]);

    [Fact]
    public void Normalize_PendulumTunerEffectMonstersOnlyErrata_FallsBackToDesc()
    {
        var desc = "[ Pendulum Effect ] \npend a b\n\n[ Monster Effect ] \nmon d e f";
        var card = MakeCard("Pendulum Tuner Effect Monster", desc);
        const string incompleteErrata = "Monster Effect: mon d e f";

        var row = CardNormalizer.Normalize(card, SingleLoreErrata(incompleteErrata), wordLimit: 20);

        row.WordCount.ShouldBe(7);
        row.IsEligible.ShouldBeTrue();
    }

    [Fact]
    public void Normalize_PendulumTunerEffectPendulumOnlyErrata_FallsBackToDesc()
    {
        var desc = "[ Pendulum Effect ] \npend a b\n\n[ Monster Effect ] \nmon d e f";
        var card = MakeCard("Pendulum Tuner Effect Monster", desc);
        const string incompleteErrata = "[Pendulum Effect] pend a b";

        var row = CardNormalizer.Normalize(card, SingleLoreErrata(incompleteErrata), wordLimit: 20);

        row.WordCount.ShouldBe(7);
    }

    [Fact]
    public void Normalize_PendulumEffectCompleteErrata_UsesErrata()
    {
        var desc = "[ Pendulum Effect ] \npend a b c d e f\n\n[ Monster Effect ] \nmon g h i j k l";
        var card = MakeCard("Pendulum Effect Monster", desc);
        const string completeErrata = "[Pendulum Effect] pend a b [Monster Effect] mon c d";

        var row = CardNormalizer.Normalize(card, SingleLoreErrata(completeErrata), wordLimit: 20);

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

        var row = CardNormalizer.Normalize(card, new CardErrata(shortErrata, desc, [new ErrataLore(shortErrata, null)]), wordLimit: 20);

        row.WordCount.ShouldBe(WordCounter.CountWords(shortErrata));
        row.IsEligible.ShouldBeTrue();
    }

    [Fact]
    public void Normalize_NoErrataPageWithTcgDate_EligibleSinceEqualsTcgDate()
    {
        var card = MakeCard("Effect Monster", "text", tcgDate: "2002-03-08");

        var row = CardNormalizer.Normalize(card, errata: null, wordLimit: 20);

        row.EligibleSince.ShouldBe(new DateOnly(2002, 3, 8));
    }

    [Fact]
    public void Normalize_NoErrataPageNullTcgDate_EligibleSinceIsNull()
    {
        var card = MakeCard("Effect Monster", "text", tcgDate: null);

        var row = CardNormalizer.Normalize(card, errata: null, wordLimit: 20);

        row.EligibleSince.ShouldBeNull();
    }

    [Fact]
    public void Normalize_NoErrataPageNullTcgDateWithEarliestSetDate_EligibleSinceFromSetDate()
    {
        var card = MakeCard("Normal Monster", "Flavour text.");

        var row = CardNormalizer.Normalize(card, errata: null, wordLimit: 20, earliestSetDate: new DateOnly(2002, 3, 8));

        row.EligibleSince.ShouldBe(new DateOnly(2002, 3, 8));
    }

    [Fact]
    public void Normalize_ErrataFirstLoreUnderLimit_EligibleSinceFromFirstLore()
    {
        var card = MakeCard("Effect Monster", string.Join(" ", Enumerable.Repeat("word", 30)), tcgDate: "2000-01-01");
        var errata = new CardErrata("short text", "short text", [new ErrataLore("short text", "2005-06-15")]);

        var row = CardNormalizer.Normalize(card, errata, wordLimit: 20);

        row.EligibleSince.ShouldBe(new DateOnly(2005, 6, 15));
    }

    [Fact]
    public void Normalize_ErrataWithFirstLoreOverLimit_EligibleSinceFromSecondLore()
    {
        var longText = string.Join(" ", Enumerable.Repeat("word", 30));
        var shortText = "Once per turn: Draw 1 card.";
        var card = MakeCard("Effect Monster", longText, tcgDate: "2000-01-01");
        var errata = new CardErrata(shortText, shortText,
        [
            new ErrataLore(longText, "2002-01-01"),
            new ErrataLore(shortText, "2010-05-20")
        ]);

        var row = CardNormalizer.Normalize(card, errata, wordLimit: 20);

        row.EligibleSince.ShouldBe(new DateOnly(2010, 5, 20));
    }

    [Fact]
    public void Normalize_SynchroErrataFirstLoreEffectUnderLimitWithStripMaterials_EligibleSinceFromFirstLore()
    {
        var card = MakeCard("Synchro Monster", "1 Tuner + 1 non-Tuner\nOriginal long effect " + string.Join(" ", Enumerable.Repeat("word", 20)), tcgDate: "2000-01-01");
        var shortEffect = "When this card is Synchro Summoned: Destroy up to 3 cards.";
        var lore0 = "1 Tuner + 1 non-Tuner " + shortEffect;
        var errata = new CardErrata(lore0, lore0, [new ErrataLore(lore0, "2009-11-05")]);

        var row = CardNormalizer.Normalize(card, errata, wordLimit: 20, stripMaterials: true);

        row.EligibleSince.ShouldBe(new DateOnly(2009, 11, 5));
    }

    [Fact]
    public void Normalize_ErrataFirstLoreUnderLimitNoDate_EligibleSinceEqualsTcgDate()
    {
        var card = MakeCard("Effect Monster", string.Join(" ", Enumerable.Repeat("word", 30)), tcgDate: "2002-03-08");
        var errata = new CardErrata("short", "short", [new ErrataLore("short", null)]);

        var row = CardNormalizer.Normalize(card, errata, wordLimit: 20);

        row.EligibleSince.ShouldBe(new DateOnly(2002, 3, 8));
    }

    [Fact]
    public void Normalize_PendulumEffectMalformedShortestLoreValidAlternativeExists_UsesShortestValidLore()
    {
        var desc = "[ Pendulum Effect ] \npend a b c d\n\n[ Monster Effect ] \nmon e f g h";
        var card = MakeCard("Pendulum Effect Monster", desc);
        const string malformed = "Pendulum Effect: pend a b c d";
        const string valid = "[Pendulum Effect] pend a b c d [Monster Effect] mon e f g h";

        var errata = new CardErrata(malformed, valid, [
            new ErrataLore(malformed, null),
            new ErrataLore(valid, null)
        ]);

        var row = CardNormalizer.Normalize(card, errata, wordLimit: 20);

        row.ShortestErrata.ShouldBe(valid);
        row.WordCount.ShouldBe(10);
    }

    [Fact]
    public void Normalize_PendulumEffectMalformedFirstLoreAppearsEligible_EligibleSinceIgnoresMalformed()
    {
        var pend = string.Join(" ", Enumerable.Repeat("p", 15));
        var mons = string.Join(" ", Enumerable.Repeat("m", 15));
        var desc = $"[Pendulum Effect] {pend} [Monster Effect] {mons}";
        var card = MakeCard("Pendulum Effect Monster", desc, tcgDate: "2000-01-01");

        const string malformed = "Monster Effect: a b c d e";
        var valid = $"[Pendulum Effect] {pend} [Monster Effect] {mons}";
        var errata = new CardErrata(malformed, valid, [
            new ErrataLore(malformed, "2005-06-15"),
            new ErrataLore(valid, "2020-01-01")
        ]);

        var row = CardNormalizer.Normalize(card, errata, wordLimit: 20);

        row.IsEligible.ShouldBeFalse();
        row.EligibleSince.ShouldBeNull();
    }
}
