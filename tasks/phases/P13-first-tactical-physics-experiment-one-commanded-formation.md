## Phase 13 — First tactical physics experiment: one commanded formation
**Depends on:** 12.  
**Outcome:** Prove terrain and physical state change control of a visible regiment before building the whole economy.  
**GDB:** §§5.14–5.17, 19.5, 22.4–22.10, 32.6.

- [ ] **SOV-P13-T01** — Define one temporary regiment actor with size, footprint, facing, speed and cohesion.
- [ ] **SOV-P13-T02** — Display its formation as temporary 2D markers without prescribing final soldier artwork.
- [ ] **SOV-P13-T03** — Support selection, right-click movement, orientation and cancellation of orders.
- [ ] **SOV-P13-T04** — Keep formation movement in world units and tactical simulation time, not pixel-space shortcuts.
- [ ] **SOV-P13-T05** — Resolve simple pathing and collision against slopes, water and impassable objects.
- [ ] **SOV-P13-T06** — Track terrain-dependent acceleration, turning, arrival and fatigue without random speed modifiers.
- [ ] **SOV-P13-T07** — Connect wet soil, mud, snow and frozen ground to measurable movement resistance.
- [ ] **SOV-P13-T08** — Connect vegetation density and obstacles to formation passability and possible disruption.
- [ ] **SOV-P13-T09** — Support a rudimentary approach/withdraw task and prevent movement through hostile walls.
- [ ] **SOV-P13-T10** — Build a fixed-arrow experiment whose trajectory samples the shared local wind field.
- [ ] **SOV-P13-T11** — Validate firing downwind, upwind and crosswind changes projectile paths geometrically.
- [ ] **SOV-P13-T12** — Compare open-field, forest-edge, hill and ford movement under dry and storm conditions.
- [ ] **SOV-P13-T13** — Check that current rain influences mobility through ground state and previous rainfall history.
- [ ] **SOV-P13-T14** — Allow pause, slow speed, single-step and restart of the tactical reference scenario.
- [ ] **SOV-P13-T15** — Add deterministic replay of movement and projectile experiment with identical inputs.
- [ ] **SOV-P13-T16** — Show coordinate/height/wind/wetness inspector readings inside the battlefield.
- [ ] **SOV-P13-T17** — Measure pathfinding and regiment simulation cost in simple and obstacle-heavy scenes.
- [ ] **SOV-P13-T18** — Create a recorded video or replay of dry-to-wet change altering both marching and shooting.
- [ ] **SOV-P13-T19** — Write a focused list of missing battle mechanics without prematurely adding all combat systems.

**Exit gate 13 — Checkpoint C — Playable physical proof:** One controllable regiment demonstrably reacts to actual slope, river, wet soil and wind-sampled projectile behavior with reproducible outcomes.
