## Phase 02 — Authoritative world identity, clock and deterministic simulation kernel
**Depends on:** 01.  
**Outcome:** One world exists in time, and every future subsystem reads the same state.  
**GDB:** §§2.3, 3, 26.13, 29–30.

- [ ] **SOV-P02-T01** — Define durable world ID, shared seed, generation version and immutable geographical coordinate contract.
- [ ] **SOV-P02-T02** — Define the simulated calendar, days, seasons, hours, local solar time and chronology conventions.
- [ ] **SOV-P02-T03** — Establish one authoritative simulation clock independent of rendering frame rate.
- [ ] **SOV-P02-T04** — Define simulation scheduler priorities, subsystem update cadence and time-step ownership.
- [ ] **SOV-P02-T05** — Separate generated base state, current mutable state, derived queries and player-observed information.
- [ ] **SOV-P02-T06** — Set up deterministic random streams keyed by system, place, event and world seed; prohibit incidental UI randomness affecting outcomes.
- [ ] **SOV-P02-T07** — Implement an ordered world-event ledger with event identity, causal origin, timestamp and affected state.
- [ ] **SOV-P02-T08** — Add stable IDs for natural features, settlements, facilities, armies, political entities and later tactical objects.
- [ ] **SOV-P02-T09** — Define authoritative write boundaries and explicit invalidation for every derived/cache layer.
- [ ] **SOV-P02-T10** — Establish spatial query semantics for unloaded, loading, loaded and refined areas.
- [ ] **SOV-P02-T11** — Implement pause, step, accelerated stepping and non-skippable decision-event interrupts in the kernel; connect the inspection shell's time controls to the authoritative clock through explicit commands.
- [ ] **SOV-P02-T12** — Provide delta/event persistence primitives without coupling them to the visual client.
- [ ] **SOV-P02-T13** — Create field-value provenance diagnostics: owner, source field, derivation and last-update time.
- [ ] **SOV-P02-T14** — Add assertions for invalid temporal order, missing feature IDs and illegal duplicate authoritative owners.
- [ ] **SOV-P02-T15** — Prove repeating a seed and command stream produces identical expected world-state checksums/tolerances.
- [ ] **SOV-P02-T16** — Prove pause, varied render frame rates and loaded/unloaded view changes do not alter simulation results.
- [ ] **SOV-P02-T17** — Define versioned save format policy and integrity checks before any persistent gameplay objects are added.

**Exit gate 02:** A headless world runs deterministically through time, supports replay and serializable authoritative events, and never depends on display frame rate; the inspection shell can pause, step and accelerate that same world and display its actual time.
