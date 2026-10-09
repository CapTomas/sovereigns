## Phase 09 — Live rivers, lakes, floods, snow, ice and seasonal thaw
**Depends on:** 08, 06 and drainage topology.  
**Outcome:** Climate moves real water through channels, lakes, snowpacks and floodplains.  
**GDB:** §§4–6, 20, 22, 23, 32.10.

- [ ] **SOV-P09-T01** — Separate static channel geometry and watershed topology from dynamic water storage and discharge.
- [ ] **SOV-P09-T02** — Route rainfall-fed runoff and baseflow to tributaries and confluences in chronological order.
- [ ] **SOV-P09-T03** — Track flow and seasonal channel depth, width, velocity and crossing status from discharge.
- [ ] **SOV-P09-T04** — Represent lakes with input, outflow, evaporation, capacity and changing surface level.
- [ ] **SOV-P09-T05** — Represent overflow into adjacent low-lying floodplains as persistent temporary water state.
- [ ] **SOV-P09-T06** — Include inland and closed-basin water behavior where appropriate rather than forced sea outlets.
- [ ] **SOV-P09-T07** — Accumulate snow water equivalent separately from visual snow depth and frozen-ground state.
- [ ] **SOV-P09-T08** — Partition precipitation into rain, snow and mixed conditions with a thermal-profile approximation.
- [ ] **SOV-P09-T09** — Model snowfall accumulation, compaction, melting and refreezing with heat history.
- [ ] **SOV-P09-T10** — Route snowmelt downhill and into rivers with a physically meaningful time lag.
- [ ] **SOV-P09-T11** — Track river/lake ice formation and thaw separately from instantaneous ambient temperature.
- [ ] **SOV-P09-T12** — Make floodwater, water depth, current, frozen ground and snow cover queryable in world coordinates.
- [ ] **SOV-P09-T13** — Compute navigability, fording, bridge exposure and bank accessibility from physical state.
- [ ] **SOV-P09-T14** — Distinguish permanent channel banks from temporary inundation geometry in the local view.
- [ ] **SOV-P09-T15** — Preserve water levels and snowpacks across save/load, fast-forward and unloaded regions.
- [ ] **SOV-P09-T16** — Add snow, flood, channel-depth, river-discharge and freeze-state debugging overlays.
- [ ] **SOV-P09-T17** — Test spring thaw causing floods well after the mountain precipitation event.
- [ ] **SOV-P09-T18** — Test river crossing availability through summer drought, storm peak and winter freeze.
- [ ] **SOV-P09-T19** — Test lake overflow, lowland backwater, deltas and ocean/river boundary conditions.
- [ ] **SOV-P09-T20** — Stress-test river and lake stores for negative values, vanishing water and unbounded flood oscillations.
- [ ] **SOV-P09-T21** — Show a mountain storm changing a downstream ford while the camera stays elsewhere.

**Exit gate 09 — Checkpoint B — Hydrology:** A river responds to its entire catchment; a mountain snowfall can cause a later downstream flood; crossing status follows state rather than scripted seasonal tags.
