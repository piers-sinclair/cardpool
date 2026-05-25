# ygodb

Yu-Gi-Oh! card database tool for Edison format play. Fetches ~12,000 cards from [YGOProDeck](https://ygoprodeck.com/), enriches them with errata history from [Yugipedia](https://yugipedia.com/), and exports to Excel/CSV.

The core purpose: identify cards whose **shortest known errata version** falls within a word-count threshold (default: ≤20 words), making them eligible under Edison format's word-count rule regardless of current oracle text.

---

## Scripts

| Script | Purpose | Output | Runtime |
|--------|---------|--------|---------|
| `export_full.py` | Full export at **≤20 words**. Fetches all cards with Yugipedia errata data. | `output/full_export.xlsx/.csv` | ~2–4 min |
| `export_no_materials.py` | Same as `export_full` but strips the material requirement line from Extra Deck monsters. Word counts reflect effect text only. | `output/no_materials_export.xlsx/.csv` | ~2–4 min |
| `export_25words.py` | Full export at **≤25 words**. | `output/full_25words_export.xlsx/.csv` | ~2–4 min |
| `export_no_materials_25words.py` | No-materials export at **≤25 words**. | `output/no_materials_25words_export.xlsx/.csv` | ~2–4 min |
| `export_30words.py` | Full export at **≤30 words**. | `output/full_30words_export.xlsx/.csv` | ~2–4 min |
| `export_no_materials_30words.py` | No-materials export at **≤30 words**. | `output/no_materials_30words_export.xlsx/.csv` | ~2–4 min |
| `inspect_card.py` | Single-card errata inspector. Prints all English errata versions with word counts, then saves a summary. | `output/{CardName}_erratas.xlsx/.csv` | Seconds |
| `core.py` | Shared logic used by the export scripts. Not run directly. | — | — |
| `tests.py` | Regression test suite verifying errata parsing and word counting against 11 known-tricky cards. | — | ~30 sec |

---

## Usage

```bash
python export_full.py
python export_no_materials.py
python export_25words.py
python export_no_materials_25words.py
python export_30words.py
python export_no_materials_30words.py
python inspect_card.py "Blue-Eyes White Dragon"
python tests.py
```

All outputs go to the `output/` directory (created automatically).

To use a custom word-count threshold from code, pass `word_limit` to `run_export`:

```python
from core import run_export
run_export("output/custom.xlsx", "output/custom.csv", word_limit=35)
```

---

## Output Columns

All export scripts produce these columns:

| Column | Description |
|--------|-------------|
| `id`, `name`, `type`, `race`, `attribute` | Core card identity |
| `level`, `atk`, `def`, `scale`, `linkval`, `linkmarkers` | Stats |
| `archetype` | Card archetype |
| `desc` | Current oracle text (from YGOProDeck) |
| `shortest_errata` | The errata version with the fewest words; falls back to `desc` if no Yugipedia page exists |
| `latest_errata` | The most recent errata version; falls back to `desc` |
| `word_count` | Effective word count of `shortest_errata`, applying game counting rules |
| `is_eligible` | `True` if `word_count` ≤ threshold — eligibility flag |
| `set_name`, `set_code`, `set_rarity` | First card set from YGOProDeck |
| `ban_tcg`, `ban_ocg` | Current banlist status |
| `image_url` | Card artwork URL |

The Excel workbook has two sheets: `<=N Words` and `>N Words` (where N is the threshold).

---

## Word Counting Rules

Word count uses `len(text.split())` — whitespace-separated tokens.

| Card Type | What's Counted |
|-----------|----------------|
| Normal Monster (no Pendulum) | 0 — all text is flavour |
| Pendulum Normal Monster | Pendulum Effect box only |
| Pendulum Effect Monster | Pendulum Effect box + Monster Effect box |
| All others (Effect Monsters, Spells, Traps) | Full description text |

The **shortest errata** is selected by fewest raw words across all errata versions; ties go to the latest version. `word_count` is then the card-type-adjusted count of that text.

---

## Edison Format Context

**Edison format** uses the September 2010 banlist.

**The ≤20-word rule:** If any printing of a card (pre-errata, typo print, etc.) has ≤20 words in its effect text, the card may be played as that printing is written — regardless of what the current oracle text says.

**Key Edison rulings:**
- **Damage Step priority**: Fast effects (spell speed 2+) may be activated during the Damage Step without needing to chain.
- **Turn player priority**: The turn player may activate ignition effects at the start of a new phase or after a chain resolves before the opponent can respond.

---

## Dependencies

```
pip install requests pandas openpyxl beautifulsoup4
```
