## Phase 07 — Atmosphere, pressure, humidity, wind, clouds and precipitation
**Depends on:** 06; drainage and coastlines.  
**Outcome:** Weather evolves through coupled physical state rather than independent random tiles.  
**GDB:** §§5.2–5.8, 5.14, 32.10.

- [ ] **SOV-P07-T01** — Define atmospheric grid resolution, vertical simplification, update frequency and domain boundary conditions.
- [ ] **SOV-P07-T02** — Establish air pressure, temperature, absolute water vapor and relative humidity as coherent related quantities.
- [ ] **SOV-P07-T03** — Model prevailing circulation tendencies, pressure-gradient flow, terrain steering and seasonal variability.
- [ ] **SOV-P07-T04** — Produce continuous horizontal wind vectors with speed, direction, gust/exposure semantics and temporal persistence.
- [ ] **SOV-P07-T05** — Couple maritime moisture sources and inland evapotranspiration hooks to atmospheric moisture stores.
- [ ] **SOV-P07-T06** — Transport atmospheric heat and vapor between cells with a stable numerically bounded scheme.
- [ ] **SOV-P07-T07** — Model uplift/cooling from terrain, convergence/frontal approximation and convective instability as cloud/precipitation contributors.
- [ ] **SOV-P07-T08** — Form and dissipate cloud-bearing moisture from actual condensation and available vapor, not rendered particle rules.
- [ ] **SOV-P07-T09** — Move cloud systems coherently with wind and carry storms across region/chunk boundaries.
- [ ] **SOV-P07-T10** — Feed cloud opacity into solar heating and surface temperature, creating thermal feedback.
- [ ] **SOV-P07-T11** — Generate precipitation amount, intensity, duration and footprint from the cloud/moisture budget.
- [ ] **SOV-P07-T12** — Determine rain, snow, mixed/freezing precipitation using the thermal profile/appropriate thermodynamic approximation—not an unconditional single surface-temperature switch.
- [ ] **SOV-P07-T13** — Account for moisture removed from atmosphere and placed into liquid/snow/ice stores; audit precipitation accounting.
- [ ] **SOV-P07-T14** — Make windward orographic precipitation and leeward rain shadows emerge from airflow and terrain.
- [ ] **SOV-P07-T15** — Derive local wind exposure/shelter in valleys, behind hills, within forest and near structures.
- [ ] **SOV-P07-T16** — Derive low-cloud/fog visibility from atmospheric moisture and temperature conditions.
- [ ] **SOV-P07-T17** — Build weather overlays for pressure, air temperature, humidity, wind vectors, cloud, precipitation and forecast uncertainty hooks.
- [ ] **SOV-P07-T18** — Test dry air, saturated air, mountain uplift, coastal storms, changing wind, night cooling and precipitation phase transitions.
- [ ] **SOV-P07-T19** — Verify storm motion, clouds and rain remain coherent while the camera is elsewhere and after save/load.
- [ ] **SOV-P07-T20** — Verify seeded weather replay with conservation/stability tolerances and detailed causal diagnostics.

**Exit gate 07:** A storm traverses the world following atmospheric state. At −5°C, snow is typical under compatible thermal profiles, but warm layers can create other phases; cloud, wind, water mass and sun remain coupled.
