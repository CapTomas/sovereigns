## Phase 06 — Solar geometry, day/night, seasons and thermal foundation
**Depends on:** 05 and clock kernel.  
**Outcome:** Geographic and seasonal solar energy drives actual thermal state.  
**GDB:** §§5.1–5.3, 5.7, 5.13–5.14.

- [ ] **SOV-P06-T01** — Define world latitude/hemisphere, calendar-year length, tilt abstraction and solar-position query.
- [ ] **SOV-P06-T02** — Calculate sunrise, sunset, daylight duration, twilight and sun elevation by place and season.
- [ ] **SOV-P06-T03** — Distinguish local clock, global campaign chronology and tactical battle encounter time.
- [ ] **SOV-P06-T04** — Compute terrain aspect/horizon exposure and seasonally meaningful slope heating.
- [ ] **SOV-P06-T05** — Model incoming radiation with cloud/land reflectivity hooks for later atmospheric coupling.
- [ ] **SOV-P06-T06** — Create separate air temperature, surface/ground thermal state and accumulating growing-season heat measures.
- [ ] **SOV-P06-T07** — Approximate land, water and snow thermal inertia without forcing an expensive global fluid solver.
- [ ] **SOV-P06-T08** — Implement diurnal heating/cooling, nighttime cooling and temperature response to season.
- [ ] **SOV-P06-T09** — Incorporate altitude-driven temperature tendency and local inversion/frost-pocket approximation.
- [ ] **SOV-P06-T10** — Connect local material, vegetation and moisture interfaces to thermal exchange without inventing independent biomes.
- [ ] **SOV-P06-T11** — Establish frozen/unfrozen ground state inputs and sustained thermal history, not just current instant air temperature.
- [ ] **SOV-P06-T12** — Create diagnostic temperature, solar exposure and daylight overlays.
- [ ] **SOV-P06-T13** — Validate contrasting latitude, mountain valley, coast, cloudy day, clear night and seasonal cases.
- [ ] **SOV-P06-T14** — Verify changing the render camera doesn't change sun position, thermal updates or local time.
- [ ] **SOV-P06-T15** — Verify future crop/visibility/snow consumers can query continuous, time-aware thermal values.

**Exit gate 06:** A winter mountain valley experiences meaningful daylight/temperature changes distinct from summer, and local sun/thermal fields are queryable at encounter time.
