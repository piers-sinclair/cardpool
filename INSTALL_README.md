# CardPool — Installation

## Install

1. Right-click `install.ps1` and choose **Run with PowerShell** (or run it in a terminal).
2. Open a **new terminal** — `cpool` is now on your PATH.

> If PowerShell blocks the script, run once: `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`

---

## Usage

```
cpool export                              # default: <=25 words, no-materials, no-link, no-pendulum
cpool export --words 20                   # <=20 words
cpool export --no-materials false         # include full material text in word count
cpool export --extra-deck all             # include all Extra Deck types (including Link)
cpool export --extra-deck none            # main-deck cards only
cpool export --extra-deck fusion synchro  # Fusion + Synchro only
cpool export --no-pendulum false          # include Pendulum monsters
cpool export --words 30 --output ~/ygo    # <=30 words, custom output dir

cpool inspect "Raiza the Storm Monarch"   # show errata history for a single card
cpool inspect "Dark Magician"

cpool --help                              # global help
cpool export --help                       # export option reference
cpool inspect --help                      # inspect argument reference
```

Output files (`*.xlsx` and `*.csv`) are written to `./output/` (or the directory given to `--output`).

---

## Default export settings

| Option | Default | Description |
|--------|---------|-------------|
| `--words` | `25` | Word-count threshold |
| `--no-materials` | `true` | Strip material requirements from Extra Deck monsters |
| `--extra-deck` | `fusion synchro xyz` | Extra Deck types included (Link excluded by default) |
| `--no-pendulum` | `true` | Exclude Pendulum monsters |
| `--output` | `./output` | Output directory |

---

## Uninstall

Right-click `uninstall.ps1` and choose **Run with PowerShell**, or run it in a terminal.
