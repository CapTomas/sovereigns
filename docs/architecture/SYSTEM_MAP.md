# Sovereigns system map and authoritative ownership

The architecture is a **modular monolith**, not microservices. Project/assembly decomposition is guided by verified dependency boundaries and benchmarks; these names describe ownership, not an obligation to prematurely generate dozens of separate projects.

| Owner | Authoritative state | Principal consumers |
|---|---|---|
| World foundation | World identity, coordinates, terrain, geology, versioned spatial fields | Hydrology, atmosphere, ecology, infrastructure, economy, war, renderer |
| Simulation scheduler | Clock, update ordering, seeded random streams, world event order | All systems |
| Atmosphere | Temperature/air moisture, wind, pressure, clouds, precipitation | Water balance, crops, fire, combat, UI |
| Hydrology | Surface flow, rivers, lakes, groundwater, snowpack, flood state | Ecology, roads, settlements, army movement, battle |
| Ecology/soil | Soil properties, vegetation, biological productivity, land cover | Agriculture, population, resources, combat |
| Economy/population | Cohorts, occupation, inventories, production, exchange, labor | Taxation, recruitment, logistics, AI, UI |
| Politics/government | Claims, legal ownership, access permissions, obligations, institutions | Trade, occupation, diplomacy, AI, warfare |
| Infrastructure | Physical structures and works: roads, bridges, drainage/irrigation works, buildings, fortifications, their condition and construction progress | Transport, hydrology, settlements, military, tactical, client |
| Military operations | Army location, march, supply, threat, control, occupation | Encounters, settlement behavior, tactical transitions |
| Tactical warfare | Regimental state, wounds, projectiles, combat outcome | Campaign, persistence, economic losses |
| Persistence | Versioned state/journal, deterministic checkpoints and migrations | All simulation modules and save UI |
| Godot client | Visual/camera/input state only | Player and render presentation |
| Content data | Versioned definitions (crops, commodities, equipment, buildings, institutions, asset references) keyed by ADR-0004 content IDs; read-only at runtime | Simulation modules, client presentation, mods |
| Tools | No authoritative game state. Batch generation, inspection, validation and replay run the same simulation assemblies | Developers, CI, agents |

Generated worlds are simulation state produced by the world-generation owners from seed and versioned content. Their outputs belong to the producing module, not to tools or the client. Save files are a Persistence-owned format that serializes each module's authoritative state through versioned contracts. No other component writes save data directly.

## Physical field ownership matrix

Field families from spec §3.2. Each **field** has exactly one writer. Where a family is split, the table names which fields each owner writes. Consumers read through queries or snapshots. Inputs from another module (energy, precipitation, uptake) arrive as explicit scheduled fluxes (spec §29.9), which the owner applies to its own fields. Owning phases add the exact fields, units (ADR-0004), resolution and cadence when they implement them.

| Field family (§3.2) | Writer(s), split by field | Principal consumers |
|---|---|---|
| Geographic reference | World foundation: seed, scale, coordinates, latitude reference, sea datum. Simulation scheduler: clock and calendar position | Every module, saves |
| Bedrock, landform and terrain derivatives | World foundation | Hydrology, ecology, infrastructure, military operations, tactical, client |
| Surface material | World foundation: rock, parent material, base texture and depth. Ecology/soil: evolving topsoil properties (organic content, structure, erosion state) | Hydrology, agriculture, transport, military, tactical |
| Atmospheric dynamics, atmospheric water, radiative/thermal state | Atmosphere | Hydrology, ecology, agriculture, transport, military, tactical, client |
| Surface water and catchment | Hydrology | Ecology, settlements, transport, military, tactical, client |
| Subsurface/soil water | Hydrology (plant uptake from Ecology/soil arrives as a flux) | Ecology, agriculture, infrastructure, military, tactical |
| Snow and frozen state | Hydrology (energy and precipitation from Atmosphere arrive as fluxes) | Transport, military, tactical, client |
| Climate reference (long-run statistics) | Atmosphere | Ecology, settlements, agriculture, AI forecasts |
| Ecosystems and soil productivity | Ecology/soil | Agriculture, resources, population, military (concealment, forage) |
| Geological resources | World foundation (extraction by Economy/population arrives as a flux) | Resources/economy, settlements |
| Human landscape | Economy/population: parcels and land use. Politics/government: rights over them. Infrastructure: physical structures and works | Hydrology, transport, military, tactical, client |
| Transient local conditions | Hydrology: standing water, ponding, mud. Ecology/soil: fire and fuel state. Tactical warfare: battle-only conditions (smoke, tracks, debris, fallen obstacles) | Military, tactical, client |

**Active battles.** During an active battle, Tactical warfare owns a refined local copy of the battle area's fields, constrained by the parent fields (spec §22, §3.4). The campaign owners keep advancing the parent fields (weather, rivers), and Tactical writes only its refined copy. When the battle ends, its persistent changes return to the campaign owners as scheduled events, which those owners apply (spec §3.11, §24). Persistence stores the events; it does not write fields. This is a time-bounded hand-off, never two concurrent writers of one value.

## Project references (Phase 01 onwards)

Compile-time dependencies point toward the simulation:

- Simulation projects reference only the .NET base library and vetted pure libraries. No Godot, UI, rendering or store SDK.
- Simulation test projects reference simulation projects and the test framework.
- Tools reference simulation projects. They are not referenced by them.
- The Godot client references simulation public interfaces and Godot. It sends commands and reads snapshots.

A CI check enforces "no Godot reference in simulation projects" once those projects exist (ADR-0001 acceptance).

## Boundary contracts

1. **Ownership ≠ influence ≠ access:** military presence cannot implicitly transfer legal sovereignty.
2. **Spatial coherence:** battlefield is a coordinate-preserving refinement; persistent physical modifications return to same world position.
3. **Coupled state:** rainfall changes water stores, which affect soils/rivers/crops/mobility. Derived modifiers must not have separate hand-authored truth.
4. **Temporal coherence:** deterministic scheduler owns simulation time. Rendering may run at arbitrary FPS without changing outcomes.
5. **Conservation:** commodities/people/water cross system boundaries with accounted fluxes, sinks and sources within documented tolerances.
6. **Data format:** authoritative schemas have stable IDs, versioned migration rules, provenance and validity tests.
7. **Cross-domain change:** producer and consuming domains jointly review field semantics (units, cadence, missing-data policy, invalidation).

## Dependency direction

Geometry → Atmosphere/Hydrology → Ecology/Soil → Agriculture/Resources → Population/Economy → Government/Army supply → Operational strategy → Tactical combat → Persistent world deltas. Feedback cycles are explicit scheduled writes (e.g., construction changes hydrology, fire changes vegetation, army destruction changes transport), never uncontrolled accidental circular dependencies.

## Build independence

Simulation assemblies must remain testable with `dotnet` alone. Godot is downstream from simulation public interfaces, not the other way around. Specs describe *what*; ADRs describe *how*; QA checks behavior and causal contracts.
