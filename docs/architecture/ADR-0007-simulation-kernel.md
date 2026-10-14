# ADR-0007 — Simulation kernel: clock, scheduler, commands, decisions, randomness, events and state

**Status:** Accepted on merge of its introducing PR (SOV-P02-T03 to T11, T13, T14, T16).
**Decision date:** 2026-10-14
**Scope:** How one world advances in time, how hosts and systems change it, and the rules every later subsystem follows to stay deterministic, explainable and consistent. Persistence is ADR-0008.

## Context

Spec §2.3 and §3.7 require one authoritative clock with independent cadences: no player-only time bubble, and no skipped decisions. Spec §29.1, §29.3, §29.9 and §29.10 add frequencies, deterministic seeded randomness, causal ordering, provenance and invalidation. Spec §3.3 separates stored, dynamic, derived, cached, observed and visual values, and §3.10 requires queries that agree everywhere. ADR-0001 keeps the clock out of rendering, and ADR-0004 fixes millisecond time, typed IDs and determinism bans.

## Decision

### Clock and stepping (T03)

- `World.Now` is the one authoritative clock. Only `World.AdvanceTo(target)` moves it, and `Step()` is `AdvanceTo(Now + StepMs)`. Targets lie a whole number of fixed steps ahead, and the step divides a day.
- Advancing is **event-driven**. The clock jumps to the next due update, the next decision or the target, so idle time costs nothing: 400 days of 1-second steps take milliseconds.
- **Updates due at t run when the clock leaves t.** While the world sits at t, nothing at t has run yet; commands submitted then apply before t's updates. Saving at t and loading continues identically.
- Rendering never advances the clock. Hosts convert real time into steps with `RealTimePacer`, which reads `TimeControl`.

### Scheduler, cadence and priorities (T04)

A system implements `ISimulationSystem`: `Id`, `Priority`, `CadenceMs`, `PhaseMs` and `OwnedComponents`. It runs at times t where (t − phase) is divisible by its cadence, and the cadence and phase are multiples of the step. Systems due at the same instant run in ascending priority, then by `Id`. Priorities follow the causal order of spec §29.9, with gaps for later systems:

| Priority | System | Starting cadence (spec §29.1, tunable by measurement) |
|---|---|---|
| 0 | Calendar (implemented) | daily, at standard midnight |
| 100 | Atmosphere: pressure, wind, moisture, cloud, precipitation | hourly or sub-hourly |
| 150 | Radiation and thermal state | hourly |
| 200 | Surface water and runoff | hourly or daily |
| 300 | Hydrology: rivers, lakes, groundwater, snow | daily |
| 400 | Ecology and soil | daily or seasonal |
| 500 | Agriculture and crops | daily |
| 600 | Economy: production, shipments, markets | daily or weekly |
| 700 | Politics: taxes, obligations | monthly or event-driven |
| 800 | Military operations | hourly or daily |

Each system owns its time step. It integrates over `[SystemContext.Since, Now)`, its previous due time, or the world start if the world began between two due times. It never uses a frame delta. A system that needs finer steps near activity raises its own cadence or splits work, but never moves the clock.

### Commands (T11, T12)

- **World commands** (`PlaceMarker`, `RemoveMarker`, `SetBreakpoint`, `AcknowledgeDecision`, and later gameplay orders) go through `World.Submit`. They apply immediately at `Now`. Each is journaled with a sequence number, its time and its outcome; a refusal carries a reason and changes nothing. Sequences continue across saves.
- **Control commands** (`Pause`, `Resume`, `SetSpeed` up to one simulated year per second, `StepOnce`) go through `World.Control`. They change only `TimeControl`, the host's pacing, and are not journaled. Pausing, stepping and changing speed therefore cannot alter world state or later events' causes (T16). A step request is refused while a decision is pending and cleared when one is raised. A fixture script that hosts play back may contain control commands; a journal replayed with `Replay` must not.
- The number of world commands applied, rejected ones included, is part of the state and the checksum. It numbers later commands, and those numbers appear in event causes.
- Replaying a journal (advance to each entry's time, submit, compare) reproduces the world. A recorded entry that replays differently is a divergence error.

### Decisions and interrupts (T11)

Some events require a decision. When one comes due, the kernel records `DecisionRaised`, adds a pending decision and stops. A system that decides during its update at t that a decision is needed schedules it for t + one step. The clock then stops on arrival there, after every update at t has run once. Nothing at t runs twice, and no "updates done" flag needs saving. `AdvanceTo` makes no further progress, whatever the speed or target, until every pending decision is acknowledged by a journaled `AcknowledgeDecision`. Developer breakpoints (`SetBreakpoint`) are the first source; later phases add encounters, offers and crises (spec §2.3 auto-advance stops). A decision is raised when the clock **arrives** at its time, before that time's updates run.

### Random streams (T06)

`RandomStream.For(seed, purpose, keys…)` is counter-based. All arithmetic is modulo 2⁶⁴, with γ = 0x9E3779B97F4A7C15:

- `mix(z)`: z ← (z ⊕ z≫30)·0xBF58476D1CE4E5B9; z ← (z ⊕ z≫27)·0x94D049BB133111EB; return z ⊕ z≫31 (the SplitMix64 finalizer).
- `fnv1a(purpose)`: 64-bit FNV-1a over the UTF-8 bytes, with offset 0xCBF29CE484222325 and prime 0x100000001B3.
- Key: k ← mix(seed ⊕ fnv1a(purpose)); then for each key part p, such as a place cell, entity ID or event ID, k ← mix(k ⊕ mix(p + γ)).
- Draw number i, counting from 0: mix(k + (i + 1)·γ). `NextDouble` uses the top 53 bits; `NextBelow` uses Lemire's unbiased method.

[`tools/random_stream_reference.py`](../../tools/random_stream_reference.py) implements this definition independently. It and the C# `RandomStreamTests` pin the same golden values. Consequences:

- A stream never depends on how many draws another stream took.
- A stream can resume from a saved `Position`.
- Golden values pin the algorithm across platforms.

Systems name purposes `<system>.<purpose>`. Simulation code cannot use `System.Random`, wall-clock time or GUIDs (banned-API errors), and host and UI randomness reaches the world only through commands.

### Event ledger (T07)

Every authoritative change is a `WorldEvent` with:

- `Id` (`event:N`), time and type;
- **cause**: origin, command sequence, system ID, or parent event;
- **subjects**, the typed IDs of the affected state;
- optional data.

The ledger is append-only and chronological; appending an older event or a non-increasing ID throws (T14). Event types are persisted by name and are append-only.

### Stable IDs (T08)

`EntityId<TKind>` holds an unsigned 64-bit value from a per-world, per-kind counter; it is never reused, and 0 means none. Kinds:

- `feature`, `settlement`, `facility`, `army`, `regiment`, `polity`, `tactical`;
- `event` (kernel);
- `marker`, `breakpoint` (developer).

The kind is part of the type, so IDs of different kinds cannot be confused. Counters are saved.

### State classes, ownership and invalidation (T05, T09, T14)

| Class (spec §3.3) | Here |
|---|---|
| Generated base state | `WorldIdentity`; terrain and generated fields from Phase 04 onward. Immutable during play. |
| Mutable authoritative state | Clock, ID counters, ledger, markers, breakpoints, pending decisions; later each system's owned components |
| Derived queries | `Inspect`, calendar, latitude and solar time. Computed on demand, never saved, never written. |
| Cached derived values | `DerivedCache<T>`, recomputed when a source component's `StateVersion` changes. Example: the marker spatial index. |
| Faction observations | Separate per-observer stores that never write back (Phase 25). The developer inspector is an explicitly omniscient view. |
| Visual representation | Client only (ADR-0001) |

Each authoritative component has exactly one owner in `OwnershipRegistry`: the kernel, developer tools, or a system's `OwnedComponents`. A second claim throws `DuplicateOwnerException` at world creation. Write access is enforced by encapsulation:

- state has no public setters;
- ID allocation is internal;
- hosts change state only through commands;
- a system receives only its `SystemContext`.

A component that feeds a derived cache has a `StateVersion`, which its owner bumps on every write; in Phase 02 that is the markers. Caches declare their sources, so a cache cannot outlive a change to its inputs.

### Spatial query semantics (T10)

A query at any coordinate and time returns the same answer whether its area is unloaded, loading, loaded or refined. Residency changes cost, never results.

- **Unloaded:** answers come from base state and correctly advanced summaries.
- **Loading:** the query materializes synchronously, or answers from those same summaries.
- **Loaded or refined:** answers are higher-resolution samples constrained by the parent data.

Queries are read-only: `Inspect` and any view, zoom or camera change never alter state; a test interleaves arbitrary inspections without changing checksums. A point outside the world returns explicitly unavailable readings. The chunk machinery itself is Phase 03 (SOV-P03-T07); until then the whole world answers from base state.

### Provenance (T13)

Each `FieldReading` is a number in SI units with:

- its basis: query, stored, derived, estimated or unavailable;
- its owner and source fields;
- its derivation formula;
- when its inputs last changed.

Unavailable readings name their owner and phase. Every layer later phases add uses the same shape, so the inspector can explain any value (spec §3.3, §26.13, §29.10).

### Faults

An exception while applying a command or running a step leaves `World.Fault` set. The world then refuses further commands, steps and saves, and the failure report records what is needed to reproduce it (ADR-0008, SOV-P01-T09).

## Alternatives considered

- **Fixed-step loop through every step:** simplest, but a 7-day turn would iterate 604 800 empty seconds. Event-driven advancing gives the same results with no idle cost.
- **Queued commands applied at the next step:** this was Phase 01's behavior. Applying immediately gives the same determinism, shows refusals at once, and works while paused.
- **Journaling pacing commands:** they would number among world commands and shift later causes, so pausing would change the ledger. Pacing is host state.
- **Stateful PRNG per subsystem (xoshiro or PCG):** fast, but a stream's position couples draws to call order and must always be saved. Counter-based streams allow keyed, random-access draws.
- **Dirty flags per cache:** fragile across many writers. Version stamps make invalidation follow from writes automatically.

## Consequences

- Every new subsystem registers as a system with a priority from the table, declares the components it owns, writes through its owner API, records ledger events with causes, uses keyed random streams, and returns readings with provenance.
- Hosts (client, headless runner, future AI and test harnesses) never write state directly; they submit commands.
- Determinism checks compare checksums over the ledger as well as state. A behavior change that alters outcomes raises `SimulationRules.Version` (ADR-0006), and saves from older rules versions need a migration (ADR-0008).
