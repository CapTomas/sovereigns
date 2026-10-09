## Phase 51 — Performance, memory, determinism and large-world scaling
**Depends on:** 49–50 and representative integrated campaign.  
**Outcome:** Huge worlds and large battles are practical on agreed target desktop hardware.  
**GDB:** §§3.4, 3.7, 29, 32.9–32.11.

- [ ] **SOV-P51-T01** — Establish representative desktop hardware profiles, target frame-rate and UI responsiveness criteria.
- [ ] **SOV-P51-T02** — Define budgets for world generation, saved-game size, memory, map query and battle start time.
- [ ] **SOV-P51-T03** — Define budgets for idle world, seven-day turn execution and multi-year headless time advance.
- [ ] **SOV-P51-T04** — Benchmark small, normal, large and pathological world seeds under reproducible load.
- [ ] **SOV-P51-T05** — Benchmark weather, soil, river, ecosystems, markets, trade and AI costs separately and together.
- [ ] **SOV-P51-T06** — Measure worst-case chunk cache miss storms and cross-border update behavior.
- [ ] **SOV-P51-T07** — Verify distant/offscreen world dynamics progress consistently under lower-frequency updates.
- [ ] **SOV-P51-T08** — Use compressed or aggregated field storage only where numerical error stays within tolerance.
- [ ] **SOV-P51-T09** — Audit unnecessary authoritative copies of spatial, weather, ownership and resource state.
- [ ] **SOV-P51-T10** — Avoid allocating per-pixel persistent objects for properties derivable from shared fields.
- [ ] **SOV-P51-T11** — Profile battlefield generation and bounded on-demand high-resolution terrain refinement.
- [ ] **SOV-P51-T12** — Profile thousands of visible soldiers represented by a feasible number of formation simulations.
- [ ] **SOV-P51-T13** — Profile pathfinding of many armies and regiment formations through bottlenecks.
- [ ] **SOV-P51-T14** — Profile projectile simulation under realistic archery volley workloads.
- [ ] **SOV-P51-T15** — Identify expensive market/network operations and incremental recalculation opportunities.
- [ ] **SOV-P51-T16** — Benchmark AI-only 50-, 100- and 300-year world runs for CPU, memory and state drift.
- [ ] **SOV-P51-T17** — Benchmark save, load, migration and recovery time in a mature large campaign.
- [ ] **SOV-P51-T18** — Check performance optimizations against conservation, determinism and gameplay golden tests.
- [ ] **SOV-P51-T19** — Create profiling artifacts and budget trend reports tracked by CI or nightly benchmarks.
- [ ] **SOV-P51-T20** — Fix frame-time spikes, memory fragmentation and runaway cache growth under long play.
- [ ] **SOV-P51-T21** — Test multiple display resolutions and window states without simulation speed changing.
- [ ] **SOV-P51-T22** — Validate physics remains identical within documented tolerances at low and high frame rate.

**Exit gate 51:** Representative large campaigns and battles meet measured hardware targets, with known numerical tolerances and no performance optimization breaking causal simulation.
