# Third-party licenses and attribution

[`third-party.json`](third-party.json) is the registry of everything in this repository that the project did not write: software dependencies, the engine and runtime, datasets and audiovisual assets. `python3 tools/validate_repo.py` enforces it on every PR (the `docs` check), so a change that adds, upgrades or removes one of these items without updating the registry fails. ADR-0003 requires that every dependency and asset allows redistribution on Windows, macOS and Linux.

This is engineering tracking, not legal advice. The final legal review (license texts in the product, store terms, signing and notarization) belongs to Phase 53.

## What must be recorded

| Section | Records | Matched against |
|---|---|---|
| `dependencies` | NuGet packages and MSBuild SDKs, plus components that are not NuGet packages: the Godot engine and the .NET runtime | `Directory.Packages.props`, `PackageReference` and `Sdk="Name/version"` in every `*.csproj`, and `packages.lock.json` under `src/` |
| `datasets` | Real-world or third-party data used to build or seed the simulation (terrain, climate, names, tables) | Reviewed by hand; none exist yet |
| `assets` | Images, audio, fonts, 3D models and video under `game/` and `content/` | Files by path; any file with an asset extension (svg, png, jpg, webp, ogg, wav, mp3, ttf, otf, woff2, glb and similar) without an entry fails |

Godot's generated `*.import` files and the `.godot/` cache are ignored. Original project art is recorded too, with license `LicenseRef-Project-Original`, so nothing in `game/` or `content/` is unaccounted for.

Record **direct** references for every scope. Record transitive packages only when they ship, which the validator checks through the lock files of the runtime projects under `src/`. Packages the Godot SDK brings in are recorded with `via` naming the SDK.

## Scopes

- **runtime**: ships inside a player build (engine, .NET runtime, GodotSharp, content, art). Needs a `copyright` line, and the build must carry the license notice. `python3 tools/ci/write_notices.py <file>` generates `THIRD-PARTY-NOTICES.txt` from the registry for the packaging smoke check.
- **build**: used to compile or analyze (analyzers, source generators, SDKs). Does not ship.
- **test**: used by tests only. Does not ship.

## Allowed licenses

Only SPDX identifiers listed in `allowed_licenses` in the registry are accepted. They are permissive: MIT, Apache-2.0, BSD-2-Clause, BSD-3-Clause, ISC, Zlib, Unlicense, CC0-1.0, plus CC-BY-4.0 and OFL-1.1 for assets (fonts, art, audio) that need visible attribution, and `LicenseRef-Project-Original` for work authored for this project.

Copyleft (GPL, AGPL, LGPL), share-alike (CC-BY-SA, ODbL), non-commercial or no-derivatives licenses, and items without a clear license are not allowed. Adding one needs an ADR first. For a dual-licensed item, record the license you elect and say so in `note`. Datasets with their own terms (for example government open data) are added to the list in the same reviewed change as the first dataset that needs them.

Assets and datasets under any license other than CC0-1.0, Unlicense or `LicenseRef-Project-Original` need an `attribution` text, which the notices file reproduces.

## Adding or changing an entry

1. Find the license in the package or asset's own repository or page, not in a third-party summary. Check that it is on the allowed list.
2. Add an object to the matching section of `third-party.json`:
   - dependency: `name`, `version`, `kind` (`nuget` or `component`), `scope`, `license`, `source` (URL), plus `copyright` for runtime items. Use `via` for packages brought in by an SDK, and `toolchain` (a dotted path into `tools/toolchain.json`) when the version is pinned there.
   - dataset: `name`, `license`, `source`, and `attribution` unless CC0/Unlicense.
   - asset: `path` (repository-relative; a trailing `/` covers a whole folder with one license), `license`, `source` (URL, or `original`), `author`, and `attribution` unless CC0/Unlicense/project-original.
3. Keep the entry in step with the code: an upgraded package needs the new `version`, and a removed one needs its entry removed. The validator reports both a missing entry and a stale one.
4. Run `python3 tools/validate_repo.py`.

Redistributing the engine also requires Godot's `LICENSE.txt` and its `COPYRIGHT.txt` (third-party components inside the engine) to ship with the build. Phase 01 packaging only proves the build runs with a generated notices file next to it; bundling the full texts is Phase 53 work.
