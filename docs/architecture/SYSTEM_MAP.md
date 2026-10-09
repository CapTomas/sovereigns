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
| Military operations | Army location, march, supply, threat, control, occupation | Encounters, settlement behavior, tactical transitions |
| Tactical warfare | Regimental state, wounds, projectiles, combat outcome | Campaign, persistence, economic losses |
| Persistence | Versioned state/journal, deterministic checkpoints and migrations | All simulation modules and save UI |
| Godot client | Visual/camera/input state only | Player and render presentation |

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
