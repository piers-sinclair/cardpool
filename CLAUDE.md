# CardPool (.NET) — Claude Code Context

CardPool is a card pool analysis tool for trading card games — designed to support multiple TCGs,
with Yu-Gi-Oh! as the first implementation. It fetches ~12,000 Yu-Gi-Oh! cards from YGOProDeck,
enriches them with errata history from Yugipedia, applies word-count rules, and exports results to
Excel/CSV. Used for Edison format play to identify cards eligible under the ≤N words rule.

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
  CardPool.Cli/
    Api/                  ← YgoProDeckClient, YugipediaClient (HTTP + rate limiting)
    Parsing/              ← WikitextParser (wikitext→lore), HtmlErrataScraper (inspect cmd), LoreEntry
    WordCount/            ← WordCounter (pure logic, no I/O)
    Pipeline/             ← CardPoolExporter, CardNormalizer, MaterialStripper, YgoCardExtensions
    Export/               ← CardPoolExcelExporter, CardPoolCsvExporter, ReleaseNotesExcelExporter,
                            ReleaseNotesCsvExporter, ExcelFormatter (ClosedXML), CsvFormatter (CsvHelper)
    Models/               ← YgoCard (API model), NormalizedRow (output model), CardErrata,
                            CardTypeExtensions, StringExtensions
    AppConstants.cs       ← shared constants (DefaultWordLimit, IsoDateFormat)
    GlobalUsings.cs       ← global usings for the project
    Program.cs            ← System.CommandLine entry point
tests/
  CardPool.Tests/
    WordCounterTests.cs               ← unit tests, no network
    MaterialStripperTests.cs          ← unit tests, no network
    CardNormalizerTests.cs            ← unit tests, no network
    CardPoolCsvExporterTests.cs       ← unit tests, no network
    ReleaseNotesExcelExporterTests.cs ← unit tests, no network
    GlobalUsings.cs
output/                   ← generated Excel/CSV files (git-ignored)
Directory.Build.props     ← shared MSBuild properties (TreatWarningsAsErrors, etc.)
global.json               ← pins .NET SDK version to 10.x
```

---

## CLI commands

```bash
# If installed as a global tool or self-contained exe:
cpool export
cpool export --words 25
cpool inspect "Raiza the Storm Monarch"

# Run from source (development):
dotnet run --project src/CardPool.Cli -- export                                        # default: ≤25 words, strip-materials, exclude pendulum link
dotnet run --project src/CardPool.Cli -- export --words 25
dotnet run --project src/CardPool.Cli -- export --words 30
dotnet run --project src/CardPool.Cli -- export --words -1                             # no word limit — export all cards
dotnet run --project src/CardPool.Cli -- export --strip-materials false                   # include full material text

# --exclude-types: exclude cards whose type contains any of these fragments (case-insensitive, repeatable)
# default is "pendulum link"; use "none" to include all types
dotnet run --project src/CardPool.Cli -- export --exclude-types none                   # include all card types
dotnet run --project src/CardPool.Cli -- export --exclude-types pendulum               # include Link, exclude Pendulum
dotnet run --project src/CardPool.Cli -- export --exclude-types fusion synchro xyz link # main deck only
dotnet run --project src/CardPool.Cli -- export --exclude-types pendulum link flip tuner

# --errata-mode: which text version to evaluate for eligibility
dotnet run --project src/CardPool.Cli -- export --errata-mode latest                   # current text only (fast — no Yugipedia fetch)
dotnet run --project src/CardPool.Cli -- export --errata-mode shortest                 # any historical printing (default)

# --output: custom output directory
dotnet run --project src/CardPool.Cli -- export --output ~/ygo

# --since: generate release notes alongside the export for cards eligible on or after this date
dotnet run --project src/CardPool.Cli -- export --since 2025-01-01

# Inspect a single card
dotnet run --project src/CardPool.Cli -- inspect "Raiza the Storm Monarch"

# Tests
dotnet test tests/CardPool.Tests
```

---

## Distribution

The CLI is packaged as both a **.NET Global Tool** and a **self-contained single-file executable**.

### Global Tool

```bash
dotnet pack src/CardPool.Cli -c Release -o dist/
dotnet tool install -g CardPool --add-source dist/
```

The `.nupkg` is produced by `<PackAsTool>true</PackAsTool>` in the csproj. PackageId is `CardPool`, command is `cpool`.

### Self-contained executables

Five publish profiles live in `src/CardPool.Cli/Properties/PublishProfiles/`:

| Profile | Platform |
|---------|----------|
| `win-x64.pubxml` | Windows x64 |
| `win-arm64.pubxml` | Windows ARM64 |
| `osx-arm64.pubxml` | macOS ARM64 (Apple Silicon) |
| `osx-x64.pubxml` | macOS x64 (Intel) |
| `linux-x64.pubxml` | Linux x64 |

```bash
dotnet publish src/CardPool.Cli -p:PublishProfile=win-x64 -o dist/win-x64
```

All profiles use `PublishSingleFile=true` and `SelfContained=true`. `PublishTrimmed` is intentionally omitted — AngleSharp, ClosedXML, and CsvHelper use reflection and are not trim-safe.

### Sharing without repo access

Windows bundle (recipients run `install.ps1`):

```powershell
dotnet publish src/CardPool.Cli -p:PublishProfile=win-x64 -o dist/win-x64
Compress-Archive -Path dist/win-x64/cpool.exe, install.ps1, uninstall.ps1, INSTALL_README.md -DestinationPath cpool-win-x64.zip
```

macOS/Linux bundle (recipients run `bash install.sh`):

```bash
dotnet publish src/CardPool.Cli -p:PublishProfile=osx-arm64 -o dist/osx-arm64
zip -j cpool-osx-arm64.zip dist/osx-arm64/cpool install.sh uninstall.sh INSTALL_README.md
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

Yugipedia sometimes stores only one section (`[Pendulum Effect]` or `[Monster Effect]`) for Pendulum Effect Monsters. `YgoCardExtensions.GetCardErrata` detects this (`hasPend != hasMons`) and falls back to the YGOProDeck `desc`, which always has both sections.

### Rate limiting

`YugipediaClient` enforces 100ms minimum between requests (10 req/s ceiling) using a `SemaphoreSlim(1,1)` lock. The 20 parallel worker cap is enforced in `ExportPipeline` with a separate `SemaphoreSlim(20)`.

### Licensing policy

All NuGet packages must be **MIT, Apache 2.0, BSD-2, BSD-3, ISC, or equivalent** (free for commercial closed-source use).
- **Do not use EPPlus** — v5+ is Polyform Non-Commercial. ClosedXML (MIT) is the Excel library.
- Before adding a new package, verify its licence and add it with a comment if unusual.

---

## Shell conventions

Always use the **Bash tool** for terminal commands. Only fall back to PowerShell when the operation is genuinely Windows-specific and has no Bash equivalent (e.g. `Compress-Archive`, registry edits). Multi-command chains should use `&&` and POSIX syntax.

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
in its effect text, the card may be played as written in that printing. The default threshold is 25.
See the Python ygodb `CLAUDE.md` for the full word-counting specification.

---

## Testing strategy

- **Unit tests** (`WordCounterTests`, `MaterialStripperTests`, `CardNormalizerTests`) — pure logic, no network, run in CI.

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
