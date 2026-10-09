## Phase 03 — Queryable multiresolution physical-field atlas
**Depends on:** 02.  
**Outcome:** Every relevant world coordinate has consistent physical meaning without storing everything at pixel resolution.  
**GDB:** §§3, 26.13, 29.1–29.11, 30.8.

- [ ] **SOV-P03-T01** — Design the global spatial hierarchy: world, macro region, local terrain and high-detail tactical region.
- [ ] **SOV-P03-T02** — Define field families: elevation, geology, water, heat, air, ground, vegetation, infrastructure and human modification.
- [ ] **SOV-P03-T03** — Define per-field units, ranges, no-data behavior, interpolation and valid precision/error budgets.
- [ ] **SOV-P03-T04** — Implement deterministic arbitrary-coordinate sampling of static physical fields.
- [ ] **SOV-P03-T05** — Support vector fields for direction/speed quantities such as wind and flow, not only scalar labels.
- [ ] **SOV-P03-T06** — Support continuous-feature geometry with durable identities for rivers, banks, roads, shorelines and ridgelines.
- [ ] **SOV-P03-T07** — Implement spatial indexing, chunk loading/unloading and querying unresident regions.
- [ ] **SOV-P03-T08** — Implement generated base tiles with mutable deltas and nondestructive local refinement.
- [ ] **SOV-P03-T09** — Implement safe aggregation from local fields to regional statistics and consistent local sampling from macro fields.
- [ ] **SOV-P03-T10** — Define authoritative storage versus recomputable caches; prevent cached values from becoming disconnected truths.
- [ ] **SOV-P03-T11** — Add dependency-driven invalidation: a changed river/road/forest must refresh relevant consumers.
- [ ] **SOV-P03-T12** — Add boundary continuity tests for adjacent chunks, coordinate seams and nested resolutions.
- [ ] **SOV-P03-T13** — Expose real diagnostic sampling in the existing location inspector: coordinate, field units, input values, derived result, state classification, last update and source/owner provenance; distinguish unavailable data.
- [ ] **SOV-P03-T14** — Add greyscale/vector/field diagnostic map overlays to the reusable inspection shell, sampled from authoritative queries with layer selection, scale legends and explicit no-data states.
- [ ] **SOV-P03-T15** — Add data migration strategy for newly added fields and legacy world versions.
- [ ] **SOV-P03-T16** — Benchmark large coordinate queries, chunk changes, aggregation and memory pressure.
- [ ] **SOV-P03-T17** — Verify a remote, unloaded coordinate returns the same durable base identity after reloading and refinement.

**Exit gate 03:** The renderer, world generator and future battle client can query one authoritative physical atlas at multiple scales with preserved identity and bounded memory; the earliest implemented fields are visibly inspectable through the reusable shell and its location inspector.
