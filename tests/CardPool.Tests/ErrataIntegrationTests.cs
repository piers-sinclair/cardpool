namespace CardPool.Tests;

public sealed class IntegrationData : IAsyncLifetime
{
    private static readonly string[] AllNames =
    [
        "Ally of Justice Catastor", "Ryko, Lightsworn Hunter", "Raiza the Storm Monarch",
        "Tribe-Infecting Virus", "Blue-Eyes White Dragon", "Ash Blossom & Joyous Spring",
        "Catapult Turtle", "Magical Android", "Bujin Hiruko", "Timegazer Magician",
        "Luster Pendulum, the Dracoslayer",
        "Stardust Dragon", "Number 39: Utopia", "Elemental HERO Flame Wingman",
        "Aqua Dragon", "Berserker of the Tenyi",
        "Treasure Map",
    ];

    public IReadOnlyDictionary<string, NormalizedRow> Rows => _rows;
    private readonly Dictionary<string, NormalizedRow> _rows = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, NormalizedRow> NoMaterialsRows => _nmRows;
    private readonly Dictionary<string, NormalizedRow> _nmRows = new(StringComparer.OrdinalIgnoreCase);

    public async Task InitializeAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        var ygoDeck = new YgoProDeckClient(http);
        var yugipedia = new YugipediaClient(http);

        var cards = new Dictionary<string, YgoCard>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in AllNames)
        {
            var card = await ygoDeck.FetchCardByNameAsync(name);
            if (card is not null)
                cards[name] = card;
        }

        var needsErrata = cards.Values
            .Where(c => CardNormalizer.NeedsErrataLookup(c, 20))
            .Select(c => c.Name)
            .ToList();
        var errataMap = await yugipedia.FetchErrataAsync(needsErrata);

        foreach (var (name, card) in cards)
        {
            errataMap.TryGetValue(name, out var errata);
            var row = CardNormalizer.Normalize(card, errata.Shortest, errata.Latest, 20);
            _rows[name] = row;

            var nmRow = row.Clone();
            MaterialStripper.PostprocessRow(nmRow, 20);
            _nmRows[name] = nmRow;
        }

        ygoDeck.Dispose();
        yugipedia.Dispose();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

[Trait("Category", "Integration")]
public class ErrataIntegrationTests(IntegrationData data) : IClassFixture<IntegrationData>
{
    private NormalizedRow Row(string name) => data.Rows[name];
    private NormalizedRow NmRow(string name) => data.NoMaterialsRows[name];

    [Fact]
    public void Normalize_AllyOfJusticeCatastor_Returns22Words()
    {
        Row("Ally of Justice Catastor").WordCount.ShouldBe(22);
        Row("Ally of Justice Catastor").IsEligible.ShouldBeFalse();
    }

    [Fact]
    public void Normalize_RykoLightsownHunter_Returns20Words()
    {
        Row("Ryko, Lightsworn Hunter").WordCount.ShouldBe(20);
        Row("Ryko, Lightsworn Hunter").IsEligible.ShouldBeTrue();
    }

    [Fact]
    public void Normalize_RaizaTheStormMonarch_Returns19Words()
    {
        Row("Raiza the Storm Monarch").WordCount.ShouldBe(19);
        Row("Raiza the Storm Monarch").IsEligible.ShouldBeTrue();
    }

    [Fact]
    public void Normalize_TribeInfectingVirus_Returns18Words()
    {
        Row("Tribe-Infecting Virus").WordCount.ShouldBe(18);
        Row("Tribe-Infecting Virus").IsEligible.ShouldBeTrue();
    }

    [Fact]
    public void Normalize_BlueEyesWhiteDragon_Returns0Words()
    {
        Row("Blue-Eyes White Dragon").WordCount.ShouldBe(0);
        Row("Blue-Eyes White Dragon").IsEligible.ShouldBeTrue();
    }

    [Fact]
    public void Normalize_AshBlossomAndJoyousSpring_Returns64Words()
    {
        Row("Ash Blossom & Joyous Spring").WordCount.ShouldBe(64);
        Row("Ash Blossom & Joyous Spring").IsEligible.ShouldBeFalse();
    }

    [Fact]
    public void Normalize_CatapultTurtle_Returns17Words()
    {
        Row("Catapult Turtle").WordCount.ShouldBe(17);
        Row("Catapult Turtle").IsEligible.ShouldBeTrue();
    }

    [Fact]
    public void Normalize_MagicalAndroid_Returns22Words()
    {
        Row("Magical Android").WordCount.ShouldBe(22);
        Row("Magical Android").IsEligible.ShouldBeFalse();
    }

    [Fact]
    public void Normalize_BujinHiruko_Returns61Words()
    {
        Row("Bujin Hiruko").WordCount.ShouldBe(61);
        Row("Bujin Hiruko").IsEligible.ShouldBeFalse();
    }

    [Fact]
    public void Normalize_TimegazerMagician_Returns70Words()
    {
        Row("Timegazer Magician").WordCount.ShouldBe(70);
        Row("Timegazer Magician").IsEligible.ShouldBeFalse();
    }

    [Fact]
    public void Normalize_LusterPendulumTheDracoslayer_NotEligible()
    {
        Row("Luster Pendulum, the Dracoslayer").IsEligible.ShouldBeFalse();
    }

    [Fact]
    public void Normalize_BlueEyesWhiteDragonNoErrataPage_ErrataEqualsDesc()
    {
        var row = Row("Blue-Eyes White Dragon");
        row.ShortestErrata.ShouldBe(row.Desc);
        row.LatestErrata.ShouldBe(row.Desc);
    }

    [Fact]
    public void Normalize_BujinHirukoNoErrataPage_ErrataEqualsDesc()
    {
        var row = Row("Bujin Hiruko");
        row.ShortestErrata.ShouldBe(row.Desc);
        row.LatestErrata.ShouldBe(row.Desc);
    }

    [Fact]
    public void Normalize_TreasureMapJapaneseLoreOnlyCard_FallsBackToDescWith38Words()
    {
        var row = Row("Treasure Map");
        row.WordCount.ShouldBe(38);
        row.IsEligible.ShouldBeFalse();
        row.ShortestErrata.ShouldBe(row.Desc);
    }

    [Theory]
    [InlineData("Stardust Dragon")]
    [InlineData("Number 39: Utopia")]
    [InlineData("Elemental HERO Flame Wingman")]
    public void PostprocessRow_ExtraDeckCardNoMaterials_ReducesOrPreservesWordCount(string cardName)
    {
        NmRow(cardName).WordCount.ShouldBeLessThanOrEqualTo(Row(cardName).WordCount);
    }

    [Fact]
    public void PostprocessRow_AquaDragonNormalFusion_ZeroWordsEligible()
    {
        NmRow("Aqua Dragon").WordCount.ShouldBe(0);
        NmRow("Aqua Dragon").IsEligible.ShouldBeTrue();
    }

    [Fact]
    public void PostprocessRow_BerserkerOfTenyiLinkMaterialOnly_ZeroWordsEligible()
    {
        NmRow("Berserker of the Tenyi").WordCount.ShouldBe(0);
        NmRow("Berserker of the Tenyi").IsEligible.ShouldBeTrue();
    }
}
