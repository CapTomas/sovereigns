# 33. Agent implementation charter and delivery order

## 33.1 What this document gives an agent

An agent implementing Sovereigns receives a product vision, authoritative system behavior, cross-system constraints, presentation boundaries and acceptance tests. **Do not treat this document as a prompt to generate the entire game in one pass.** Derive concrete development work from coherent dependency boundaries, integrate into a single world state, and demonstrate behaviors with observable scenario tests.

A good task can be expressed as: **player-observable behavior**, authoritative inputs, derived changes, persistence obligations, UI exposure, AI implications, performance expectations and a reproducible test scenario. An insufficient task is “make economy realistic” with no integration definition.

## 33.2 Dependency order (validation sequence, not reduced scope)

1. **Physical-world kernel:** deterministic world seeds, global coordinates, coherent elevation/drainage, persistent features and region refinement.
2. **Clock and environment:** one calendar, atmospheric/weather fields, runoff, ground moisture, season/daylight, appropriate update scheduling.
3. **Ecology and land use:** soils, vegetation, fields, natural resources and meaningful physical changes.
4. **Settlement and population:** cohorts, location, household production/consumption, regional centers, inventories and migration.
5. **Economic network:** production, markets, shipments, route graph, prices, private/public stores and monetary transfers.
6. **Institutions and political entities:** rights, state formation, crown domains, taxes, vassals, local authority and generated history.
7. **Campaign operations and territory:** roads, armies, recruitment, passage permissions, actual military control, occupation, encirclement, route planning, fog, supply and encounters.
8. **Battle refinement:** same-location battle terrain, formations, ranged/melee/environment, tactical AI and physical aftermath.
9. **Sieges, diplomacy and mature world loop:** fortified sites, defended control nodes, blockades, military-access treaties, peace/annexation, AI strategic objectives, chronicles and enduring campaigns.
10. **Depth and polish:** additional crop/material/unit/culture varieties, balance, game flow, interface refinement, audiovisual implementation under the companion visual direction, and stress testing.

Dependencies are not absolute one-way gates. Early stages should demonstrate thin, **integrated** examples of downstream behavior instead of producing isolated academic simulators. Conversely, agents must not implement detailed tactics before the underlying location/time contracts exist and then retrofit them with incompatible shortcuts.

## 33.3 Recommended reference scenario

Maintain a small, deterministic **river valley reference world** throughout development. It contains one mountain watershed, main river and tributary, forest, farmland, bridge/ford, road, quarry/iron resource, market town, hilltop fortress, two polities, multiple population cohorts, two armies, a neutral transit corridor, a fortified enemy pocket and changeable weather. Use it to repeatedly demonstrate: rain raises river; roads/bridges allow trade; an allied transit force does not annex land; a hostile advance threatens but does not own a valley; blocking all viable routes isolates a fortress; an alternate river route breaks isolation; armies draw supplies; crossing affects battle; bridge destruction persists; surrounding economy and legal/military control adapt. This scenario is a test fixture, not a replacement for full procedural generation.

## 33.4 Per-feature agent handoff checklist

Before calling work complete, verify:

- **State ownership:** what entity or field is authoritative, who reads it, what can change it?
- **Time:** at what cadence does it update, and how does it behave across a seven-day turn and during battle?
- **Spatial identity:** how do coordinates and features survive level-of-detail refinement and save/load?
- **Resource and labor accounting:** what is produced, moved, reserved, consumed, lost and who owns it?
- **Player action:** what can the sovereign order and what does autonomous society do?
- **AI behavior:** do rival rulers and local agents obey the same constraints?
- **Presentation:** what can a player see or understand, including uncertainty and warnings?
- **Persistence:** does the change survive unloading, ending a turn, war, battle and reload?
- **Failures:** are impossible actions explained and blocked rather than silently skipped?
- **Tests:** which acceptance examples show the mechanic under normal and stressed conditions?

Do not mark a system finished solely because it renders convincing visuals or prints plausible numbers without causal integration.

## 33.5 Integration policy

No subsystem may quietly copy an authoritative value into an unrelated local truth without documented synchronization semantics. For example, tactical battle may create refined local wind/ground fields but must derive them from or consistently advance the authoritative environment. A trade path should use the same bridge availability as army movement, allowing differences in carrying capacity due to vehicle types but not contradictory bridge existence.

If a subsystem needs a simplifying approximation, state what information is lost and verify that gameplay-level invariants still hold. When approximation causes unacceptable contradiction, improve representation at the affected boundary rather than creating a special-case cheat.

## 33.6 Art and design-agent constraints

The game is fully **2D orthographic**. No unapproved final art style, sprite anatomy, frame orientation, palette, texture treatment or audio identity is determined by this bible. Those decisions belong to `SOVEREIGNS_VISUAL_DIRECTION.md`. Campaign and battle geography must match, and visual representations must not imply gameplay-relevant obstacles, cover, hazards or access that contradict authoritative simulation state.

## 33.7 Never optimize by deleting meaning

If performance is poor, profile and first consider lower render density, aggregated actors, cached derived fields, dynamic region activation, less frequent background updates where physically defensible, or a cheaper tactical solver preserving frontage and environmental inputs. Do **not** make all ground equally traversable, delete supply inventories, use interchangeable battle maps, generate unlimited arrows, let marching paths instantly annex regions, replace encirclement with polygon-fill mechanics, flatten political rights into one money statistic or convert every culture into a combat-bonus skin. Such changes destroy the distinguishing product.

## 33.8 Living-document proposal template

When design changes are proposed, record in prose:

**Problem:** what observed issue or contradiction requires change?  
**Existing commitment:** which sections and invariants are affected?  
**Proposed decision:** precise player-facing behavior and rule.  
**Affected systems:** geography, environment, economy, politics, warfare, AI, UI, persistence, generation, etc.  
**Trade-offs:** realism, gameplay, performance, readability, production cost.  
**Acceptance test:** what demonstrates the new rule?  
**Migration/update:** effects on saves, content and previous design rules.  
**Decision:** accepted/rejected; date and author/approver.

If a new document is created for a subsystem, it should link to relevant sections of this bible and avoid contradicting them.

## 33.9 Physical-world agent implementation charter

An agent changing physical-world behavior or consuming its contracts must read the **relevant subsections of Sections 3–6** and preserve their definitions of units, ownership, timing, resolution, conservation and data provenance. Start with task routing and expand to affected upstream/downstream contracts; unrelated repository maintenance does not require loading these chapters. Each delivered subsystem must state its upstream physical dependencies, own authoritative outputs, update schedule, cache invalidation rules, save/load behavior, level-of-detail behavior and downstream consumers. If it substitutes coarse physics, it must document what it approximates and demonstrate that the required coupling still works.

A visual-only weather overlay does **not** count as a weather system. A rivers-as-polylines map with no rainfall-fed discharge does **not** count as hydrology. A biome color mask with no water/soil-dependent ecological state does **not** count as living ecosystems. An isolated battle wind value unconnected to the shared atmosphere does **not** count as tactical weather. A printed `soil moisture` number that never affects crops or mobility does **not** meet the design. Build foundation-first vertical integration through the causal chain; add fidelity only when the chain is truthful and validated.

The technical implementation is agent-owned, including numerical scheme, cache structure, storage compression, language and threading, but must satisfy the game behavior and acceptance tests specified here.

---
