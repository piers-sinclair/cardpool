using YgoDb.Cli.Api;

namespace YgoDb.Tests;

/// <summary>
/// Pre-fetches all card and errata data once before any test runs, then exposes
/// pre-computed NormalizedRow objects. This mirrors the Python tests.py approach:
/// all I/O happens sequentially upfront; individual test methods are pure assertions.
///
/// API calls made during InitializeAsync:
///   - N individual YGOProDeck requests (one per card)
///   - 1 batched Yugipedia request for all errata at once
/// </summary>
public sealed class IntegrationData : IAsyncLifetime
{
    // All test card names — order matches Python TEST_CASES + NO_MATERIALS_INTEGRATION
    private static readonly string[] AllNames =
    [
        "Ally of Justice Catastor", "Ryko, Lightsworn Hunter", "Raiza the Storm Monarch",
        "Tribe-Infecting Virus", "Blue-Eyes White Dragon", "Ash Blossom & Joyous Spring",
        "Catapult Turtle", "Magical Android", "Bujin Hiruko", "Timegazer Magician",
        "Luster Pendulum, the Dracoslayer",
        "Stardust Dragon", "Number 39: Utopia", "Elemental HERO Flame Wingman",
    ];

    /// <summary>Full-export rows (word limit 20, materials included).</summary>
    public IReadOnlyDictionary<string, NormalizedRow> Rows => _rows;
    private readonly Dictionary<string, NormalizedRow> _rows = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>No-materials rows for Extra Deck cards (materials stripped, word count recalculated).</summary>
    public IReadOnlyDictionary<string, NormalizedRow> NoMaterialsRows => _nmRows;
    private readonly Dictionary<string, NormalizedRow> _nmRows = new(StringComparer.OrdinalIgnoreCase);

    public async Task InitializeAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        var ygoDeck = new YgoProDeckClient(http);
        var yugipedia = new YugipediaClient(http);

        // 1. Fetch card data from YGOProDeck (individual requests — no bulk-by-name API)
        var cards = new Dictionary<string, YgoCard>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in AllNames)
        {
            var card = await ygoDeck.FetchCardByNameAsync(name);
            if (card is not null)
                cards[name] = card;
        }

        // 2. Single batched Yugipedia request for all cards that need errata
        //    (mirrors Python's fetch_errata_batch call before any assertions)
        var needsErrata = cards.Values
            .Where(c => CardNormalizer.NeedsErrataLookup(c, 20))
            .Select(c => c.Name)
            .ToList();
        var errataMap = await yugipedia.FetchErrataAsync(needsErrata);

        // 3. Normalize all cards — all subsequent test assertions are pure (no I/O)
        foreach (var (name, card) in cards)
        {
            errataMap.TryGetValue(name, out var errata);
            var row = CardNormalizer.Normalize(card, errata.Shortest, errata.Latest, 20);
            _rows[name] = row;

            // Pre-compute no-materials variant (clone so PostprocessRow doesn't mutate _rows)
            var nmRow = row.Clone();
            MaterialStripper.PostprocessRow(nmRow, 20);
            _nmRows[name] = nmRow;
        }

        ygoDeck.Dispose();
        yugipedia.Dispose();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

/// <summary>
/// Live-API regression tests mirroring tests.py. All I/O happens in IntegrationData.InitializeAsync;
/// individual test methods are synchronous pure assertions.
///
/// Run with: dotnet test --filter Category=Integration
/// </summary>
[Trait("Category", "Integration")]
public class ErrataIntegrationTests(IntegrationData data) : IClassFixture<IntegrationData>
{
    private NormalizedRow Row(string name) => data.Rows[name];
    private NormalizedRow NmRow(string name) => data.NoMaterialsRows[name];

    // ── Word count / eligibility regressions ──────────────────────────────

    [Fact]
    public void AllyOfJusticeCatastor_WordCount22_NotEligible()
    {
        // lore0 "after" skeleton was 20 words (garbled). Fix: skip _clean_lore(lore0).
        Row("Ally of Justice Catastor").WordCount.ShouldBe(22);
        Row("Ally of Justice Catastor").IsEligible.ShouldBeFalse();
    }

    [Fact]
    public void RykoLightsownHunter_WordCount20_Eligible()
    {
        // Initial print (LODT) is exactly 20 words.
        Row("Ryko, Lightsworn Hunter").WordCount.ShouldBe(20);
        Row("Ryko, Lightsworn Hunter").IsEligible.ShouldBeTrue();
    }

    [Fact]
    public void RaizaTheStormMonarch_WordCount19_Eligible()
    {
        // Short errata at 19 words (original print).
        Row("Raiza the Storm Monarch").WordCount.ShouldBe(19);
        Row("Raiza the Storm Monarch").IsEligible.ShouldBeTrue();
    }

    [Fact]
    public void TribeInfectingVirus_WordCount18_Eligible()
    {
        // Punctuation regression: ',' and ';' were stray tokens. get_text("") fix → 18 words.
        Row("Tribe-Infecting Virus").WordCount.ShouldBe(18);
        Row("Tribe-Infecting Virus").IsEligible.ShouldBeTrue();
    }

    [Fact]
    public void BlueEyesWhiteDragon_WordCount0_Eligible()
    {
        Row("Blue-Eyes White Dragon").WordCount.ShouldBe(0);
        Row("Blue-Eyes White Dragon").IsEligible.ShouldBeTrue();
    }

    [Fact]
    public void AshBlossomAndJoyousSpring_WordCount64_NotEligible()
    {
        Row("Ash Blossom & Joyous Spring").WordCount.ShouldBe(64);
        Row("Ash Blossom & Joyous Spring").IsEligible.ShouldBeFalse();
    }

    [Fact]
    public void CatapultTurtle_WordCount17_Eligible()
    {
        // Intermediate errata ≤20 words even though first (48w) and latest (23w) are >20.
        Row("Catapult Turtle").WordCount.ShouldBe(17);
        Row("Catapult Turtle").IsEligible.ShouldBeTrue();
    }

    [Fact]
    public void MagicalAndroid_WordCount22_NotEligible()
    {
        // _clean_lore skeleton was 20w (garbled). After _lore_full fix: 22w.
        Row("Magical Android").WordCount.ShouldBe(22);
        Row("Magical Android").IsEligible.ShouldBeFalse();
    }

    [Fact]
    public void BujinHiruko_WordCount61_NotEligible()
    {
        // Pendulum Normal: Pendulum Effect (61w) counted; monster portion is flavour.
        Row("Bujin Hiruko").WordCount.ShouldBe(61);
        Row("Bujin Hiruko").IsEligible.ShouldBeFalse();
    }

    [Fact]
    public void TimegazerMagician_WordCount70_NotEligible()
    {
        // Pendulum Effect Monster: both sections summed. All versions >20 words.
        Row("Timegazer Magician").WordCount.ShouldBe(70);
        Row("Timegazer Magician").IsEligible.ShouldBeFalse();
    }

    [Fact]
    public void LusterPendulumTheDracoslayer_NotEligible()
    {
        // Yugipedia stored only Monster Effect section → incomplete errata detected →
        // falls back to desc (both sections). Exact count is desc-version-dependent.
        Row("Luster Pendulum, the Dracoslayer").IsEligible.ShouldBeFalse();
    }

    // ── Errata fallback — no Yugipedia page ───────────────────────────────

    [Fact]
    public void BlueEyesWhiteDragon_NoErrataPage_FallsBackToDesc()
    {
        var row = Row("Blue-Eyes White Dragon");
        row.ShortestErrata.ShouldBe(row.Desc);
        row.LatestErrata.ShouldBe(row.Desc);
    }

    [Fact]
    public void BujinHiruko_NoErrataPage_FallsBackToDesc()
    {
        var row = Row("Bujin Hiruko");
        row.ShortestErrata.ShouldBe(row.Desc);
        row.LatestErrata.ShouldBe(row.Desc);
    }

    // ── No-materials integration ──────────────────────────────────────────

    [Theory]
    [InlineData("Stardust Dragon")]
    [InlineData("Number 39: Utopia")]
    [InlineData("Elemental HERO Flame Wingman")]
    public void ExtraDeckCard_NoMaterials_WordCountLessOrEqualToFull(string cardName)
    {
        NmRow(cardName).WordCount.ShouldBeLessThanOrEqualTo(Row(cardName).WordCount);
    }
}
