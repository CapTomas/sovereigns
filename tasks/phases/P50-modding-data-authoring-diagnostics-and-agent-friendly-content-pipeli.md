## Phase 50 — Modding, data authoring, diagnostics and agent-friendly content pipeline
**Depends on:** 49 and stable game systems.  
**Outcome:** Data-driven content can evolve without undocumented coupling or world corruption.  
**GDB:** §§0, 12, 18, 28.4, 29, 33.

- [ ] **SOV-P50-T01** — Define stable content formats for plants, crops, commodities, buildings and production transformations.
- [ ] **SOV-P50-T02** — Define stable content formats for unit equipment, formations, military traditions and policies.
- [ ] **SOV-P50-T03** — Define stable content formats for generated cultures, names, laws and historical event templates.
- [ ] **SOV-P50-T04** — Build validation rules for units, ranges, references, circular dependencies and missing assets.
- [ ] **SOV-P50-T05** — Reject impossible commodity conversions and negative-cost production recipes at load time.
- [ ] **SOV-P50-T06** — Keep game-code mechanics separate from data-defined balancing parameters.
- [ ] **SOV-P50-T07** — Provide declarative content overrides with explicit priority and conflict detection.
- [ ] **SOV-P50-T08** — Build world seed and content-version repro reporting for modded crash reports.
- [ ] **SOV-P50-T09** — Document allowable extensions and systems that require engineering rather than pure data.
- [ ] **SOV-P50-T10** — Provide a debug inspector for any world field, settlement, shipment, polity and regiment.
- [ ] **SOV-P50-T11** — Provide time-travel or deterministic reproduction tools for named fixtures and failures.
- [ ] **SOV-P50-T12** — Add simulation audit dashboards for mass balance, population and resource changes.
- [ ] **SOV-P50-T13** — Add cross-system dependency maps identifying data producers and consumers.
- [ ] **SOV-P50-T14** — Create authoring documentation for content contributors without exposing private implementation details.
- [ ] **SOV-P50-T15** — Build data catalog generation showing all gameplay parameters and their intended units.
- [ ] **SOV-P50-T16** — Define sandbox limits and error reporting for community-authored content.
- [ ] **SOV-P50-T17** — Test broken asset references, duplicate IDs, incompatible schema versions and missing localization.
- [ ] **SOV-P50-T18** — Test a custom crop requiring new growing conditions without modifying the weather system.
- [ ] **SOV-P50-T19** — Test an equipment balance change correctly affecting manufacture, recruitment and tactical outcomes.

**Exit gate 50:** New compatible content can be authored and validated without breaking the simulation contract; failures are diagnosable and reproducible.
