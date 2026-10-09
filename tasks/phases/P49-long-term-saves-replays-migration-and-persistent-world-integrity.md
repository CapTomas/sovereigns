## Phase 49 — Long-term saves, replays, migration and persistent world integrity
**Depends on:** 37, 42 and evolving system schemas.  
**Outcome:** Large procedural worlds remain safely resumable through updates and crashes.  
**GDB:** §§0, 3.11, 24.2, 29.3, 29.8, 32.7.

- [ ] **SOV-P49-T01** — Define authoritative save contents for world seed, generation version and persistent physical modifications.
- [ ] **SOV-P49-T02** — Define save contents for current weather, soil water, snow, rivers, ecological stores and world clock.
- [ ] **SOV-P49-T03** — Persist historical events, changed structures, settlements, ownership and occupation consistently.
- [ ] **SOV-P49-T04** — Persist cohorts, inventories, shipments, prices, construction progress and political agreements.
- [ ] **SOV-P49-T05** — Persist regiments, army orders, injuries, ammo, fatigue, supply and active sieges.
- [ ] **SOV-P49-T06** — Preserve pending time-scheduled events and guard against double application after loading.
- [ ] **SOV-P49-T07** — Support checkpoints during strategic turns and between individual chronological events.
- [ ] **SOV-P49-T08** — Specify supported in-battle checkpoint policy and guarantee crash-safe return at allowed points.
- [ ] **SOV-P49-T09** — Keep source-of-truth world fields and regenerable caches clearly separated in save data.
- [ ] **SOV-P49-T10** — Add save schema/version migrations with explicit compatibility policy and rollback backup.
- [ ] **SOV-P49-T11** — Represent content/mod version signatures and provide clear incompatibility errors.
- [ ] **SOV-P49-T12** — Detect partial/corrupted saves with checksums or integrity validation and recovery options.
- [ ] **SOV-P49-T13** — Write saves atomically to prevent power-loss corruption of the only valid campaign.
- [ ] **SOV-P49-T14** — Keep bounded event logs for causal explanations while preserving important historical chronicles.
- [ ] **SOV-P49-T15** — Reconstruct fine battlefield terrain deterministically from authoritative world and sparse edits.
- [ ] **SOV-P49-T16** — Create snapshot comparison tools for world, economy, political and battle states.
- [ ] **SOV-P49-T17** — Test saving before storm, during flood, after snowmelt and through winter transition.
- [ ] **SOV-P49-T18** — Test saving mid-march, mid-shipment, mid-siege and after a territory treaty transfer.
- [ ] **SOV-P49-T19** — Test many sequential load/re-save cycles for drift or growing corruption.
- [ ] **SOV-P49-T20** — Test version upgrade migrations with documented golden-world fixtures.

**Exit gate 49:** Saved campaigns survive restart, supported upgrades and interruptions without changing physical causality, losing history or applying outcomes twice.
