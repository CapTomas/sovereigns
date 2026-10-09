## Phase 34 — Ranged combat, visibility, wind and evolving battlefield weather
**Depends on:** 32–33 and physical atmosphere.  
**Outcome:** Arrows, missiles and detection respond to physical state rather than decorative penalties.  
**GDB:** §§5.14–5.17, 22.8, 22.10–22.14, 32.6.

- [ ] **SOV-P34-T01** — Represent bows, crossbows and other approved projectiles through data-defined parameters.
- [ ] **SOV-P34-T02** — Model projectile flight with appropriate gravity, range, initial velocity and wind sampling.
- [ ] **SOV-P34-T03** — Sample wind in consistent world coordinates with direction and temporal evolution during flight.
- [ ] **SOV-P34-T04** — Use physical height fields, structures and forests for line-of-sight and obstruction.
- [ ] **SOV-P34-T05** — Separate known enemy position, visible enemy position and firing confidence.
- [ ] **SOV-P34-T06** — Model low light, mist, precipitation and smoke as physical visibility constraints.
- [ ] **SOV-P34-T07** — Represent ammunition inventory, reload preparation and operator fatigue.
- [ ] **SOV-P34-T08** — Implement aimed volleys, free fire, cease fire and target-priority orders.
- [ ] **SOV-P34-T09** — Allow terrain behind a target to obstruct or intercept shots rather than assuming a flat plane.
- [ ] **SOV-P34-T10** — Apply shields, armor and formation density to hits without ignoring projectile trajectory.
- [ ] **SOV-P34-T11** — Use existing weather and soil conditions to reduce archer maneuverability appropriately.
- [ ] **SOV-P34-T12** — Handle range loss/gain from elevation and realistic firing arcs within chosen abstraction.
- [ ] **SOV-P34-T13** — Ensure source wind and weather can change during a long tactical battle.
- [ ] **SOV-P34-T14** — Support localized smoke and fire as stateful disturbances where relevant.
- [ ] **SOV-P34-T15** — Show wind direction, firing arc, visibility and obstruction feedback without omniscient aiming aid.
- [ ] **SOV-P34-T16** — Test calm, strong crosswind, tailwind and shifting gusts against same fixed targets.
- [ ] **SOV-P34-T17** — Test woodland target, ridge obstruction, night battle and rainfall near freezing point.
- [ ] **SOV-P34-T18** — Test arrows cannot hit through impassable walls or unseen formations by accident.
- [ ] **SOV-P34-T19** — Profile thousands of representative projectiles and choose safe approximations under documented tests.

**Exit gate 34:** A shared wind field changes actual projectile travel, and terrain/light/weather constrain sight and engagement without separate battlefield weather rules.
