# YgoDb (.NET) — Claude Code Context

YgoDb is a .NET 10 port of the Python ygodb tool. It fetches ~12,000 Yu-Gi-Oh! cards from
YGOProDeck, enriches them with errata history from Yugipedia, applies word-count rules, and
exports results to Excel/CSV. Used for Edison format play to identify cards eligible under the ≤N
words rule.

---

## Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 (LTS) |
| App type | Console application — CLI batch tool, **not** a web API |
| CLI parsing | System.CommandLine (preview) |
| HTML parsing | AngleSharp — `TextContent` = BeautifulSoup `get_text("")` |
| Excel output | ClosedXML (MIT licence) — **not** EPPlus (non-commercial licence) |
| CSV output | CsvHelper |
| HTTP | `HttpClient` (BCL) + `System.Text.Json` |
| Tests | xUnit + Shouldly + NSubstitute |

---

## Repo structure

```
src/
  YgoDb.Cli/
    Api/                  ← YgoProDeckClient, YugipediaClient (HTTP + rate limiting)
    Parsing/              ← WikitextParser (wikitext→lore), HtmlErrataScraper (inspect cmd)
    WordCount/            ← WordCounter (pure logic, no I/O)
    Pipeline/             ← ExportPipeline, CardNormalizer, MaterialStripper
    Export/               ← ExcelExporter (ClosedXML), CsvExporter (CsvHelper)
    Models/               ← YgoCard (API model), NormalizedRow (output model)
    GlobalUsings.cs       ← global usings for the project
    Program.cs            ← System.CommandLine entry point
tests/
  YgoDb.Tests/
    WordCounterTests.cs         ← unit tests, no network
    MaterialStripperTests.cs    ← unit tests, no network
    ErrataIntegrationTests.cs   ← live API tests (Category=Integration)
    GlobalUsings.cs
output/                   ← generated Excel/CSV files (git-ignored)
Directory.Build.props     ← shared MSBuild properties (TreatWarningsAsErrors, etc.)
global.json               ← pins .NET SDK version to 10.x
```

---

## CLI commands

```bash
# Export (single command with options replaces 6 Python entry scripts)
dotnet run --project src/YgoDb.Cli -- export                         # full, ≤20 words
dotnet run --project src/YgoDb.Cli -- export --words 25              # full, ≤25 words
dotnet run --project src/YgoDb.Cli -- export --words 30              # full, ≤30 words
dotnet run --project src/YgoDb.Cli -- export --no-materials          # strip Extra Deck, ≤20w; keeps stripped materials in a separate column
dotnet run --project src/YgoDb.Cli -- export --no-materials --words 25

# --extra-deck: which Extra Deck types to include (all|none|fusion|synchro|xyz|link, repeatable)
dotnet run --project src/YgoDb.Cli -- export --extra-deck none        # main deck cards only
dotnet run --project src/YgoDb.Cli -- export --extra-deck fusion synchro   # fusion + synchro only
dotnet run --project src/YgoDb.Cli -- export --no-materials --extra-deck synchro xyz

# Inspect a single card (replaces inspect_card.py)
dotnet run --project src/YgoDb.Cli -- inspect "Raiza the Storm Monarch"

# Tests
dotnet test tests/YgoDb.Tests --filter "Category!=Integration"       # unit tests only (fast)
dotnet test tests/YgoDb.Tests --filter "Category=Integration"        # live API tests (~5 min)
dotnet test tests/YgoDb.Tests                                        # all tests
```

---

## Key conventions

### Word counting — must match Python exactly

```csharp
// Correct equivalent of Python len(text.split()):
text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length

// Wrong — does not collapse multiple spaces or strip ends:
text.Split(' ').Length
```

### HTML parsing — the `get_text("")` equivalence

Python's BeautifulSoup `get_text("")` (empty separator) is critical: punctuation inside `<ins>`/`<del>` tags must not become a stray token. AngleSharp's `element.TextContent` is the exact equivalent — it concatenates child text with no separator inserted between nodes.  
**Always use `TextContent`; never `InnerHtml` or a space-separated text getter.**

### Wikitext errata parsing

Yugipedia stores `<del>removed</del>` and `<ins>added</ins>` diff markup. **Both tags must be unwrapped** (call `ReplaceWith(ChildNodes)` on each), not stripped. Stripping either tag gives garbled or incomplete text.

### Japanese lore filtering

Some Yugipedia errata pages (e.g. OCG-only cards like Treasure Map) have an `== English ==` section that contains only Japanese lore text. Both `WikitextParser.ExtractEnglishLoresAsync` and `HtmlErrataScraper.ParseErrataTableAsync` discard any lore string containing Hiragana, Katakana, or Kanji. When all lore versions are filtered the caller receives an empty list and falls back to the YGOProDeck `desc`.

### Card type filtering — Tokens and Skill Cards

`ExportPipeline` filters out non-playable card types before normalization using `CardTypeExtensions.IsToken()` and `CardTypeExtensions.IsSkillCard()`. Tokens (`type = "Token"`) and Skill Cards (`type = "Skill Card"`) are excluded from all exports.

### Incomplete Pendulum errata detection

Yugipedia sometimes stores only one section (`[Pendulum Effect]` or `[Monster Effect]`) for Pendulum Effect Monsters. `CardNormalizer.Normalize` detects this (`hasPend != hasMons`) and falls back to the YGOProDeck `desc`, which always has both sections.

### Rate limiting

`YugipediaClient` enforces 100ms minimum between requests (10 req/s ceiling) using a `SemaphoreSlim(1,1)` lock. The 20 parallel worker cap is enforced in `ExportPipeline` with a separate `SemaphoreSlim(20)`.

### Licensing policy

All NuGet packages must be **MIT, Apache 2.0, BSD-2, BSD-3, ISC, or equivalent** (free for commercial closed-source use).
- **Do not use EPPlus** — v5+ is Polyform Non-Commercial. ClosedXML (MIT) is the Excel library.
- Before adding a new package, verify its licence and add it with a comment if unusual.

---

## Code quality

All projects inherit from `Directory.Build.props`:
- `TreatWarningsAsErrors` — every warning is a build failure
- `Nullable enable` — null-safety enforced throughout
- `ImplicitUsings enable` — SDK usings included automatically
- `AnalysisLevel latest-Recommended` — Roslyn analyser rules enabled
- `EnforceCodeStyleInBuild` — code style violations fail the build

**Build must pass with 0 warnings before any commit.**

**No comments** — do not write inline or block comments anywhere in the codebase. Well-named identifiers convey intent; a comment is only justified for a non-obvious external constraint, invariant, or workaround that naming alone cannot express. When in doubt, omit the comment.

---

## External APIs

| Service | Base URL | Notes |
|---|---|---|
| YGOProDeck | `https://db.ygoprodeck.com/api/v7/cardinfo.php` | No auth; 60s timeout for bulk fetch |
| Yugipedia | `https://yugipedia.com/api.php` | Requires `User-Agent` header; 30s timeout; rate-limited to 10 req/s |

---

## Game rules context

Edison format (September 2010 banlist). The ≤N-word rule: if any printing of a card has ≤N words
in its effect text, the card may be played as written in that printing. The default threshold is 20.
See the Python ygodb `CLAUDE.md` for the full word-counting specification.

---

## Testing strategy

- **Unit tests** (`WordCounterTests`, `MaterialStripperTests`, `CardNormalizerTests`) — pure logic, no network, always run in CI.
- **Integration tests** (`ErrataIntegrationTests`, `[Trait("Category","Integration")]`) — hit live APIs;
  assert exact word counts for 11 known-tricky cards (same card list as Python `tests.py`).
- Both suites run in CI (`.github/workflows/ci.yml`).

### Test naming convention

All test methods follow `Method_Scenario_ExpectedBehaviour`:

- **Method** — the public method or feature under test (e.g. `CountEffectiveWords`, `Normalize`, `PostprocessRow`)
- **Scenario** — the input or setup condition (e.g. `PendulumNormal_SpaceInBracketFormat`, `NoErrataPage`)
- **ExpectedBehaviour** — the observable assertion result, not an implementation detail (e.g. `Returns0Words`, `ErrataEqualsDesc`, not `UsesFallback`)

Examples: `CountEffectiveWords_NormalMonster_ReturnsZero`, `Normalize_BlueEyesWhiteDragonNoErrataPage_ErrataEqualsDesc`

---

## Keeping this file up to date

Update `CLAUDE.md` whenever you:
- Add a new class, command, or significant algorithm
- Change an external API URL, timeout, or rate limit
- Introduce or swap a NuGet dependency
- Change a code-quality setting in `Directory.Build.props`
