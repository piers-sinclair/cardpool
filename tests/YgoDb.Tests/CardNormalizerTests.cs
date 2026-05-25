using YgoDb.Cli.Models;

namespace YgoDb.Tests;

public class CardNormalizerTests
{
    private static YgoCard MakeCard(string type, string desc) =>
        new(Id: 1, Name: "Test", Type: type, Race: null, Attribute: null,
            Level: null, Atk: null, Def: null, Scale: null, LinkVal: null,
            LinkMarkers: null, Archetype: null, Desc: desc,
            CardSets: null, BanlistInfo: null, CardImages: null);

    [Fact]
    public void Normalize_PendulumTunerEffectMonstersOnlyErrata_FallsBackToDesc()
    {
        var desc = "[ Pendulum Effect ] \npend a b\n\n[ Monster Effect ] \nmon d e f";
        var card = MakeCard("Pendulum Tuner Effect Monster", desc);

        const string incompleteErrata = "Monster Effect: mon d e f";

        var row = CardNormalizer.Normalize(card, incompleteErrata, incompleteErrata, wordLimit: 20);

        row.WordCount.ShouldBe(7);
        row.IsEligible.ShouldBeTrue();
    }

    [Fact]
    public void Normalize_PendulumTunerEffectPendulumOnlyErrata_FallsBackToDesc()
    {
        var desc = "[ Pendulum Effect ] \npend a b\n\n[ Monster Effect ] \nmon d e f";
        var card = MakeCard("Pendulum Tuner Effect Monster", desc);

        const string incompleteErrata = "[Pendulum Effect] pend a b";

        var row = CardNormalizer.Normalize(card, incompleteErrata, incompleteErrata, wordLimit: 20);

        row.WordCount.ShouldBe(7);
    }

    [Fact]
    public void Normalize_PendulumEffectCompleteErrata_UsesErrata()
    {
        var desc = "[ Pendulum Effect ] \npend a b c d e f\n\n[ Monster Effect ] \nmon g h i j k l";
        var card = MakeCard("Pendulum Effect Monster", desc);

        const string completeErrata = "[Pendulum Effect] pend a b [Monster Effect] mon c d";

        var row = CardNormalizer.Normalize(card, completeErrata, completeErrata, wordLimit: 20);

        row.WordCount.ShouldBe(6);
    }

    [Fact]
    public void Normalize_PendulumNormalNoErrataPage_UsesDesc()
    {
        var desc = "[ Pendulum Effect ] \npend a b c\n\n[ Monster Effect ] \nFlavour only.";
        var card = MakeCard("Pendulum Normal Monster", desc);

        var row = CardNormalizer.Normalize(card, null, null, wordLimit: 20);

        row.WordCount.ShouldBe(4);
    }
}
