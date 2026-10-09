# ADR-0005 — Toolchain pins, repository layout and build policy

**Status:** Accepted on merge of its introducing PR (SOV-P01-T01, T02, T06).
**Decision date:** 2026-10-09
**Scope:** Exact runtime and dependency versions, project layout, build configurations and the content data area. It fills in the Phase 01 pins that ADR-0001 deferred; ADR-0001's architecture is unchanged.

## Context

ADR-0001 chose Godot 4 .NET for the client and an independent C#/.NET simulation, leaving exact versions for Phase 01 to pin with clean-build evidence. ADR-0003 targets Windows x64, macOS arm64 and Linux x64. ADR-0004 requires deterministic simulation code. Phase 01 needs one reproducible way to build, test, launch and package.

## Decision

### Pins

| Item | Pin | Where it is enforced |
|---|---|---|
| .NET SDK | 10.0.401, roll forward to later patches of the 10.0.4xx band only | `global.json`; CI installs exactly this version |
| Target framework | `net10.0` (LTS) for every project | `Directory.Build.props`, and explicitly in `game/Sovereigns.Client.csproj` so the Godot editor does not insert its own default |
| Godot | 4.7.2 stable, .NET (mono) build | `tools/toolchain.json`, with SHA-512 values for the editor archives and export templates |
| Godot C# SDK | `Godot.NET.Sdk/4.7.2`, which pins GodotSharp and Godot.SourceGenerators to the same version | `game/Sovereigns.Client.csproj` |
| NuGet packages | Exact versions via central package management | `Directory.Packages.props`, plus `packages.lock.json` per project, restored in locked mode in CI |
| Package source | nuget.org only | `NuGet.config` clears machine feeds |
| Python tooling | 3.11 or newer, standard library only | `tools/doctor.py` |

The Godot client has no lock file: Godot.NET.Sdk adds GodotSharpEditor only in editor configurations, so one lock file cannot describe both `Debug` and `ExportRelease`. The SDK version pins its packages exactly.

Godot 4.7.2 loads `net10.0` assemblies; this was verified locally on 2026-10-09 (runtime 10.0.12) and is re-verified by the CI client job.

### Layout

| Area | Responsibility |
|---|---|
| `src/Sovereigns.Simulation/` | Authoritative simulation library. References only the .NET base library and analyzers. |
| `tests/Sovereigns.Simulation.Tests/` | xUnit v3 tests of the simulation and the headless runner; run with `dotnet test`, no Godot needed. |
| `tests/fixtures/` | Versioned reference fixtures (seed plus scenario path). |
| `tools/Sovereigns.Headless/` | `sovereigns-headless`: runs fixtures and scenarios, replays failure reports, validates content. |
| `tools/*.py`, `tools/ci/` | Repository validation, bootstrap checks and CI helper scripts. |
| `content/` | Versioned read-only content definitions, one JSON file per definition at `content/<namespace>/<kind>/<name>.json`. |
| `game/` | Godot project: rendering, input, UI and the development inspection shell. |
| `docs/legal/` | Third-party license and attribution registry. |

`Sovereigns.slnx` holds all .NET projects. The solution's `Release` configuration builds the client as `ExportRelease`.

### Build configurations and warnings

- Every configuration: nullable reference types are on, and nullable warnings are errors. In the simulation, banned nondeterministic APIs (`BannedSymbols.txt`: wall-clock time, `System.Random`, `Guid.NewGuid`, `string.GetHashCode`) are errors.
- `Debug` and `ExportDebug` check arithmetic overflow. Other analyzer and style warnings are reported but do not fail the build, so local iteration stays fast.
- `Release`, `ExportRelease` and every CI build (`CI=true`) treat all compiler, analyzer and code-style warnings as errors. CI also runs `dotnet format --verify-no-changes`.
- Analyzers run at `AnalysisLevel` latest with the `Recommended` rule set, and code style comes from `.editorconfig`.

### Content data

A definition's C# record is its schema. Loading uses strict JSON: unknown, missing, mistyped or null-for-non-nullable members are errors. The `id` must equal the path-derived `namespace:kind/name` (ADR-0004 §5). Each kind adds semantic checks and declares its references. A reference must exist and have the expected kind, and unknown IDs get a nearest-match suggestion. Every error names the file, line or JSON path, and the fix. `sovereigns-headless validate-content` runs the same loader as the game.

### Renderer

The client uses Godot's Compatibility renderer (OpenGL 3). It covers the 2D orthographic view on the widest range of desktop drivers, and it can run under software rendering in CI. Revisit when measured rendering needs exceed it (Phases 46 and 51).

## Alternatives considered

- **.NET 8 LTS:** older and supported by Godot, but .NET 10 LTS is installed, supported until 2028 and runs with Godot 4.7.2. Choosing .NET 8 would mean an upgrade within the project's life.
- **One lock file for the client:** impossible across Godot's configurations without disabling editor builds; the exact SDK pin gives the same reproducibility.
- **JSON Schema files for content:** they are better for external editors, but validation in .NET needs a third-party library, and the records would still have to be written. Revisit with Phase 50's modding pipeline.
- **MSTest or NUnit:** equivalent capability. xUnit v3 (the established 3.x line) was chosen for familiarity; any upgrade is a deliberate change.
- **Forward+ or Mobile renderer:** more features, which a debug-graphics 2D viewer does not need, and fewer supported drivers.

## Consequences

- Updating the SDK band, Godot or any package is a reviewed change to these pins, the toolchain file, the lock files and `docs/legal/third-party.json`.
- `tools/doctor.py` reports any mismatch between a developer machine and these pins.
- A client-only change still compiles in the `simulation` CI job, because the client is in the solution. Running it needs the Godot `client` job.
