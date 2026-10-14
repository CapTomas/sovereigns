# Godot client and inspection shell

Godot 4.7.2 .NET client ([ADR-0005](../docs/architecture/ADR-0005-toolchain-and-repository-layout.md)). It renders, accepts input and presents state. It owns no terrain, weather, armies or clock of its own. Everything it shows comes from one authoritative `Sovereigns.Simulation.World` ([ADR-0001](../docs/architecture/ADR-0001-runtime.md)).

Phase 01 delivered the reusable development **inspection shell**: a 2D orthographic map with pan and zoom, a layer list, a location inspector and a debug HUD. Phase 02 adds the world's clock to it: the HUD shows the calendar date and time, and the shell can pause, step and accelerate the same world, stop at decisions, list recent world events and show where each inspected reading comes from. The graphics are nonfinal debug graphics. Physical field families are listed but **unavailable**, each with its owner and the phase that implements it. The shell never draws invented values.

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
| `--paused` | Start with time paused (a pacing change, not journaled); press Space to resume |
| `--speed <1-5>` | Start at a speed preset: 1 = 1 s, 2 = 1 min, 3 = 1 h, 4 = 1 day, 5 = 1 week per real second |
| `--fail-after-ms <ms>` | Inject a failure that many milliseconds of simulation time after the world's start, to exercise failure reporting |
| `--content <dir>` | Load content from another directory |

Godot's own options go before `--`, for example `--headless`, `--quit-after <frames>`, `--verbose` and `--fixed-fps <n>`.

## Inspect

1. Launch with `--fixture F-EMPTY`. The HUD (top left) shows `seed: 20261009  fixture: F-EMPTY@2`, scenario `core:scenario/empty_world`, `date: 1 Mar 1203, 06:00:00 (world standard time)` and `time: running at 1 s/s`; the date advances one simulated second per real second.
2. Pan with W/A/S/D, the arrow keys or a middle-mouse drag. Zoom with the mouse wheel or `=`/`-` (anchored at the cursor). Fit the world with Home or R. Axis labels show metres east and north from the south-west origin, with north up.
3. Left-click a location. The inspector (right) lists every spec §3.2 field family. The geographic reference shows the world seed and extent, easting and northing, latitude and local mean solar time, each with its provenance (see below). Every other family reads `unavailable: <owner> implements this family in Phase NN`, in grey.
4. Press `]` or `[`, or click a layer, to select an unavailable layer such as "Atmospheric dynamics". The map is hatched and a banner reads `No data — unavailable: Atmosphere implements this family in Phase 07`.
5. Press `M` to place a developer marker at the selected location. It exists at once (commands apply immediately), the journal count in the HUD rises, the Recent events panel lists `MarkerPlaced`, and the inspector lists the marker. `Delete` or `Backspace` removes the nearest marker.
6. Press `F3` to hide or show the HUD and `F9` to write a manual failure report. Quit with Ctrl+Q (Cmd+Q on macOS) or by closing the window. The log ends with a `shutdown` record holding the simulation time and state checksum.

### Time controls

Pause, step and speed pace the one world; they are not world state. They go to the world's time control, are not journaled and never enter the state checksum, so pausing or accelerating cannot change what happens at a given simulation time. They are kept in the input history that failure reports include. World commands (marker, breakpoint, acknowledge) are journaled at the world's current time.

| Key (action) | Effect |
|---|---|
| Space (`time_toggle_pause`) | Pause or resume |
| `.` (`time_step`) | Advance exactly one step (1000 ms), also while paused; ignored while a decision is pending |
| 1 to 5 (`time_speed_1` to `time_speed_5`) | 1 s, 1 min, 1 h, 1 day or 1 week of simulation per real second |
| B (`set_breakpoint`) | Journaled `SetBreakpoint` at the next standard-time midnight, labelled B1, B2, … At that time the world raises a decision and stops |
| Enter (`acknowledge_decision`) | Journaled `AcknowledgeDecision` of the oldest pending decision, so time can continue |

The kernel's `RealTimePacer` turns real frame time into whole steps. It catches up on at most a quarter second of real time per frame (at 1 s/s that is one step), drops the rest after a hitch and logs one warning. A pending decision stops time at any speed until it is acknowledged. A banner over the map reads `Decision pending: <label> — press Enter to acknowledge`, and the HUD lists the decision with the time it was raised.

### HUD fields

Seed and fixture, scenario, the world-standard date and time (for example `1 Mar 1203, 06:00:00`), season and hemisphere (at the world's mid latitude), time-control state (`running at 1 day/s`, `PAUSED`, or `STOPPED by a pending decision`, with any requested steps), step count, step size and ledger event count, pending decisions (label, time raised, and the acknowledge key), the active world chunk (unavailable until chunking, SOV-P03-T07) with the camera centre and metres per pixel, FPS, frame time, cost and step count of the last simulation advance, managed heap, Godot static memory, orphan nodes, log warning and error counts with the last warning, journal entries, a short state checksum, and the build configuration. Month names, season names and speed labels are display data in the client; the simulation reports numbers.

### Inspector provenance

Each available reading shows its value in display units, then dim lines for where it comes from: `basis` (query, stored, derived, estimated), `owner`, `source`, `derivation` and `updated` (the calendar time its inputs last changed). Latitude is shown in degrees with four decimals (the simulation reports radians) and local mean solar time as `hh:mm:ss` after local midnight (reported in seconds). Easting and northing are the query itself. A reading that is not available stays grey and shows its reason; no value is invented.

### Recent events

The panel under the layer list shows the 12 newest events of the world's ledger, newest first: calendar time and type, then the subjects, data and cause (a command number, a system, an earlier event or the world's origin). It refreshes as events arrive: the creation of the world, markers, breakpoints, decisions raised and acknowledged, season and year boundaries.

### Walk through F-KERNEL

F-KERNEL is a 400-day run from 15 Nov 1202, 06:00 with a recorded command stream (markers, two breakpoints and their acknowledgements). The client submits each stream entry when world time reaches it, exactly as the headless runner does, and never runs past an entry's time.

```sh
godot --path game -- --fixture F-KERNEL --speed 5 --select 52000,51000
```

1. At 1 week/s the 400 days take about a minute. Watch the date in the HUD and the Recent events panel: `SeasonBegan` (Winter) at 1 Dec 1202, `YearBegan` at 1 Jan 1203, then Spring (1 Mar), Summer (1 Jun), Autumn (1 Sep) and Winter again on 1 Dec 1203. The season line in the HUD follows. The stream places the markers `camp` and `ford` on the first day, removes `camp` on 30 Dec 1202 and adds `winter camp`, then adds `bridge` on 11 Sep 1203.
2. The stream sets the breakpoint `muster` on 25 Nov 1202 for 5 Dec 1202, 06:00:00, and `harvest check` on 3 Jun 1203 for 23 Jul 1203, 06:00:00. Its own acknowledgements apply at the same instant, so the events panel shows `DecisionRaised` and `DecisionAcknowledged` with the same time and time never stops there.
3. To see a decision stop time, press `B` at any speed. The breakpoint `B1` is set for the next midnight; when the date reaches it the banner appears, the HUD reads `STOPPED by a pending decision`, and the date stays put until you press Enter.
4. The stream's acknowledgements name event IDs recorded from the unmodified run. Add any world command of your own (pacing does not count) and they no longer match: launch with `--marker 40000,60000` (or press `M` with a location selected) and the stream's acknowledgement is refused with a logged warning, so time stops at `muster` on 5 Dec 1202, 06:00:00 and again at `harvest check` on 23 Jul 1203 until you press Enter. Your own commands also make the world differ from the recorded run, so use a plain launch to compare with the headless runner.
5. Press Space to pause (`PAUSED` in the HUD), `.` to take single steps, and 1 to 5 to change speed. Nothing here changes the state checksum.

To compare with the headless runner, the same fixture, end time and journal give the same checksum (the HUD or the `shutdown` record shows the client's):

```sh
godot --headless --fixed-fps 60 --path game --quit-after 400 -- --fixture F-EMPTY --marker 40000,60000 --record artifacts/j.jsonl
dotnet run --project tools/Sovereigns.Headless -- run --fixture F-EMPTY --until-ms <shutdown sim_time_ms> --commands artifacts/j.jsonl
```

With a fixture that has a command stream, the headless runner applies the stream by itself, so omit `--commands` (and `--marker`) to compare a plain client run:

```sh
godot --headless --fixed-fps 60 --path game --quit-after 2400 -- --fixture F-KERNEL --speed 5
dotnet run --project tools/Sovereigns.Headless -- run --fixture F-KERNEL --until-ms <shutdown sim_time_ms>
```

## Input

All player and developer actions go through named input actions (`src/Input/InputActions.cs`). The catalog registers them with Godot's `InputMap` at startup; `project.godot` defines none. Overrides live in `user://input_bindings.json` as `{"version": 1, "bindings": {"<action>": ["key:A", "mouse:middle", "key:Ctrl+Q"]}}`. Unknown actions and malformed bindings are skipped with a warning, and conflicts are logged. There is no rebinding screen yet. Actions that change the world (marker, breakpoint, acknowledge) become simulation commands, which the world applies at once and journals with sequence and time. Time controls (pause, step, speed) go to the world's time control instead and are not journaled. Every handled action also enters a 256-entry history that failure reports include, so a report keeps the pacing history.

## Files and checks

| Path | Content |
|---|---|
| `src/Boot/` | Entry scene script, launch options, paths, world launch |
| `src/Simulation/SimulationHost.cs` | Paces the world with the kernel's `RealTimePacer`, submits a fixture's command stream at its recorded times, failure handling; no Godot types |
| `src/View/` | `ViewTransform` (world metres to screen, y flipped, float32 only relative to the camera centre), map drawing and layers |
| `src/Ui/` | Shell layout, layer list, inspector and its provenance lines, events panel, HUD, decision and error banners, calendar text |
| `src/Input/` | Action catalog, bindings model and persistence |
| `src/Diagnostics/` | Logging sinks, input history, failure reports |
| `tests/` | Headless client tests (excluded from release exports) |

```sh
godot --headless --path game --build-solutions --quit
godot --headless --path game res://tests/ClientTests.tscn
```

Logs, failure reports and binding overrides are under Godot's `user://` directory. On macOS that is `~/Library/Application Support/Godot/app_userdata/Sovereigns/`, on Linux `~/.local/share/godot/app_userdata/Sovereigns/`, and on Windows `%APPDATA%\Godot\app_userdata\Sovereigns\`. The client prints the exact paths at startup. See [diagnostics](../docs/development/DIAGNOSTICS.md).
