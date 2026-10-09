## Phase 12 — Tactical terrain refinement and two views of one location
**Depends on:** 11.  
**Outcome:** Any campaign coordinate can become a finer battlefield without altering its geographical truth.  
**GDB:** §§3.4, 3.10–3.13, 20, 22.1–22.3, 27.

- [ ] **SOV-P12-T01** — Specify coordinate transformations among global, regional and tactical resolutions with unit tests.
- [ ] **SOV-P12-T02** — Define battle-footprint selection from encounter coordinate and approach vectors, not random map labels.
- [ ] **SOV-P12-T03** — Implement deterministic high-resolution elevation refinement constrained by the parent terrain.
- [ ] **SOV-P12-T04** — Preserve ridgelines, slopes, watersheds and drainage across detail transitions.
- [ ] **SOV-P12-T05** — Preserve each existing river, stream, lake, road and major geological feature at local scale.
- [ ] **SOV-P12-T06** — Refine bank shapes, terrain materials, small drainage features and local obstacles consistently.
- [ ] **SOV-P12-T07** — Use persistent sparse edits for cleared forest, built structures, excavations and destroyed bridges.
- [ ] **SOV-P12-T08** — Read current cloud, rain, snow, wind, solar time, soil and water from global state at battle start.
- [ ] **SOV-P12-T09** — Derive local wind exposure and microterrain effects without creating unrelated weather values.
- [ ] **SOV-P12-T10** — Compute passable areas, water depth, banks, slopes and surface traction at tactical resolution.
- [ ] **SOV-P12-T11** — Build a simple 2D orthographic battlefield viewer using temporary debug terrain materials.
- [ ] **SOV-P12-T12** — Add selectable world coordinates and return-to-campaign controls to the debug viewer.
- [ ] **SOV-P12-T13** — Compare selected strategic ridges, rivers, roads and buildings against tactical footprints automatically.
- [ ] **SOV-P12-T14** — Test battles placed exactly on chunk boundaries and major water confluences.
- [ ] **SOV-P12-T15** — Test narrow bridges, wet valleys, steep passes and dense forest with stable feature positions.
- [ ] **SOV-P12-T16** — Test that zoom, graphics settings and camera location never affect underlying terrain or weather.
- [ ] **SOV-P12-T17** — Test save/load with generated tactical refinements and persistent player-made edits.
- [ ] **SOV-P12-T18** — Profile tactical generation latency and memory; retain seed-based reconstruction where possible.
- [ ] **SOV-P12-T19** — Record a fixed valley two-view golden scene for all future terrain renderer changes.

**Exit gate 12 — Checkpoint C — Two views:** A selected campaign river crossing opens as the exact same 2D location at finer resolution and remains geographically identical after reload.
