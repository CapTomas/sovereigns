## Phase 04 — Procedural geology, coastlines, mountains and elevation
**Depends on:** 03.  
**Outcome:** A seeded continent has plausible regional landforms, height and meaningful geological structure.  
**GDB:** §§4.1–4.6, 5.1, 6.1, 32.2.

- [ ] **SOV-P04-T01** — Define world creation parameters for continent scale, landmass character, climate-relevant latitude and seed.
- [ ] **SOV-P04-T02** — Generate believable landmass masks, coastlines, straits, continental shelves and seas.
- [ ] **SOV-P04-T03** — Generate coherent large-scale geological provinces rather than assigning independent terrain noise values.
- [ ] **SOV-P04-T04** — Produce correlated mountain belts, highlands, foothills, basins and plains.
- [ ] **SOV-P04-T05** — Represent elevation in a globally consistent datum with valid sea/land transition handling.
- [ ] **SOV-P04-T06** — Produce valleys, ridgelines, passes and catchment-friendly slopes at macro scale.
- [ ] **SOV-P04-T07** — Apply an appropriate terrain-shaping/erosion pass that creates credible landform structure without breaking drainage.
- [ ] **SOV-P04-T08** — Preserve geology/terrain seed information so strategic resources can later correlate with world formation.
- [ ] **SOV-P04-T09** — Derive slope, aspect, curvature, ruggedness and exposure from actual terrain geometry.
- [ ] **SOV-P04-T10** — Handle coastal cliffs, escarpments, interior basins and unusual landforms without invalid samples.
- [ ] **SOV-P04-T11** — Define how mountain passes and wide valleys survive regional-to-local refinement.
- [ ] **SOV-P04-T12** — Ensure generation boundaries, tile edges and regional seams have continuous slopes where intended.
- [ ] **SOV-P04-T13** — Add diagnostic overlays for elevation, slope, rock structure and drainage potential.
- [ ] **SOV-P04-T14** — Write property checks for impossible cliffs, widespread spikes, accidental isolated fragments and invalid ocean elevations.
- [ ] **SOV-P04-T15** — Batch-generate diverse seeds and classify defects by generation rule rather than manually repairing maps.
- [ ] **SOV-P04-T16** — Verify same-seed geography identity across saves and engine restarts.
- [ ] **SOV-P04-T17** — Capture a representative continent and `F-VALLEY` location that remain fixed regression references.

**Exit gate 04:** Different seeds create convincingly different, connected physical landscapes; arbitrary points have consistent elevation and derived topographic properties.
