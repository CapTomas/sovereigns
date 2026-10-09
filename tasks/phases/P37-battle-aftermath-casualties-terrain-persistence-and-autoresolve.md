## Phase 37 — Battle aftermath, casualties, terrain persistence and autoresolve
**Depends on:** 35–36, world persistence and campaign clock.  
**Outcome:** Every battle returns consistent, permanent consequences to its source world.  
**GDB:** §§22.13–22.14, 24, 31.5, 32.6–32.7.

- [ ] **SOV-P37-T01** — Define one authoritative battle outcome ledger for all killed, wounded, prisoners and survivors.
- [ ] **SOV-P37-T02** — Deduct equipment, ammunition, provisions and mounts consumed or lost in battle.
- [ ] **SOV-P37-T03** — Commit commander injuries, experience, morale and regiment condition changes to campaign.
- [ ] **SOV-P37-T04** — Commit elapsed tactical time once to the shared world clock and resolve pending external events.
- [ ] **SOV-P37-T05** — Represent surrender, retreat and pursuit exits through valid operational positions.
- [ ] **SOV-P37-T06** — Commit damage to bridges, fieldworks, fortifications and settlements as sparse world edits.
- [ ] **SOV-P37-T07** — Commit forest fires, cleared vegetation and relevant flood/earthwork changes to environmental state.
- [ ] **SOV-P37-T08** — Track recovery of wounded, prisoners, captured stores and damaged property over time.
- [ ] **SOV-P37-T09** — Implement manual-battle outcomes as deterministic campaign events with provenance.
- [ ] **SOV-P37-T10** — Create autoresolve inputs from the same manpower, weather, terrain, fortification and supply data.
- [ ] **SOV-P37-T11** — Calibrate autoresolve distributions against representative fully simulated tactical scenarios.
- [ ] **SOV-P37-T12** — Prevent autoresolve from conjuring casualties, provisions, weapons or unearned territory.
- [ ] **SOV-P37-T13** — Report uncertainty and expected losses before the player chooses automatic resolution.
- [ ] **SOV-P37-T14** — Handle tactical loss with retreating surviving regiments without instant disbanding.
- [ ] **SOV-P37-T15** — Handle battle at turn boundary and subsequent chronological economic/world updates.
- [ ] **SOV-P37-T16** — Support save/load at pre-battle, in-battle supported checkpoint and post-battle boundaries.
- [ ] **SOV-P37-T17** — Test repeat visit to same battlefield after destroyed bridge and harvested forest.
- [ ] **SOV-P37-T18** — Test automated/manual outcomes for consistent advantages from wet terrain and fortification.
- [ ] **SOV-P37-T19** — Run crash/replay tests ensuring a battle cannot apply aftermath twice.
- [ ] **SOV-P37-T20** — Create a complete battle chronicle linking cause, participants, geography and consequences.

**Exit gate 37 — Checkpoint F — War loops back:** A repeated fight at the same place sees previous damage; all troops, time, territory and world changes survive deterministic return to campaign.
