# Godot client and inspection shell

Godot 4.7.2 .NET client ([ADR-0005](../docs/architecture/ADR-0005-toolchain-and-repository-layout.md)). It renders, accepts input and presents state. It owns no terrain, weather, armies or clock of its own. Everything it shows comes from one authoritative `Sovereigns.Simulation.World` ([ADR-0001](../docs/architecture/ADR-0001-runtime.md)).

Phase 01 delivers the reusable development **inspection shell**: a 2D orthographic map with pan and zoom, a layer list, a location inspector and a debug HUD. The graphics are nonfinal debug graphics. Physical field families are listed but **unavailable**, each with its owner and the phase that implements it. The shell never draws invented values.

## Launch

From the repository root, with Godot on `PATH` as `godot` ([setup](../docs/development/SETUP.md)):

```sh
godot --path game -- --fixture F-EMPTY
```

The first run imports resources and builds the C# solution. You can also open `game/project.godot` in the Godot editor and press Play, which runs the default scenario with seed 1.

Options go after `--`:

| Option | Effect |
|---|---|
| `--fixture <F-ID\|path>` | Run a defined fixture from `tests/fixtures/`; it pins seed and scenario |
| `--scenario <content-id> --seed <n>` | Run a scenario by name with a seed, for example `--scenario core:scenario/empty_world --seed 42` |
| (none) | `core:scenario/empty_world` with seed 1 |
| `--record <journal.jsonl>` | Write the applied command journal at exit; replay it with the headless runner |
| `--log <file.jsonl>`, `--reports <dir>` | Override the log file and failure-report directory |
| `--bindings <file.json>` | Use another key-binding override file |
| `--select <x,y>`, `--marker <x,y>`, `--layer <key\|name>` | Start with a selected location (metres east, north), a placed marker, or a layer, for reproducible inspection |
| `--capture <file.png>` [`--capture-frame <n>`] | Save the window image after `n` frames (default 10); windowed runs only |
| `--fail-at-ms <ms>` | Inject a failure at that simulation time to exercise failure reporting |
| `--content <dir>` | Load content from another directory |

Godot's own options go before `--`, for example `--headless`, `--quit-after <frames>`, `--verbose` and `--fixed-fps <n>`.

## Inspect

1. Launch with `--fixture F-EMPTY`. The HUD (top left) shows `seed: 20261009  fixture: F-EMPTY@1`, scenario `core:scenario/empty_world`, and simulation time advancing one simulated second per real second in 1000 ms steps.
2. Pan with W/A/S/D, the arrow keys or a middle-mouse drag. Zoom with the mouse wheel or `=`/`-` (anchored at the cursor). Fit the world with Home or R. Axis labels show metres east and north from the south-west origin, with north up.
3. Left-click a location. The inspector (right) lists every spec §3.2 field family. The geographic reference shows the seed, easting and northing, and whether the point is inside the 100 km × 100 km frame. Every other family reads `unavailable: <owner> implements this family in Phase NN`, in grey.
4. Press `]` or `[`, or click a layer, to select an unavailable layer such as "Atmospheric dynamics". The map is hatched and a banner reads `No data — unavailable: Atmosphere implements this family in Phase 07`.
5. Press `M` to place a developer marker at the selected location. It appears after the next step, the journal count in the HUD rises, and the inspector lists it. `Delete` or `Backspace` removes the nearest marker.
6. Press `F3` to hide or show the HUD and `F9` to write a manual failure report. Quit with Ctrl+Q (Cmd+Q on macOS) or by closing the window. The log ends with a `shutdown` record holding the simulation time and state checksum.

HUD fields: seed and fixture, scenario, simulation time and steps, step size, time controls (arrive with SOV-P02-T11), active world chunk (unavailable until chunking, SOV-P03-T07) with the camera centre and metres per pixel, FPS, frame time, last step cost, managed heap, Godot static memory, orphan nodes, log warning and error counts with the last warning, journal and pending commands, a short state checksum, and the build configuration.

To compare with the headless runner, the same fixture, end time and journal give the same checksum:

```sh
godot --headless --fixed-fps 60 --path game --quit-after 400 -- --fixture F-EMPTY --marker 40000,60000 --record artifacts/j.jsonl
dotnet run --project tools/Sovereigns.Headless -- run --fixture F-EMPTY --until-ms <shutdown sim_time_ms> --commands artifacts/j.jsonl
```

## Input

All player and developer actions go through named input actions (`src/Input/InputActions.cs`). The catalog registers them with Godot's `InputMap` at startup; `project.godot` defines none. Overrides live in `user://input_bindings.json` as `{"version": 1, "bindings": {"<action>": ["key:A", "mouse:middle", "key:Ctrl+Q"]}}`. Unknown actions and malformed bindings are skipped with a warning, and conflicts are logged. There is no rebinding screen yet. Actions that change the world become simulation commands, which the world journals with sequence and time. Every handled action also enters a 256-entry history that failure reports include.

## Files and checks

| Path | Content |
|---|---|
| `src/Boot/` | Entry scene script, launch options, paths, world launch |
| `src/Simulation/SimulationHost.cs` | Real-time pacing of fixed steps, failure handling; no Godot types |
| `src/View/` | `ViewTransform` (world metres to screen, y flipped, float32 only relative to the camera centre), map drawing and layers |
| `src/Ui/` | Shell layout, layer list, inspector, HUD, banners |
| `src/Input/` | Action catalog, bindings model and persistence |
| `src/Diagnostics/` | Logging sinks, input history, failure reports |
| `tests/` | Headless client tests (excluded from release exports) |

```sh
godot --headless --path game --build-solutions --quit
godot --headless --path game res://tests/ClientTests.tscn
```

Logs, failure reports and binding overrides are under Godot's `user://` directory. On macOS that is `~/Library/Application Support/Godot/app_userdata/Sovereigns/`, on Linux `~/.local/share/godot/app_userdata/Sovereigns/`, and on Windows `%APPDATA%\Godot\app_userdata\Sovereigns\`. The client prints the exact paths at startup. See [diagnostics](../docs/development/DIAGNOSTICS.md).
