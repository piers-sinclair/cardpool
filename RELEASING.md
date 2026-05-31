# Releasing

CardPool is distributed via NuGet, winget, Homebrew, and GitHub Releases. The entire pipeline is automated — the only manual step is bumping the version number.

## How to release

1. Update `<Version>` in `src/CardPool.Cli/CardPool.Cli.csproj`
2. Commit and merge to `main`

That's it. The pipeline detects the version change and handles everything else automatically.

## What happens automatically

Merging a version bump triggers `release.yml`, which:

1. Checks if a GitHub Release for that version already exists — skips if so (safe to re-run)
2. Cross-compiles all five platform binaries
3. Creates a GitHub Release tagged `v<version>` with the binaries attached
4. Pushes the NuGet package to nuget.org

Once the binaries are built and the GitHub Release is created, two further jobs run in the same workflow:

- **`winget`** — opens a pull request on [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) with the correct installer manifest and real SHA256 hashes. Microsoft's bot reviews and merges this within a few days, after which `winget install PiersSinclair.CardPool` works.
- **`homebrew`** — copies the formula template from `packaging/homebrew/cpool.rb`, stamps in real SHA256 hashes, and pushes to the [piers-sinclair/homebrew-cpool](https://github.com/piers-sinclair/homebrew-cpool) tap. `brew install cpool` works immediately after.

## Post-release verification

Homebrew is verified automatically: `smoke-test.yml` triggers after `release.yml` completes and installs `cpool` from the tap on both macOS and Linux.

Winget requires a manual check once the winget-pkgs PR is merged:

1. Go to **Actions → Package Manager Smoke Test → Run workflow**
2. Tick **"Test winget install"** and run
3. Confirm the job passes

## Required secrets

All three secrets are set at **https://github.com/piers-sinclair/cardpool/settings/secrets/actions**.

| Secret | Used by | What it needs |
|--------|---------|---------------|
| `NUGET_API_KEY` | NuGet push | API key scoped to the `CardPool` package |
| `WINGET_TOKEN` | winget PR submission | Classic PAT — `public_repo` scope only |
| `HOMEBREW_TAP_TOKEN` | Homebrew tap push | Fine-grained PAT — `homebrew-cpool` repo, Contents read/write |

### Renewing `NUGET_API_KEY`

1. Go to **https://www.nuget.org/account/apikeys**
2. Find the existing key → **Regenerate** (or create a new one scoped to `CardPool`)
3. Paste the new value into the `NUGET_API_KEY` secret

### Renewing `WINGET_TOKEN`

1. Go to **https://github.com/settings/tokens** (classic tokens)
2. Find `winget-releaser` → **Regenerate**, keeping scope: `public_repo` only
3. Paste the new value into the `WINGET_TOKEN` secret

### Renewing `HOMEBREW_TAP_TOKEN`

1. Go to **https://github.com/settings/personal-access-tokens** (fine-grained tokens)
2. Find `homebrew-tap-releaser` → **Regenerate**
   - Repository access: `homebrew-cpool` only
   - Permission: **Contents** → Read and write
3. Paste the new value into the `HOMEBREW_TAP_TOKEN` secret

> If a release workflow fails with a 401 or 403 error, an expired secret is the most likely cause.

## Homebrew formula

`packaging/homebrew/cpool.rb` in this repository is the **single source of truth** for the Homebrew formula. Never edit [piers-sinclair/homebrew-cpool](https://github.com/piers-sinclair/homebrew-cpool) directly.

On each release, the `homebrew` job in `release.yml` copies the full formula file from this repo into the tap and replaces the placeholder SHA256 values with real hashes. Any change to the formula — description, install logic, test, URL pattern — belongs in `packaging/homebrew/cpool.rb` here and takes effect on the next release.

## Packaging reference files

| Path | Purpose |
|------|---------|
| `packaging/homebrew/cpool.rb` | Homebrew formula template (source of truth) |
| `packaging/winget/manifests/…/` | Winget manifest reference for the current version — the `winget-releaser` action generates the real submission automatically |
