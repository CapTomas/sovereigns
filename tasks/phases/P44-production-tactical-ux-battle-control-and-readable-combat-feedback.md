## Phase 44 — Production tactical UX, battle control and readable combat feedback
**Depends on:** 38 and formation/combat simulation.  
**Outcome:** Large battles can be commanded precisely under 2D orthographic camera and readable overlays.  
**GDB:** §§22.3–22.4, 26.7, 26.9, 27–28.

- [ ] **SOV-P44-T01** — Build deployment editor with legal positions, terrain heights, obstacles and facing.
- [ ] **SOV-P44-T02** — Implement robust multi-regiment selection, formations, groups and predictable order modifiers.
- [ ] **SOV-P44-T03** — Create camera pan/zoom/recenter and command filtering for cluttered battlefields.
- [ ] **SOV-P44-T04** — Show regiment identity, manpower, fatigue, morale, cohesion and ammunition.
- [ ] **SOV-P44-T05** — Show order destinations, turn paths, attack directions and movement conflicts clearly.
- [ ] **SOV-P44-T06** — Expose wind direction, visibility, firing arcs, crossing depth and ground condition where relevant.
- [ ] **SOV-P44-T07** — Distinguish selected and hostile formations without relying exclusively on color.
- [ ] **SOV-P44-T08** — Show partial information and blocked line-of-sight instead of omniscient targeting.
- [ ] **SOV-P44-T09** — Provide contextual feedback when an order is impossible due to terrain or enemy blockade.
- [ ] **SOV-P44-T10** — Make routing, panic, pursuit and surrender distinguishable through readable UI cues.
- [ ] **SOV-P44-T11** — Build pause, speed controls and optional slow motion with time/replay correctness.
- [ ] **SOV-P44-T12** — Create battle summary with losses, prisoners, equipment and terrain damage.
- [ ] **SOV-P44-T13** — Support keyboard shortcuts and remappable tactical actions for frequent orders.
- [ ] **SOV-P44-T14** — Design conservative selection behavior for dense clusters and stacked formations.
- [ ] **SOV-P44-T15** — Support large-army command with army groups, control groups and delegated basic AI.
- [ ] **SOV-P44-T16** — Allow important messages without interrupting manual commands or hiding battlefield actions.
- [ ] **SOV-P44-T17** — Test low-end screen resolutions and dense, visually busy formations for legibility.
- [ ] **SOV-P44-T18** — Run battle playtests with novice users to discover control ambiguity and formation errors.
- [ ] **SOV-P44-T19** — Verify display effects never alter hit detection, projectile physics or environmental state.

**Exit gate 44:** Players can deploy and control large armies accurately, understand why attacks fail or succeed and see true environmental effects without UI overload.
