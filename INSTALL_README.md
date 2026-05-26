# CardPool — Installation

## Windows

Run the installer (no .NET required):

```powershell
.\install.ps1
```

Or right-click `install.ps1` and choose **Run with PowerShell**.

> If PowerShell blocks the script: `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`

Open a **new terminal** — `cpool` is now on your PATH.

To uninstall: run `.\uninstall.ps1` the same way.

---

## macOS / Linux

Run the installer (no .NET required):

```bash
bash install.sh
```

Open a **new terminal** — `cpool` is now on your PATH.

To uninstall:

```bash
bash uninstall.sh
```

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
