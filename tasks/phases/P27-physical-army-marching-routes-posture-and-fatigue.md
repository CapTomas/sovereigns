## Phase 27 — Physical army marching, routes, posture and fatigue
**Depends on:** 26, terrain/water, transport and access diplomacy.  
**Outcome:** Armies move across real terrain and contact opportunities arise from geography.  
**GDB:** §§19.9, 20, 21.4, 5.15.

- [ ] **SOV-P27-T01** — Define army footprint, operational coordinates, formation column width and baggage constraints.
- [ ] **SOV-P27-T02** — Implement marching pathfinding that uses slopes, surfaces, crossings, roads and route permissions.
- [ ] **SOV-P27-T03** — Calculate travel time and speed from composition, weather, terrain, condition and local resistance.
- [ ] **SOV-P27-T04** — Make snow, recent rainfall and ground saturation alter real route choices through the physical atlas.
- [ ] **SOV-P27-T05** — Implement march postures: normal, forced, cautious, concealed, patrol, raid and withdrawal.
- [ ] **SOV-P27-T06** — Track fatigue, rest, illness exposure and march cohesion over time.
- [ ] **SOV-P27-T07** — Define terrain bottlenecks, bridge carrying limits and narrow-road movement capacity.
- [ ] **SOV-P27-T08** — Support army order queues, stop/redirect and predictable execution within seven-day turns.
- [ ] **SOV-P27-T09** — Create route previews with travel-time uncertainty, supply pressure and passage legality.
- [ ] **SOV-P27-T10** — Represent delayed arrival of rearguards, trains and separate detachments.
- [ ] **SOV-P27-T11** — Account for damaged infrastructure and seasonal closures without secret rerolls.
- [ ] **SOV-P27-T12** — Prevent armies from crossing impassable mountains, deep rivers or hostile walls by pathfinding error.
- [ ] **SOV-P27-T13** — Make crossing a narrow bridge expose army position and time to possible interception.
- [ ] **SOV-P27-T14** — Support intercept, screen, patrol, retreat and withdraw with meaningful movement constraints.
- [ ] **SOV-P27-T15** — Compute army visibility/observation footprint from terrain and light at relevant times.
- [ ] **SOV-P27-T16** — Avoid automatic battle simply because two abstract province tokens share a region.
- [ ] **SOV-P27-T17** — Test same army on dry summer road, spring mire, winter pass and flooded ford.
- [ ] **SOV-P27-T18** — Test border transit with access permitted, denied and revoked mid-march.
- [ ] **SOV-P27-T19** — Test two armies occupying opposite sides of a river without battle contact.
- [ ] **SOV-P27-T20** — Test long-distance movement persists after a save in the middle of a turn.

**Exit gate 27:** A route is chosen from real accessible land, armies move through elapsed time, and a storm alters travel without arbitrary march-point penalties.
