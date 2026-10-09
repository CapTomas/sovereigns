## Phase 32 — Battle command foundation and full regiment formation geometry
**Depends on:** 13 and 31.  
**Outcome:** A large formation can obey tactical orders predictably within complex terrain.  
**GDB:** §§22.4–22.6, 22.12, 28.3.

- [ ] **SOV-P32-T01** — Define regiment footprint from personnel, rank depth, width, spacing and formation type.
- [ ] **SOV-P32-T02** — Implement selection, multiselection, group assignment and queued orders.
- [ ] **SOV-P32-T03** — Implement move, rotate, face, hold, maintain formation, advance and withdraw.
- [ ] **SOV-P32-T04** — Support drag-to-place formation frontage and facing previews before order submission.
- [ ] **SOV-P32-T05** — Implement open, dense, column and other approved basic formations where unit doctrine permits.
- [ ] **SOV-P32-T06** — Calculate movement of formation center and constituent subgroups without clipping terrain.
- [ ] **SOV-P32-T07** — Enforce realistic turn rate, acceleration, cohesion loss and recovery on uneven terrain.
- [ ] **SOV-P32-T08** — Implement terrain-aware collision between formations, obstacles and static structures.
- [ ] **SOV-P32-T09** — Handle queueing/crowding around bridges, paths, gates and forest chokepoints.
- [ ] **SOV-P32-T10** — Give difficult terrain real cost and formation disruption rather than only combat-stat penalties.
- [ ] **SOV-P32-T11** — Support hills, riverbanks, ditches and wet ground as query-based movement constraints.
- [ ] **SOV-P32-T12** — Include commander presence, communications and order delay at a readable level.
- [ ] **SOV-P32-T13** — Allow regroup, rally, stop, resume and individual or combined regiment orders.
- [ ] **SOV-P32-T14** — Avoid instant perfect formation reorganization after abrupt facing changes.
- [ ] **SOV-P32-T15** — Scale to many simultaneous formations without requiring independent full AI for every soldier.
- [ ] **SOV-P32-T16** — Support lightweight soldier visual representations independently of the underlying formation simulation.
- [ ] **SOV-P32-T17** — Add transparent overlays for formation footprint, cohesion, command reach and route obstacles.
- [ ] **SOV-P32-T18** — Test two long lines executing simultaneous advances and turning around a forest edge.
- [ ] **SOV-P32-T19** — Test regiments crossing one bridge without interpenetrating or teleporting.
- [ ] **SOV-P32-T20** — Measure responsiveness and simulation cost for increasing regiment counts.

**Exit gate 32:** A player can control substantial formations on the real battlefield, with reliable orders, terrain-aware motion and reproducible collision/cohesion behavior.
