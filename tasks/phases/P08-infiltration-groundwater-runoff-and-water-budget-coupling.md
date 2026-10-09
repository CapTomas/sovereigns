## Phase 08 — Infiltration, groundwater, runoff and water-budget coupling
**Depends on:** 07 and 05.  
**Outcome:** Atmospheric precipitation becomes conserved landscape water and delayed hydrological response.  
**GDB:** §§3, 5.8–5.12, 5.21, 32.10.

- [ ] **SOV-P08-T01** — Define surface interception, infiltration, soil stores, groundwater and runoff as distinct water reservoirs.
- [ ] **SOV-P08-T02** — Define physical units and auditable transfers between atmospheric, surface, soil and groundwater stores.
- [ ] **SOV-P08-T03** — Give material-dependent infiltration capacities from rock, soil texture, slope and existing saturation.
- [ ] **SOV-P08-T04** — Implement soil-layer water retention and realistic saturation limits without negative stores.
- [ ] **SOV-P08-T05** — Carry excess precipitation into surface runoff using topography and drainage direction.
- [ ] **SOV-P08-T06** — Model evaporation from wet ground and transpiration from vegetation as outflows with proper limitations.
- [ ] **SOV-P08-T07** — Create delayed groundwater recharge, storage and baseflow supporting rivers after rain ends.
- [ ] **SOV-P08-T08** — Calculate springs and wet spots where groundwater intersects relevant landforms.
- [ ] **SOV-P08-T09** — Maintain landscape moisture histories rather than assigning instantaneous wet/dry terrain tags.
- [ ] **SOV-P08-T10** — Resolve heavy rainfall on frozen or sealed ground differently from rainfall on permeable ground.
- [ ] **SOV-P08-T11** — Ensure neighboring cells exchange surface runoff without teleporting across divides.
- [ ] **SOV-P08-T12** — Respect world/chunk boundaries when water crosses tiles or regions.
- [ ] **SOV-P08-T13** — Prevent double-counting one precipitation event in soil, streams and groundwater.
- [ ] **SOV-P08-T14** — Add transparent water-budget diagnostics per cell, watershed, region and global domain.
- [ ] **SOV-P08-T15** — Add debug overlays for soil moisture, runoff, infiltration, storage and groundwater recharge.
- [ ] **SOV-P08-T16** — Save and reload water stores without jump in river baseflow or land wetness.
- [ ] **SOV-P08-T17** — Test rain pulse on dry sand, wet clay, steep rock, flat wetland and mixed catchment.
- [ ] **SOV-P08-T18** — Test zero precipitation, extreme storms, saturated terrain and extended drought for numerical stability.
- [ ] **SOV-P08-T19** — Profile update cost and make water-storage updates independent of renderer visibility.
- [ ] **SOV-P08-T20** — Demonstrate that a single rain event reaches soil, streams and groundwater on different schedules.

**Exit gate 08:** Water is conserved within documented boundaries; a storm creates surface runoff and infiltration, groundwater responds with delay, and a saved/unloaded catchment gives the same outcome.
