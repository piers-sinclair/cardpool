using YgoDb.Cli.Models;

namespace YgoDb.Tests;

public class CardNormalizerTests
{
    private static YgoCard MakeCard(string type, string desc) =>
        new(Id: 1, Name: "Test", Type: type, Race: null, Attribute: null,
            Level: null, Atk: null, Def: null, Scale: null, LinkVal: null,
            LinkMarkers: null, Archetype: null, Desc: desc,
            CardSets: null, BanlistInfo: null, CardImages: null);

    // ── Incomplete Pendulum errata detection ──────────────────────────────
    // Regression: CardNormalizer was checking Contains("Pendulum Effect") which
    // missed "Pendulum Tuner Effect Monster" (where "Tuner" sits between the words).
    // Fix: matches Python — "Pendulum" in type AND "Normal" not in type.

    [Fact]
    public void Normalize_PendulumTunerEffect_IncompleteMonstersOnlyErrata_FallsBackToDesc()
    {
        // Errata has only Monster Effect section (no Pendulum Effect marker) — must fall back to desc
        var desc = "[ Pendulum Effect ] \npend a b\n\n[ Monster Effect ] \nmon d e f";
        var card = MakeCard("Pendulum Tuner Effect Monster", desc);

        // shortestErrata has only Monster Effect: prefix (no Pendulum Effect marker)
        const string incompleteErrata = "Monster Effect: mon d e f";

        var row = CardNormalizer.Normalize(card, incompleteErrata, incompleteErrata, wordLimit: 20);

        // Should use desc (both sections) not the incomplete errata
        // desc: "pend a b" (3) + "mon d e f" (4) = 7 words → eligible
        row.WordCount.ShouldBe(7);
        row.IsEligible.ShouldBeTrue();
    }

    [Fact]
    public void Normalize_PendulumTunerEffect_IncompletePendulumOnlyErrata_FallsBackToDesc()
    {
        var desc = "[ Pendulum Effect ] \npend a b\n\n[ Monster Effect ] \nmon d e f";
        var card = MakeCard("Pendulum Tuner Effect Monster", desc);

        // shortestErrata has only Pendulum Effect section (no Monster Effect marker)
        const string incompleteErrata = "[Pendulum Effect] pend a b";

        var row = CardNormalizer.Normalize(card, incompleteErrata, incompleteErrata, wordLimit: 20);

        // Falls back to desc — both sections counted: 3 + 4 = 7
        row.WordCount.ShouldBe(7);
    }

    [Fact]
    public void Normalize_PendulumEffect_CompleteErrata_UsesErrata()
    {
        var desc = "[ Pendulum Effect ] \npend a b c d e f\n\n[ Monster Effect ] \nmon g h i j k l";
        var card = MakeCard("Pendulum Effect Monster", desc);

        // Complete errata — both sections present
        const string completeErrata = "[Pendulum Effect] pend a b [Monster Effect] mon c d";

        var row = CardNormalizer.Normalize(card, completeErrata, completeErrata, wordLimit: 20);

        // Errata used: "pend a b" (3) + "mon c d" (3) = 6 words (not desc 12)
        row.WordCount.ShouldBe(6);
    }

    [Fact]
    public void Normalize_PendulumNormal_NoErrataPage_UsesDesc()
    {
        var desc = "[ Pendulum Effect ] \npend a b c\n\n[ Monster Effect ] \nFlavour only.";
        var card = MakeCard("Pendulum Normal Monster", desc);

        // null errata → falls back to desc
        var row = CardNormalizer.Normalize(card, null, null, wordLimit: 20);

        // Only Pendulum Effect counted: "pend a b c" = 4 words
        row.WordCount.ShouldBe(4);
    }
}
