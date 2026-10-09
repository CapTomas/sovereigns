## Phase 11 — Environmental laboratory, stable fixtures and causality proof
**Depends on:** 03–10.  
**Outcome:** A trustworthy world simulator can be inspected, advanced and validated without any campaign gameplay.  
**GDB:** §§3–6, 29, 31.7–31.8, 32.10–32.11.

- [ ] **SOV-P11-T01** — Build a headless environmental run mode with seed, map bounds, time advance and output captures.
- [ ] **SOV-P11-T02** — Build a lightweight orthographic debug atlas viewer with pan, zoom, inspect and time controls.
- [ ] **SOV-P11-T03** — Expose one coordinate inspector showing measured, derived and cached physical fields with units.
- [ ] **SOV-P11-T04** — Expose provenance and last-updated timing for all important environment values.
- [ ] **SOV-P11-T05** — Add overlays for elevation, slope, aspect, watershed, soil, vegetation, surface water and groundwater.
- [ ] **SOV-P11-T06** — Add overlays for pressure, air heat, cloud, humidity, wind, precipitation, snow and time of day.
- [ ] **SOV-P11-T07** — Introduce event tracer from source rainstorm to flood and local ground condition change.
- [ ] **SOV-P11-T08** — Create permanent `F-RAIN-SNOW` and `F-VALLEY` reference world fixtures.
- [ ] **SOV-P11-T09** — Record golden measurements at representative mountain, river, coast, valley and desert sites.
- [ ] **SOV-P11-T10** — Run automated multi-year stability tests for weather, river stores, soil and vegetation.
- [ ] **SOV-P11-T11** — Identify and correct nonphysical negative water, NaN values, runaway temperature and edge artifacts.
- [ ] **SOV-P11-T12** — Test world-size-independent determinism and repeatability under documented precision tolerance.
- [ ] **SOV-P11-T13** — Test spatial consistency when loading and unloading adjacent regions during a storm.
- [ ] **SOV-P11-T14** — Instrument per-system time, peak memory, chunk resident size and environmental query latency.
- [ ] **SOV-P11-T15** — Check cause-and-effect: no rainfall without atmospheric source and no river peak without upstream water.
- [ ] **SOV-P11-T16** — Check that a change to the terrain invalidates derived slope, drainage and accessibility caches.
- [ ] **SOV-P11-T17** — Check water/soil/snow after serialization against uninterrupted chronological execution.
- [ ] **SOV-P11-T18** — Add before/after seed captures to regression evidence for every subsequent physical change.
- [ ] **SOV-P11-T19** — Demonstrate user-readable explanation for why a valley flooded and nearby crops failed.
- [ ] **SOV-P11-T20** — Freeze an environmental reference-build tag only after all fixtures and diagnostics pass.

**Exit gate 11 — Checkpoint B — Living world:** A saved reference valley shows a forecastable storm, wind, rain, wet soil, snow/melt and changing river; data is queryable, stable, conserved and reproducible.
