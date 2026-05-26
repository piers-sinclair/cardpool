namespace CardPool.Tests;

public class MaterialStripperTests
{
    [Fact]
    public void StripMaterialLine_MultiLine_RemovesFirstLine()
    {
        var text = "1 Tuner + 1 non-Tuner\nOnce per turn, you can draw 1 card.";
        MaterialStripper.StripMaterialLine(text).ShouldBe("Once per turn, you can draw 1 card.");
    }

    [Fact]
    public void StripMaterialLine_SingleLineSynchro_RemovesMaterial()
    {
        var text = "1 Tuner + 1 non-Tuner If this card attacks a Defense Position monster, inflict piercing battle damage.";
        var result = MaterialStripper.StripMaterialLine(text);
        result.ShouldStartWith("If");
    }

    [Fact]
    public void StripMaterialLine_SingleLineXyz_RemovesMaterial()
    {
        var text = "2 Level 4 monsters Once per turn: You can detach 1 material from this card; draw 1 card.";
        var result = MaterialStripper.StripMaterialLine(text);
        result.ShouldStartWith("Once");
    }

    [Fact]
    public void StripMaterialLine_SingleLineFusion_RemovesMaterial()
    {
        var text = "\"Elemental HERO\" monster + 1 FIRE monster Must be Fusion Summoned. When this card destroys an opponent's monster by battle: Inflict damage equal to that monster's original ATK.";
        var result = MaterialStripper.StripMaterialLine(text);
        result.ShouldStartWith("Must");
    }

    [Fact]
    public void StripMaterialLine_NoMaterialIndicator_ReturnsOriginal()
    {
        var text = "Once per turn: Draw 1 card.";
        MaterialStripper.StripMaterialLine(text).ShouldBe(text);
    }

    [Fact]
    public void StripMaterialLine_MaterialOnlyText_ReturnsEmpty()
    {
        var text = "2 Level 4 monsters";
        MaterialStripper.StripMaterialLine(text).ShouldBe(string.Empty);
    }

    [Fact]
    public void StripMaterialLine_AquaDragonNormalFusionMaterials_ReturnsEmpty()
    {
        var text = "\"Fairy Dragon\" + \"Amazon of the Seas\" + \"Zone Eater\"";
        MaterialStripper.StripMaterialLine(text).ShouldBe(string.Empty);
    }

    [Fact]
    public void StripMaterialLine_BerserkerOfTenyiLinkMaterial_ReturnsEmpty()
    {
        var text = "2+ monsters, including a Link Monster";
        MaterialStripper.StripMaterialLine(text).ShouldBe(string.Empty);
    }

    [Fact]
    public void StripMaterialLine_EmptyString_ReturnsEmpty() =>
        MaterialStripper.StripMaterialLine("").ShouldBe("");

    [Fact]
    public void PostprocessRow_SynchroCard_StripsAndRecalculates()
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
        result.Materials.ShouldBe("1 Tuner + 1 non-Tuner");
        result.WordCount.ShouldBe(WordCounter.CountEffectiveWords("Once per turn, you can draw 1 card.", "Synchro Monster"));
    }

    [Fact]
    public void PostprocessRow_SingleLineSynchroCard_SetsMaterials()
    {
        var row = new NormalizedRow
        {
            Type = "Synchro Monster",
            Desc = "1 Tuner + 1 non-Tuner If this card attacks a Defense Position monster, inflict piercing battle damage.",
            ShortestErrata = "1 Tuner + 1 non-Tuner If this card attacks a Defense Position monster, inflict piercing battle damage.",
            LatestErrata = "1 Tuner + 1 non-Tuner If this card attacks a Defense Position monster, inflict piercing battle damage.",
            WordCount = 99
        };

        var result = MaterialStripper.PostprocessRow(row, 20);

        result.Materials.ShouldBe("1 Tuner + 1 non-Tuner");
    }

    [Fact]
    public void PostprocessRow_SingleLineFusionCard_SetsMaterials()
    {
        var row = new NormalizedRow
        {
            Type = "Fusion Monster",
            Desc = "\"Elemental HERO\" monster + 1 FIRE monster Must be Fusion Summoned. When this card destroys an opponent's monster by battle: Inflict damage equal to that monster's original ATK.",
            ShortestErrata = "\"Elemental HERO\" monster + 1 FIRE monster Must be Fusion Summoned. When this card destroys an opponent's monster by battle: Inflict damage equal to that monster's original ATK.",
            LatestErrata = "\"Elemental HERO\" monster + 1 FIRE monster Must be Fusion Summoned. When this card destroys an opponent's monster by battle: Inflict damage equal to that monster's original ATK.",
            WordCount = 99
        };

        var result = MaterialStripper.PostprocessRow(row, 20);

        result.Materials.ShouldBe("\"Elemental HERO\" monster + 1 FIRE monster");
    }

    [Fact]
    public void PostprocessRow_SingleLineXyzCard_SetsMaterials()
    {
        var row = new NormalizedRow
        {
            Type = "XYZ Monster",
            Desc = "2 Level 4 monsters Once per turn: You can detach 1 material from this card; draw 1 card.",
            ShortestErrata = "2 Level 4 monsters Once per turn: You can detach 1 material from this card; draw 1 card.",
            LatestErrata = "2 Level 4 monsters Once per turn: You can detach 1 material from this card; draw 1 card.",
            WordCount = 99
        };

        var result = MaterialStripper.PostprocessRow(row, 20);

        result.Materials.ShouldBe("2 Level 4 monsters");
    }

    [Fact]
    public void PostprocessRow_SpellCard_Unchanged()
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
        result.Materials.ShouldBeNull();
        result.WordCount.ShouldBe(5);
    }

    [Fact]
    public void PostprocessRow_EffectMonster_Unchanged()
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

    [Fact]
    public void PostprocessRow_AquaDragonNormalFusion_StripsToEmptyAndZeroWords()
    {
        var formula = "\"Fairy Dragon\" + \"Amazon of the Seas\" + \"Zone Eater\"";
        var row = new NormalizedRow
        {
            Type = "Fusion Monster",
            Desc = formula,
            ShortestErrata = formula,
            LatestErrata = formula,
            WordCount = 99
        };

        var result = MaterialStripper.PostprocessRow(row, 20);

        result.Desc.ShouldBe(string.Empty);
        result.WordCount.ShouldBe(0);
        result.IsEligible.ShouldBeTrue();
    }

    [Fact]
    public void PostprocessRow_BerserkerOfTenyiLinkMaterial_StripsToEmptyAndZeroWords()
    {
        var formula = "2+ monsters, including a Link Monster";
        var row = new NormalizedRow
        {
            Type = "Link Monster",
            Desc = formula,
            ShortestErrata = formula,
            LatestErrata = formula,
            WordCount = 99
        };

        var result = MaterialStripper.PostprocessRow(row, 20);

        result.Desc.ShouldBe(string.Empty);
        result.WordCount.ShouldBe(0);
        result.IsEligible.ShouldBeTrue();
    }
}
