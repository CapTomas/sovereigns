# Godot client and inspection shell

Godot 4.7.2 .NET client ([ADR-0005](../docs/architecture/ADR-0005-toolchain-and-repository-layout.md)). It renders, accepts input and presents state. It owns no terrain, weather, armies or clock of its own. Everything it shows comes from one authoritative `Sovereigns.Simulation.World` ([ADR-0001](../docs/architecture/ADR-0001-runtime.md)).

Phase 01 delivered the reusable development **inspection shell**: a 2D orthographic map with pan and zoom, a layer list, a location inspector and a debug HUD. Phase 02 adds the world's clock to it: the HUD shows the calendar date and time, and the shell can pause, step and accelerate the same world, stop at decisions, list recent world events and show where each inspected reading comes from. Phase 03 adds the **field atlas** to it: every field of the atlas catalogue is a layer, the implemented ones are drawn as greyscale or arrow overlays with scale legends, the inspector shows the grid inputs behind each value, and developer tools paint a scratch field and draw survey lines through journaled commands. The graphics are nonfinal debug graphics. Fields that no phase has implemented yet are listed but **unavailable**, each with its owner and the phase that implements it. The shell never draws invented values: overlays and the inspector are sampled from the world's `FieldAtlas` and nothing else.

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
| `--select <x,y>`, `--marker <x,y>`, `--layer <key\|name>` | Start with a selected location (metres east, north), a placed marker, or a layer (a field key such as `scratch`, `scratch_gradient`, `latitude`, or a layer name such as `"Developer scratch"`), for reproducible inspection |
| `--paint <x,y,radius,value[;...]>` | Submit `PaintScratch` commands at startup, one per stroke: centre in metres, radius (0 to 5000 m) and value (0 to 1) |
| `--line <x,y;x,y[;...][\|...]>` | Submit `DrawSurveyLine` commands at startup, one per line (lines separated by `\|`), labelled S1, S2, … |
| `--center <x,y>`, `--zoom <m/px>` | Put the camera at a place and scale (metres per screen pixel, so `--zoom 2` is 0.5 px/m) instead of fitting the world |
| `--level <macro\|regional\|local\|tactical>` | Sample and inspect at that atlas level instead of the one the zoom chooses |
| `--residency` | Start with the chunk-residency overlay on |
| `--no-hud` | Start with the debug HUD hidden (F3 shows it) |
| `--capture <file.png>` [`--capture-frame <n>`] | Save the window image after `n` frames (default 10); windowed runs only |
| `--paused` | Start with time paused (a pacing change, not journaled); press Space to resume |
| `--speed <1-5>` | Start at a speed preset: 1 = 1 s, 2 = 1 min, 3 = 1 h, 4 = 1 day, 5 = 1 week per real second |
| `--fail-after-ms <ms>` | Inject a failure that many milliseconds of simulation time after the world's start, to exercise failure reporting |
| `--content <dir>` | Load content from another directory |

Godot's own options go before `--`, for example `--headless`, `--quit-after <frames>`, `--verbose` and `--fixed-fps <n>`.

## Inspect

1. Launch with `--fixture F-EMPTY`. The HUD (top left) shows `seed: 20261009  fixture: F-EMPTY@2`, scenario `core:scenario/empty_world`, `date: 1 Mar 1203, 06:00:00 (world standard time)` and `time: running at 1 s/s`; the date advances one simulated second per real second.
2. Pan with W/A/S/D, the arrow keys or a middle-mouse drag. Zoom with the mouse wheel or `=`/`-` (anchored at the cursor). Fit the world with Home or R. Axis labels show metres east and north from the south-west origin, with north up.
3. Left-click a location. The inspector (right) lists every spec §3.2 field family and the developer tools, those with data first. The geographic reference shows the world seed and extent, easting and northing, latitude and local mean solar time, each with its provenance (see below). Every field of an unavailable family reads `unavailable: <owner> implements <field> in Phase NN`, in grey.
4. Press `]` or `[`, or click a layer, to select one. The layer list holds the world frame, the developer markers and every field of the atlas catalogue under its family; the first layers are the ones this build can draw (see [field overlays](#field-overlays)). Select an unavailable field such as "Air temperature at 2 m": the map is hatched and the legend reads `No data — unavailable: Atmosphere implements air temperature at 2 m in Phase 07`.
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

Seed and fixture, scenario, the world-standard date and time (for example `1 Mar 1203, 06:00:00`), season and hemisphere (at the world's mid latitude), time-control state (`running at 1 day/s`, `PAUSED`, or `STOPPED by a pending decision`, with any requested steps), step count, step size and ledger event count, pending decisions (label, time raised, and the acknowledge key), the camera centre and metres per pixel, the active chunk and cache statistics (below), FPS, frame time, cost and step count of the last simulation advance, managed heap, Godot static memory, orphan nodes, log warning and error counts with the last warning, journal entries, a short state checksum, and the build configuration. Month names, season names and speed labels are display data in the client; the simulation reports numbers.

### Inspector provenance

Each available reading shows its value in display units, then dim lines for where it comes from. For an atlas field these are, in order:

- `level`: the grid level the value was sampled at, with its cell size, for example `local (16 m)`;
- `classification`: how the value was obtained, for example `stored: bilinear over native cells with overrides`, `base: …`, `derived: bilinear over central differences of scratch`, `aggregate: bilinear over macro cell means of local cells` or `analytic: φ = φ₀ + y / 6 371 000 m`;
- `basis` (query, stored, derived, estimated), `owner` and `source` (where the values come from, for example `geographic reference: south-edge latitude`, `base data and stored writes of scratch` or `scratch (central differences)`);
- `updated`: the calendar time its inputs last changed;
- for a vector field, the `east` and `north` components in the field's unit (the value line then shows the magnitude);
- `inputs`: the grid cells and weights that produced the value, one line each, for example `cell local[3125,3125] w=0.250 v=0.400 (stored)`. `stored` marks a cell with an authoritative override (painted); the other kinds are `base`, `derived` and `aggregate`. A vector input shows `v=(east, north)`. Analytic readings (latitude, solar time) have no grid inputs and say so.

Latitude is shown in degrees with four decimals (the simulation reports radians) and local mean solar time as `hh:mm:ss` after local midnight (reported in seconds). A dimensionless value shows `(dimensionless)`. Easting and northing are the query itself. A reading that is not available stays grey and shows its reason; no value is invented.

Under the location the inspector says which level it used. With a field layer selected it samples at the overlay's level (so what you read is what you see); with the world frame selected it uses each field's native level; `PageUp`/`PageDown` fix a level for both. Then come the markers and the **nearby features** within 14 screen pixels of the location, each with its ID, label, kind, distance and drawing time; the feature nearest the selection is highlighted on the map. The selected layer's family is listed first.

### Field overlays

Every field of the atlas catalogue (`FieldCatalog`) is a layer, listed under its family. The layers that this build can draw come first: **Latitude** and **Local mean solar time** (geographic reference, analytic), **Developer scratch** (a stored 0 to 1 field that the paint tool writes, native level local) and **Developer scratch gradient** (its derived vector field, in m-1). Every other field is shown greyed as `(unavailable — <owner>, <phase>)`; selecting it keeps the hatched no-data map and banner.

A drawable layer is sampled with `FieldAtlas.SampleArea` over the visible part of the world, about one sample every 6 screen pixels, and nothing else feeds the picture. The overlay is rebuilt when the camera, layer, level or clock changes (at most four times a second) and at once after any command, so paint appears the moment it is applied.

- **Scalar fields** are drawn as a greyscale picture, dark at the low end of the range and light at the high end. A sample with no value is transparent. Outside the world frame is hatched.
- **Vector fields** are drawn as arrows every 40 screen pixels, pointing along the vector, with the length proportional to the magnitude. A full-length arrow stands for the next 1, 2 or 5 times a power of ten above the largest magnitude in view (the legend says which); a flat area has no arrows.
- **Range.** The grey ramp spans the field's declared range. When the values in view span less than a tenth of it (latitude over a 100 km world, or faint macro averages of a small painted disc) the ramp spans the data range instead and the legend says so, with both ranges.
- **Level.** The atlas answers at four nested levels (macro 4096 m, regional 256 m, local 16 m, tactical 1 m cells). The zoom chooses the level whose cells are closest to, and not finer than, the sample spacing: local up to 2.7 m/px, regional up to 43 m/px, macro beyond, tactical below 0.17 m/px. `PageUp` and `PageDown` fix the next coarser or finer level instead (a further step returns to automatic); `--level` does the same at startup. Coarser levels show the aggregate of the native cells, so a painted disc smooths out and fades, and the gradient of a small disc averages almost to nothing: use local level or finer for arrows.
- **Legend** (bottom of the map): field name and unit, family and class, the level and whether the zoom or the keys chose it, the sample spacing, the value range with a colour bar (or the arrow scale in the field's unit), and the no-data swatch.

### Chunk residency overlay

`C` (or the "Chunk residency" check box) outlines the chunks the atlas cache holds right now, taken from `FieldAtlas.ResidentChunks()`, over whatever layer is selected. Colours show what the chunk holds: green **native** cells (base data with overrides), blue **derived** cells (computed from other fields, like the gradient), orange **aggregate** cells (coarser means of native cells) and magenta **refined** terms (per-cell corrections and limiters of conservatively refined fields; no Phase 03 field in the shell uses them); a key is drawn at the top right. Chunks with several kinds of content are outlined one inside the other; large chunks are labelled with their content and key. Residency never changes an answer, so this only shows what happened to be loaded: panning, zooming, painting and inspecting load chunks, and the cache evicts the least recently used when it exceeds its budget. The HUD line `active chunk: <key> (<residency>) of <field>` names the chunk under the camera centre at the view level, for the selected layer when it is held in chunks; latitude and local solar time are analytic and have no chunks, so the line then follows scratch.

### Developer tools

Painting and drawing go through journaled commands (`PaintScratch`, `DrawSurveyLine`, `RemoveSurveyLine`), so they are in the journal, the ledger (`FieldPainted`, `FeatureDrawn`, `FeatureRemoved`) and the state checksum, and replay reproduces them. The brush and the pending line are host state; the status line above the help line always shows them, with the view level.

| Key (action) | Effect |
|---|---|
| P (`paint_scratch`) | Paint the scratch field at the cursor with the current radius and value; the status line reports how many local cells changed |
| Shift+= / Shift+- (`paint_radius_up`, `paint_radius_down`) | Radius 10, 20, 50, 100, 200, 500 (default), 1000, 2000 or 5000 m |
| Shift+] / Shift+[ (`paint_value_up`, `paint_value_down`) | Value in steps of 0.1 between 0 and 1 (default 1; painting 0 erases) |
| L (`survey_add_point`) | Add the cursor position to the pending survey line (drawn dashed, points numbered) |
| K (`survey_finish`) | Submit `DrawSurveyLine` labelled S1, S2, … once there are at least two points |
| J (`survey_cancel`) | Drop the pending line |
| X (`survey_remove`) | Submit `RemoveSurveyLine` for the survey line nearest the selected location |
| PageUp / PageDown (`level_coarser`, `level_finer`) | Fix the next coarser or finer atlas level; past either end the zoom chooses again |
| V (`toggle_survey_lines`), C (`toggle_residency`) | Show or hide survey lines (on by default) and the residency overlay (off) |

Painting and survey points use the pointer, so the cursor must be over the map. Survey lines stay inside the world; the world refuses a point outside it and the status line says so.

### Walk through the atlas

```sh
godot --path game -- --fixture F-EMPTY --layer scratch --center 50000,50000 --zoom 2
```

1. The map shows the dark, unpainted scratch field at **local** level; the legend reads `level: local (16 m) (chosen from the zoom); one sample every 12 m`, the ramp `dark 0, light 1`, and the HUD `active chunk: local chunk (48, 48) (Loaded) of scratch`.
2. Put the cursor on the map and press `P`: a light disc of radius 500 m appears. The status line says `PaintScratch applied … N local cell(s) changed`, Recent events lists `FieldPainted`, and the HUD journal count rises. Press `Shift+[` three times (value 0.70) and `Shift+-` once (radius 200 m), move the cursor and paint again for a second, greyer disc.
3. Click the middle of a disc. The inspector's Developer tools section shows `developer scratch: 1 (dimensionless)`, `level: local (16 m)`, `classification: stored: bilinear over native cells with overrides` and four input lines such as `cell local[3125,3125] w=0.250 v=1.000 (stored)`.
4. Press `PageUp`: the level becomes **regional** (256 m cells) and the same view is a smoother blob; the inspector's inputs become `regional[…]` cells marked `(aggregate)`. Press `PageUp` again for **macro**: the disc is a faint smear, and the legend switches to the data range because the values span far less than 0 to 1. Press `PageUp` once more to return to automatic, and zoom out with the wheel to watch the level follow the zoom.
5. Press `]` to select **Developer scratch gradient** (or launch with `--layer scratch_gradient`). At local level, arrows around each disc point toward the higher values, longest where the paint changes fastest; the legend gives the scale in m-1.
6. Press `C`: the chunk outlines appear, green native and blue derived, and the HUD cache line counts them. Pan and zoom to load more.
7. Press `L` at three places, then `K`: a yellow polyline `S1 (survey:1)` appears. Select a spot within 14 pixels of it: the inspector lists `survey:1 "S1": survey line, … m away` under Nearby features and the line is highlighted. Press `X` to remove it. `J` drops a line you have started.

For a ready-made scene, `--paint "50000,50000,400,0.4;50150,50050,200,0.8;49850,49950,120,1" --line "49500,49600;50000,50300;50500,50100"` paints three overlapping discs and draws one line, and the fixture `F-ATLAS` replays strokes and lines over its first half hour (`godot --path game -- --fixture F-ATLAS --speed 3 --layer scratch`).

### Recent events

The panel under the layer list shows the 12 newest events of the world's ledger, newest first: calendar time and type, then the subjects, data and cause (a command number, a system, an earlier event or the world's origin). It refreshes as events arrive: the creation of the world, markers, breakpoints, decisions raised and acknowledged, season and year boundaries, scratch paint (`FieldPainted`) and survey lines drawn or removed (`FeatureDrawn`, `FeatureRemoved`).

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

All player and developer actions go through named input actions (`src/Input/InputActions.cs`). The catalog registers them with Godot's `InputMap` at startup; `project.godot` defines none. Overrides live in `user://input_bindings.json` as `{"version": 1, "bindings": {"<action>": ["key:A", "mouse:middle", "key:Ctrl+Q"]}}`. Unknown actions and malformed bindings are skipped with a warning, and conflicts are logged. There is no rebinding screen yet. Actions that change the world (marker, breakpoint, acknowledge, scratch paint, survey lines) become simulation commands, which the world applies at once and journals with sequence and time. Time controls (pause, step, speed) go to the world's time control instead and are not journaled. Every handled action also enters a 256-entry history that failure reports include, so a report keeps the pacing history.

## Files and checks

| Path | Content |
|---|---|
| `src/Boot/` | Entry scene script, launch options, paths, world launch |
| `src/Simulation/SimulationHost.cs` | Paces the world with the kernel's `RealTimePacer`, submits a fixture's command stream at its recorded times, failure handling; no Godot types |
| `src/View/` | `ViewTransform` (world metres to screen, y flipped, float32 only relative to the camera centre), map drawing, the layer list, field overlays sampled from the atlas (`FieldOverlay`, `FieldRamp`, `ViewLevels`) and chunk outlines |
| `src/Ui/` | Shell layout, layer panel and overlay toggles, legend, inspector and its provenance and input lines, developer tools, events panel, HUD, decision and error banners, calendar and value text |
| `src/Input/` | Action catalog, bindings model and persistence |
| `src/Diagnostics/` | Logging sinks, input history, failure reports |
| `tests/` | Headless client tests (excluded from release exports) |

```sh
godot --headless --path game --build-solutions --quit
godot --headless --path game res://tests/ClientTests.tscn
```

Logs, failure reports and binding overrides are under Godot's `user://` directory. On macOS that is `~/Library/Application Support/Godot/app_userdata/Sovereigns/`, on Linux `~/.local/share/godot/app_userdata/Sovereigns/`, and on Windows `%APPDATA%\Godot\app_userdata\Sovereigns\`. The client prints the exact paths at startup. See [diagnostics](../docs/development/DIAGNOSTICS.md).
