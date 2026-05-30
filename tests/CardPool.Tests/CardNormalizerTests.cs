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
    public void Normalize_NullTcgDateWithMatchingSetDates_EligibleSinceFromEarliestSet()
    {
        var card = new YgoCard(Id: 1, Name: "Test", Type: "Normal Monster", Race: null, Attribute: null,
            Level: null, Atk: null, Def: null, Scale: null, LinkVal: null, LinkMarkers: null,
            Archetype: null, Desc: "flavour text",
            CardSets: [new CardSet("Legend of Blue Eyes White Dragon", "LOB", "Common")],
            BanlistInfo: null, CardImages: null, MiscInfo: null);
        var setDates = new Dictionary<string, string>
        {
            ["Legend of Blue Eyes White Dragon"] = "2002-03-08"
        };

        var row = CardNormalizer.Normalize(card, errata: null, wordLimit: 20, setDates: setDates);

        row.EligibleSince.ShouldBe(new DateOnly(2002, 3, 8));
    }

    [Fact]
    public void Normalize_NullTcgDateMultipleSets_EligibleSinceFromEarliestSet()
    {
        var card = new YgoCard(Id: 1, Name: "Test", Type: "Normal Monster", Race: null, Attribute: null,
            Level: null, Atk: null, Def: null, Scale: null, LinkVal: null, LinkMarkers: null,
            Archetype: null, Desc: "flavour text",
            CardSets:
            [
                new CardSet("Dark Beginning 1", "DB1", "Common"),
                new CardSet("Legend of Blue Eyes White Dragon", "LOB", "Common")
            ],
            BanlistInfo: null, CardImages: null, MiscInfo: null);
        var setDates = new Dictionary<string, string>
        {
            ["Dark Beginning 1"] = "2004-11-01",
            ["Legend of Blue Eyes White Dragon"] = "2002-03-08"
        };

        var row = CardNormalizer.Normalize(card, errata: null, wordLimit: 20, setDates: setDates);

        row.EligibleSince.ShouldBe(new DateOnly(2002, 3, 8));
    }

    [Fact]
    public void Normalize_NullTcgDateNoMatchingSetDates_EligibleSinceIsNull()
    {
        var card = new YgoCard(Id: 1, Name: "Test", Type: "Normal Monster", Race: null, Attribute: null,
            Level: null, Atk: null, Def: null, Scale: null, LinkVal: null, LinkMarkers: null,
            Archetype: null, Desc: "flavour text",
            CardSets: [new CardSet("UnknownSet", "UNK", "Common")],
            BanlistInfo: null, CardImages: null, MiscInfo: null);
        var setDates = new Dictionary<string, string>
        {
            ["Legend of Blue Eyes White Dragon"] = "2002-03-08"
        };

        var row = CardNormalizer.Normalize(card, errata: null, wordLimit: 20, setDates: setDates);

        row.EligibleSince.ShouldBeNull();
    }

    [Fact]
    public void Normalize_ErrataLoreNoDateNullTcgDateWithSetDates_EligibleSinceFromSet()
    {
        var card = new YgoCard(Id: 1, Name: "Test", Type: "Effect Monster", Race: null, Attribute: null,
            Level: null, Atk: null, Def: null, Scale: null, LinkVal: null, LinkMarkers: null,
            Archetype: null, Desc: string.Join(" ", Enumerable.Repeat("word", 30)),
            CardSets: [new CardSet("Set A", "SA", "Common")],
            BanlistInfo: null, CardImages: null, MiscInfo: null);
        var setDates = new Dictionary<string, string>
        {
            ["Set A"] = "2003-07-10"
        };
        var errata = new CardErrata("short", "short", [new ErrataLore("short", null)]);

        var row = CardNormalizer.Normalize(card, errata, wordLimit: 20, setDates: setDates);

        row.EligibleSince.ShouldBe(new DateOnly(2003, 7, 10));
    }
}
