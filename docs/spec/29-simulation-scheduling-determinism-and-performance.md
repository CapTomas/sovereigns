# 29. Simulation scheduling, determinism and performance

## 29.1 Systems run at different frequencies

A useful **starting schedule** (tunable based on measured outcomes):

| Layer | Typical frequency | Detail strategy |
|---|---|---|
| Tactical orders, collision, projectiles | Sub-second fixed steps | Active battlefield only |
| Local battle environment | Seconds/minutes | Active battlefield only |
| Weather and wind fields | Hourly/sub-hourly | Coarse world grid + localized refinement |
| Ground wetness/runoff | Hourly/daily as needed | Regional cells and active chunks |
| Army movement, consumption, encounters | Hourly/daily | Active routes and armies |
| Crop growth, household consumption | Daily | Aggregated parcels/cohorts |
| Production and shipments | Daily/weekly | Sites and transport graph |
| Market clearing and economic plans | Daily/weekly | Market regions |
| Construction progress | Daily/weekly | Active projects |
| Taxes and obligations | Monthly/event-driven | Local authorities/kingdoms |
| Demographics and migration | Monthly/seasonal | Cohorts |
| Forestry, land development and long-run knowledge | Seasonal/annual | Ecological/institutional regions |

A strategic turn's seven days advance all due systems in chronological order. Do not run a complete full-resolution atmospheric solver for every rendered frame. Distant regions remain economically and politically active through aggregated schedules, without tactical refinement.

## 29.2 Active vs background simulation

A sleeping chunk is **not frozen in time**. Weather, crops, populations, supplies and trade continue at their appropriate aggregate resolution. Active battle terrain is refined in space and time. When loading an area, reconstruct its detailed state from seed, coarse state, relevant events and persistent modifications; it should not have gained or lost resources because it was unloaded.

## 29.3 Deterministic world and event streams

Same seed + configuration + initial state + ordered inputs + compatible simulator version should reproduce the coupled physical, ecological and human campaign state closely and reliably. Randomness is explicit, seeded and separated by subsystem/event identity enough to avoid changing world geography because an unrelated cosmetic event consumed a random draw. Save all necessary state and event positions. Fixed-step battlefield simulation should prioritize reproducibility; document acceptable platform-dependent numerical variation if absolutely necessary.

## 29.4 Conserved quantities and bookkeeping

Water, snowpack, biomass, people, goods, money, armies, livestock and major structures must not be silently duplicated or deleted. Economic production transforms inputs into outputs while consuming time, energy and labor; biological growth and extraction may add goods through modeled natural sources; consumption/loss removes them. Transit tracks quantities without double counting. Casualties reduce population and regiment totals coherently. Historical event log and persistent scene state must not diverge.

## 29.5 Stability and causality

Coupled systems risk positive feedback loops and unrealistic oscillation. Use appropriate smoothing, capacity limits, inventories, delays, substitution and bounded response. Every major chain reaction should have an explainable path through entities and resources. Extreme events can still cause serious collapse if the underlying conditions justify it. Do not fix instability by silently overriding stocks with game-balance freebies.

## 29.6 Performance philosophy

The simulation scale is constrained by **information value**, while physical state remains coherent at every queried coordinate. Full tactical detail is only paid for where battles occur. World fields are chunked and updated by frequency. Markets, populations and commercial flows are aggregated. Visible soldiers do not each require complex global AI. Battle simulations can use formation/contact abstractions while preserving frontage and geometry. Visual density scales independently from core tactical computations within bounded accuracy.

Profile actual bottlenecks rather than assuming language or engine is the issue. Support configurable visual fidelity and tactical agent draw density without changing campaign economics. Simulation-speed scaling should reduce visual expense first, not silently drop crop updates or replay-critical events.

## 29.7 Game engine and implementation guidance boundary

**Committed separation:** authoritative simulation model vs presentation/client. **Recommended baseline:** Godot 4 for 2D presentation and editor; a separate pure simulation module in a performant language compatible with robust integration (C# is a reasonable default given strong tooling, but the particular language is a replaceable engineering choice). The design bible does **not** prescribe code structure, APIs, serialization library or module layout. All choices must preserve deterministic state, testability and the authoritative world contract.

## 29.8 Persistence, upgrades and integrity

Saves capture seed, generator/rules versions, calendar, all active and persistent entities, inventories, land changes, treaties, political rights, event state, queued orders, battle continuation state and required random state. Load validation must detect missing/invalid references rather than silently inventing replacements. A changed engine version needs explicit migration or declared compatibility limits. World seeds alone are not sufficient save data after a campaign evolves.

## 29.9 Causal scheduling and conservative world-field integration

The engine maintains a dependency-ordered, timestamped sequence across physical subsystems. Regional pressure/wind and temperature/moisture determine cloud/precipitation and evaporative forcing; precipitation and energy update snow, soil, surface/ground water; catchment routing updates rivers/lakes and inundation; updated ground conditions feed roads, construction, crops, navigation, army routes and combat. Slow growth and cultural/economic change consume histories and aggregates rather than the last sampled instantaneous weather value.

Different fields may use different grids and time steps, but interpolation and accumulation must preserve significant water/energy-proxy and terrain-feature consistency. Halo/edge exchange between chunks, river routing across active/background boundaries, wet/dry threshold crossings, save/load and transition to a tactical battle require reproducible reconciliation. Distant world chunks may use summarized flux and storage; they cannot discard upstream rainfall or reset snowpack when a player zooms in.

## 29.10 Spatial-field provenance, invalidation and diagnostics

Every authoritative field family declares owners, update cadence, dependencies, units, spatial support and persistence. Derived outputs declare invalidation triggers. Changes to a riverbed, road, bridge, tree cover or surface-water state must invalidate dependent route/access, land suitability, LOS and/or combat caches, without necessarily rebuilding the entire world. Debug tooling should trace important effects backward through physical state and event history to identify simulation errors and explain unexpected outcomes.

## 29.11 Performance budget hierarchy

Spend calculation in order of gameplay consequence: global low-resolution atmosphere and catchments; persistent regional land/river/soil state; active settlement and route production; live tactical terrain and combat; decorative rendering last. Large armies and climate must not compete for millions of unnecessary global per-pixel agents. Adaptive spatial/temporal refinement is permitted if tests prove the same location-time queries and accumulated outcomes stay within declared tolerances. Profile against the actual largest supported worlds and battles, not only a tiny valley fixture.

---
