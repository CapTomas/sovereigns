# Development setup

How to get a machine ready to build, test and launch Sovereigns, and how to run locally what CI runs. The pins come from [ADR-0005](../architecture/ADR-0005-toolchain-and-repository-layout.md); the machine-readable copy is [`tools/toolchain.json`](../../tools/toolchain.json), and `python3 tools/doctor.py` checks a machine against it.

## What to install

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | **10.0.401**, or a later patch of the same feature band (10.0.4xx) | `global.json` pins it. A newer feature band (10.0.5xx) is not accepted. |
| Godot | **4.7.2 stable, .NET (mono) build** | The standard (non-.NET) build cannot run the C# client. Verify the download against the SHA-512 in `tools/toolchain.json`. |
| Python | 3.11 or newer | Repository tooling only; standard library only. |
| git | any recent version | |

The Godot export templates (same version, .NET flavor) are needed only to package a build.

### macOS (Apple Silicon)

1. Install the .NET SDK from <https://dotnet.microsoft.com/download/dotnet/10.0> (arm64 installer), or `./dotnet-install.sh --version 10.0.401`.
2. Download `Godot_v4.7.2-stable_mono_macos.universal.zip` from the release linked in `tools/toolchain.json`, check `shasum -a 512 <file>` against the pinned value, and move `Godot_mono.app` to `/Applications`.
3. Expose it to the tools: `export SOVEREIGNS_GODOT=/Applications/Godot_mono.app/Contents/MacOS/Godot`, and for the commands below use `godot` as a shell alias or substitute `"$SOVEREIGNS_GODOT"`.
4. Python and git: `xcode-select --install` provides git; get Python 3.11+ from python.org or Homebrew.

### Windows (x64)

1. Install the .NET SDK x64 from the same page, or `dotnet-install.ps1 -Version 10.0.401`.
2. Download `Godot_v4.7.2-stable_mono_win64.zip`, check `Get-FileHash -Algorithm SHA512 <file>` against the pinned value, and unzip it. Use the `_console.exe` binary for headless runs, because it prints to the terminal.
3. Set `SOVEREIGNS_GODOT` to the full path of that executable (or put its folder on `PATH` as `godot`).
4. Install Python 3.11+ and git for Windows. The commands below use bash syntax; run them in Git Bash, or translate `export` to `$env:`.

### Linux (x64)

1. Install the .NET SDK with Microsoft's package feed or `./dotnet-install.sh --version 10.0.401`.
2. Install the pinned editor the way CI does, with checksum verification: `bash tools/ci/install_godot.sh ~/.cache/sovereigns/godot`, then add `~/.cache/sovereigns/godot/bin` to `PATH` (add `--templates` for packaging; that downloads 1.2 GB once).
3. Python 3.11+ and git come from the distribution.

### Check the machine

```
python3 tools/doctor.py
```

Each line is `ok`, `warn` or `FAIL`, with a fix for anything not ok. Missing export templates are only a warning. The exit code is 1 when a required tool is missing or does not match the pin.

## Run the CI checks locally

CI sets `CI=true`, which restores NuGet packages in locked mode and turns every warning into an error. Set it to see exactly what CI sees; leave it unset for faster, more forgiving local iteration. Commands run from the repository root.

### Simulation (runs on Ubuntu, Windows and macOS in CI)

```bash
export CI=true
dotnet restore Sovereigns.slnx
dotnet format Sovereigns.slnx --verify-no-changes --no-restore
dotnet build Sovereigns.slnx -c Release --no-restore
dotnet test Sovereigns.slnx -c Release --no-build --logger trx --results-directory artifacts/test-results
dotnet run --project tools/Sovereigns.Headless -c Release --no-build -- validate-content
python3 tools/ci/check_determinism.py -- dotnet run --project tools/Sovereigns.Headless -c Release --no-build -- run --fixture F-EMPTY
```

The last command runs the `F-EMPTY` fixture twice and fails if the checksum, simulated time or step count differ. In CI each operating system also saves its result with `--write`. The `determinism` job then compares all three with `check_determinism.py --compare`, which is the cross-platform measurement ADR-0004 §3 asks for. To fix a formatting failure run `dotnet format Sovereigns.slnx` (without `--verify-no-changes`).

### Repository documentation checks (the `docs` check)

```bash
python3 tools/validate_repo.py
python3 -m unittest discover -s tools/tests -v
```

These also enforce the license registry and the toolchain pins; see [`docs/legal`](../legal/README.md).

### Godot client (the `client` check, Linux in CI)

```bash
godot --headless --path game --import
godot --headless --path game --build-solutions --quit
python3 tools/ci/run_checked.py --log artifacts/logs/client-tests.log -- godot --headless --path game res://tests/ClientTests.tscn
python3 tools/ci/run_checked.py --log artifacts/logs/shell-smoke.log \
  --require "world created" --require "shutdown" \
  --forbid "leaked at exit" --forbid "ObjectDB instances leaked" --forbid "still in use at exit" --forbid "ERROR:" \
  -- godot --headless --verbose --path game --quit-after 300 -- --fixture F-EMPTY
```

`run_checked.py` keeps the combined output in the named log and fails on a non-zero exit code, on forbidden text, or on missing required text. Use `"$SOVEREIGNS_GODOT"` where this page says `godot` if it is not on `PATH`.

### Packaging smoke (Linux, every PR and mainline change)

```bash
bash tools/ci/install_godot.sh ~/.cache/sovereigns/godot --templates
mkdir -p artifacts/package/linux
python3 tools/ci/run_checked.py --log artifacts/logs/export.log \
  --forbid "completed with warnings" --forbid 'Project export for preset "Linux" failed' \
  -- godot --headless --path game --export-release "Linux" "$PWD/artifacts/package/linux/sovereigns.x86_64"
cp -R content artifacts/package/linux/content
python3 tools/ci/write_notices.py artifacts/package/linux/THIRD-PARTY-NOTICES.txt
python3 tools/ci/run_checked.py --log artifacts/logs/package-smoke.log --require "world created" --require "shutdown" --forbid "ERROR:" \
  -- artifacts/package/linux/sovereigns.x86_64 --headless --quit-after 120 -- --scenario core:scenario/empty_world --seed 20261009
```

Godot publishes with a runtime identifier (`-r linux-x64`). The simulation project lists every export runtime identifier, so its lock file covers them and locked restore still works. Add a new export platform's identifier to `RuntimeIdentifiers` in `src/Sovereigns.Simulation/Sovereigns.Simulation.csproj` and refresh the lock file. The export presets live in `game/export_presets.cfg` (Linux, Windows Desktop, macOS). Only Linux is packaged in CI. Windows and macOS exports need their templates and, for macOS, `rendering/textures/vram_compression/import_etc2_astc` enabled in the project; signing and notarization are Phase 53 work.

## Where logs and failure reports go

Everything generated locally lands under `artifacts/`, which git ignores.

| Path | Content |
|---|---|
| `artifacts/reports/` | Failure reports written by the headless runner when a simulation run fails. Each holds the seed, scenario, commands and state checksum needed to reproduce the failure. |
| `artifacts/logs/` | Combined output of the Godot runs started through `run_checked.py`. |
| `artifacts/test-results/` | TRX files from `dotnet test`. |
| `artifacts/package/` | Packaged builds. |

When a CI job fails or finishes, it uploads `artifacts/` as a GitHub Actions artifact (`simulation-<os>`, `client-failure`, `package-failure`; the Linux package is `package-linux` and is kept for 3 days). Download it from the run page.

To rehearse the failure path, inject a failure and replay the report it writes:

```bash
dotnet run --project tools/Sovereigns.Headless -c Release --no-build -- run --fixture F-EMPTY --fail-at-ms 60000
dotnet run --project tools/Sovereigns.Headless -c Release --no-build -- replay artifacts/reports/<report file>.json
```

The first command exits 1 and names the report; the second rebuilds the world from the report and compares the checksum.

## Required checks

A pull request needs `docs`, `simulation (ubuntu-24.04)`, `simulation (windows-2025)`, `simulation (macos-15)`, `determinism`, `client` and `package` to pass. The same jobs run again on every push to `main`. The required list is in `.github/rulesets/protect-main.json`, which must be applied to GitHub (`gh api --method PUT repos/CapTomas/sovereigns/rulesets/<id> --input .github/rulesets/protect-main.json`) after the first `build.yml` run has produced the check names; see [repository enforcement](../process/REPOSITORY_ENFORCEMENT.md).

## Refreshing pins

Change pins deliberately, in one reviewed PR, and run `python3 tools/validate_repo.py` before pushing. The validator rejects most partial updates.

- **.NET SDK:** edit `global.json` and `tools/toolchain.json` `dotnet.sdk` to the same version. CI installs whatever `global.json` says. Update ADR-0005's pin table.
- **NuGet packages:** edit the version in `Directory.Packages.props`, regenerate lock files with `dotnet restore Sovereigns.slnx --force-evaluate` (with `CI` unset), commit the changed `packages.lock.json` files, and update the entry in `docs/legal/third-party.json`.
- **Godot:** take the new editor and template file names and SHA-512 values from the release's `SHA512-SUMS.txt`, update every URL and hash in `tools/toolchain.json`, the `Godot.NET.Sdk/<version>` in `game/Sovereigns.Client.csproj`, the `config/features` version in `game/project.godot`, and the Godot entries in `docs/legal/third-party.json`. The CI cache key includes `tools/toolchain.json`, so the new editor is downloaded once on the next run.
- **GitHub Actions:** pin by full commit SHA with the version in a comment. Resolve a tag with `gh api repos/<owner>/<repo>/git/ref/tags/<tag>` (dereference annotated tags through `git/tags/<sha>`).

## Inspection shell

```sh
godot --path game -- --fixture F-EMPTY
```

This opens the development inspection shell on the seeded empty world. Selection by scenario and seed, the inspection steps and every option are in [`game/README.md`](../../game/README.md). Logs, failure reports and replay are in [DIAGNOSTICS.md](DIAGNOSTICS.md).
