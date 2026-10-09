## Phase 36 — Tactical opponent AI, battle automation and reinforcement tactics
**Depends on:** 35.  
**Outcome:** The player can fight an opponent that reasons about terrain and formation objectives.  
**GDB:** §§22–25, 25.5–25.6.

- [ ] **SOV-P36-T01** — Define tactical AI observation boundaries matching the information available to a commander.
- [ ] **SOV-P36-T02** — Implement basic line deployment based on approach directions and strategic objective.
- [ ] **SOV-P36-T03** — Evaluate ridge, cover, river crossings, road access and defensive chokepoints.
- [ ] **SOV-P36-T04** — Maintain reserves rather than committing all regiments instantly.
- [ ] **SOV-P36-T05** — Coordinate infantry, cavalry and ranged formations with clear roles.
- [ ] **SOV-P36-T06** — Use physical line-of-sight and realistic terrain to determine ranged positioning.
- [ ] **SOV-P36-T07** — Avoid suicidal attacks against closed walls, deep rivers or inaccessible slopes.
- [ ] **SOV-P36-T08** — Manage skirmishing, disengagement, defensive hold and reasonable pursuit.
- [ ] **SOV-P36-T09** — Respond to flanking danger and broken formation lines with local reorientation.
- [ ] **SOV-P36-T10** — Use fatigue and cohesion to time rests, attacks and cavalry charges.
- [ ] **SOV-P36-T11** — Bring plausible reinforcements onto the battlefield from actual campaign routes.
- [ ] **SOV-P36-T12** — Protect vulnerable rear units and baggage when operational context requires it.
- [ ] **SOV-P36-T13** — Respect tactical AI uncertainty and refrain from reading invisible enemy positions.
- [ ] **SOV-P36-T14** — Support battle AI control of allied regiments only when explicitly delegated.
- [ ] **SOV-P36-T15** — Include a deterministic fixture suite to measure AI execution and battle outcomes.
- [ ] **SOV-P36-T16** — Create AI explanation logs for deployment, target selection, retreat and reserve commitment.
- [ ] **SOV-P36-T17** — Test attacking a defended ford, holding a ridge and encountering forest ambushes.
- [ ] **SOV-P36-T18** — Test AI beating a stationary unprotected formation without impossible bonuses.
- [ ] **SOV-P36-T19** — Test tactical player controls against human-input cancellation, pausing and speed changes.
- [ ] **SOV-P36-T20** — Run large AI-vs-AI battle stress tests for deadlocks, endless pursuit and unfair routing.

**Exit gate 36:** An AI opponent executes coherent battlefield plans using the same fog, movement and environmental limits, with no hidden omniscience.
