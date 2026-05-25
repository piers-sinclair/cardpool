using YgoDb.Cli.Models;

namespace YgoDb.Tests;

public class MaterialStripperTests
{
    // ── StripMaterialLine ─────────────────────────────────────────────────

    [Fact]
    public void Strip_MultiLine_RemovesFirstLine()
    {
        var text = "1 Tuner + 1 non-Tuner\nOnce per turn, you can draw 1 card.";
        MaterialStripper.StripMaterialLine(text).ShouldBe("Once per turn, you can draw 1 card.");
    }

    [Fact]
    public void Strip_SingleLineSynchro_RemovesMaterial()
    {
        var text = "1 Tuner + 1 non-Tuner If this card attacks a Defense Position monster, inflict piercing battle damage.";
        var result = MaterialStripper.StripMaterialLine(text);
        result.ShouldStartWith("If");
    }

    [Fact]
    public void Strip_SingleLineXyz_RemovesMaterial()
    {
        var text = "2 Level 4 monsters Once per turn: You can detach 1 material from this card; draw 1 card.";
        var result = MaterialStripper.StripMaterialLine(text);
        result.ShouldStartWith("Once");
    }

    [Fact]
    public void Strip_SingleLineFusion_RemovesMaterial()
    {
        var text = "\"Elemental HERO\" monster + 1 FIRE monster Must be Fusion Summoned. When this card destroys an opponent's monster by battle: Inflict damage equal to that monster's original ATK.";
        var result = MaterialStripper.StripMaterialLine(text);
        result.ShouldStartWith("Must");
    }

    [Fact]
    public void Strip_NoMaterialIndicator_ReturnsOriginal()
    {
        var text = "Once per turn: Draw 1 card.";
        MaterialStripper.StripMaterialLine(text).ShouldBe(text);
    }

    [Fact]
    public void Strip_MaterialOnlyText_ReturnsOriginal()
    {
        var text = "2 Level 4 monsters";
        MaterialStripper.StripMaterialLine(text).ShouldBe(text);
    }

    [Fact]
    public void Strip_EmptyString_ReturnsEmpty() =>
        MaterialStripper.StripMaterialLine("").ShouldBe("");

    // ── PostprocessRow ────────────────────────────────────────────────────

    [Fact]
    public void Postprocess_SynchroCard_StripsAndRecalculates()
    {
        var row = new NormalizedRow
        {
            Type = "Synchro Monster",
            Desc = "1 Tuner + 1 non-Tuner\nOnce per turn, you can draw 1 card.",
            ShortestErrata = "1 Tuner + 1 non-Tuner\nOnce per turn, you can draw 1 card.",
            LatestErrata = "1 Tuner + 1 non-Tuner\nOnce per turn, you can draw 1 card.",
            WordCount = 99
        };

        var result = MaterialStripper.PostprocessRow(row, 20);

        result.Desc.ShouldBe("Once per turn, you can draw 1 card.");
        result.ShortestErrata.ShouldBe("Once per turn, you can draw 1 card.");
        result.WordCount.ShouldBe(WordCounter.CountEffectiveWords("Once per turn, you can draw 1 card.", "Synchro Monster"));
    }

    [Fact]
    public void Postprocess_SpellCard_Unchanged()
    {
        var original = "Discard 1 card; draw 2 cards.";
        var row = new NormalizedRow
        {
            Type = "Spell Card",
            Desc = original,
            ShortestErrata = original,
            LatestErrata = original,
            WordCount = 5
        };

        var result = MaterialStripper.PostprocessRow(row, 20);
        result.Desc.ShouldBe(original);
        result.WordCount.ShouldBe(5);
    }

    [Fact]
    public void Postprocess_EffectMonster_Unchanged()
    {
        var original = "Once per turn: Draw 1 card.";
        var row = new NormalizedRow
        {
            Type = "Effect Monster",
            Desc = original,
            ShortestErrata = original,
            LatestErrata = original,
            WordCount = 6
        };

        var result = MaterialStripper.PostprocessRow(row, 20);
        result.Desc.ShouldBe(original);
    }
}
