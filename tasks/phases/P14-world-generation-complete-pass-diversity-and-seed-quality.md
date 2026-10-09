## Phase 14 — World generation complete pass, diversity and seed quality
**Depends on:** 11 and early tactical consistency.  
**Outcome:** Varied world seeds create coherent continents fit for settlements and warfare.  
**GDB:** §§4, 6, 7.1, 32.2, 32.10.

- [ ] **SOV-P14-T01** — Expose world seed, size, latitude, landmass structure and climate configuration through data-driven settings.
- [ ] **SOV-P14-T02** — Run full generator passes in dependency order: geology, erosion, drainage, climate, soils and ecosystems.
- [ ] **SOV-P14-T03** — Create named continental, coastal, inland and island-scale worldgen fixtures.
- [ ] **SOV-P14-T04** — Measure mountain continuity, valley usability, coast quality and basin connectivity across many seeds.
- [ ] **SOV-P14-T05** — Check wet/dry latitudinal distributions, seasonal gradients and rain-shadow coherence.
- [ ] **SOV-P14-T06** — Detect isolated unplayable land, broken river sinks, impossible enclosed seas and discontinuous coastlines.
- [ ] **SOV-P14-T07** — Detect duplicate or missing feature IDs after resampling and spatial chunk splitting.
- [ ] **SOV-P14-T08** — Derive resource-deposit potential from generated geology and long-term environment.
- [ ] **SOV-P14-T09** — Derive fertility, hazard, accessibility, harbor and defensible-site suitability overlays.
- [ ] **SOV-P14-T10** — Derive candidate mountain passes, river crossing locations and natural defensive chokepoints.
- [ ] **SOV-P14-T11** — Add user-friendly controls for seed replay and world preview without waiting for civilizations.
- [ ] **SOV-P14-T12** — Expose quality warnings and optional rejection/regeneration rules for catastrophically bad seeds.
- [ ] **SOV-P14-T13** — Validate no generator stage depends on global RNG execution order or camera visibility.
- [ ] **SOV-P14-T14** — Support reproducing terrain from world ID and content version without hard-coded coordinates.
- [ ] **SOV-P14-T15** — Write a world-generation report summarizing geography, climate zones, rivers and resource distribution.
- [ ] **SOV-P14-T16** — Compare short and long generations and document approximation/fidelity settings honestly.
- [ ] **SOV-P14-T17** — Test stable battlefield refinement at remote coasts, peaks, river mouths and dense forests.
- [ ] **SOV-P14-T18** — Document worldgen failure cases that should produce a validated rejection rather than silent corruption.
- [ ] **SOV-P14-T19** — Run a large deterministic seed batch and review automatic outliers plus sample maps manually.

**Exit gate 14 — Checkpoint A/B — Generation quality:** At least a diverse batch of seeded worlds generates successfully, retains spatial correctness and produces recognizable geographical opportunities with no hand-authored necessary map.
