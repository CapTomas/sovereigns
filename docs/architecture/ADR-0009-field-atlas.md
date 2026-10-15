# ADR-0009 — Multi-resolution physical-field atlas

**Status:** Accepted on merge of its introducing PR (SOV-P03-T01 to T17).
**Decision date:** 2026-10-14
**Scope:** How every physical field of the world is defined, stored, sampled at any coordinate and scale, cached, invalidated, persisted and inspected. Field contents come from later phases (elevation from Phase 04, weather from Phase 07 and so on); this ADR fixes the machinery and the contract they plug into.

## Context

Spec §3.2 lists the field families; §3.3 separates stored, dynamic, derived, cached, observed and visual values; §3.4 asks for a chunked multi-resolution world where every point queries one hierarchy and zooming reveals detail, not a different world; §3.10 requires location queries that agree across subsystems; §29.2, §29.9–29.11 require background areas to stay coherent, conservative integration across grids, provenance, invalidation, and performance spent by consequence. ADR-0004 fixes SI units, the y-up planar frame and cell-centred grids; ADR-0007 fixes state classes, ownership and query semantics; ADR-0008 fixes saves.

## Decision

### Spatial hierarchy (T01)

Four nested levels, each 16 times finer than the one above, all anchored at the world origin:

| Level | Cell | Chunk (64 × 64 cells) | Typical fields |
|---|---|---|---|
| Macro | 4096 m | 262 km | Climate, synoptic atmosphere, macro statistics |
| Regional | 256 m | 16 km | Elevation, hydrology, soils, vegetation |
| Local | 16 m | 1 km | Land use, ground near settlements and routes |
| Tactical | 1 m | 64 m | Refined battlefield terrain, materialized where needed |

Cell (i, j) of a level covers [iΔ, (i+1)Δ) × [jΔ, (j+1)Δ), sampled at its centre (ADR-0004 §2). Cell indices are integers, so lookups are exact and platform-independent.

### Field contracts (T02, T03)

`FieldCatalog` lists every field as a `FieldDefinition`:

- key, family, unit (SI), kind (scalar, vector as east and north, or category);
- class (base, stored, dynamic or derived);
- native level, interpolation (bilinear or nearest) and refinement (interpolate or conserve);
- valid range, storage precision, owner, dependencies and delivering phase;
- a no-data meaning (why a value can be missing; a missing value is never zero) and a zero meaning (what the value 0 means, such as calm air or dry soil; spec §3.5);
- a schema version for saves.

All nine requested family groups (elevation, geology, water, heat, air, ground, vegetation, infrastructure, human modification) are covered through the spec §3.2 families. Fields of later phases are listed with their intended contract and stay unavailable until a provider is registered. Their phase may refine the contract by amending this ADR.

### Sampling (T04, T05)

`FieldAtlas.Sample(field, point, level)` returns:

- the value (one component, or east and north);
- its basis and classification;
- the grid inputs with weights;
- the time its inputs last changed.

Sampling at each level works as follows:

- **Native level:** bilinear interpolation between the four nearest cell centres, using only + − × ÷, so results are bit-identical across platforms. Near the world's edge, indices clamp to edge cells. Outside the world, the sample is unavailable.
- **Coarser level:** each coarse cell's value is the exact mean of the native cells it contains inside the world. A native cell partly outside the world counts by its part inside, so coarse values agree with `Aggregate`. The point is then interpolated among coarse cells.
- **Finer level:** the point lies on the native bilinear surface. A **conserve** field shifts it, per native cell, by a correction so that the mean of its refined values equals the native value. The correction uses the closed form of the bilinear mean, which weights the 3 × 3 neighbourhood (1/8, 3/4, 1/8) per axis. The shifted surface s is bilinear between the cell's centre, edge midpoints and corners, so its extremes are among those nine points. Where they leave the field's range, a limiter θ ∈ [0, 1] blends it towards the cell value v as v + θ (s − v). That keeps both the mean and the range, so refined water is never negative. Interpolated fields are continuous; conserved fields keep totals and may step at native cell edges.
- **Categories** (lithology, land use) use nearest interpolation and are never refined conservatively. At coarser levels they will take the majority class, with ties going to the lowest class index. Until the first category field (Phase 04) implements that, the atlas refuses to register category fields, so no coarse level averages class indices.

Analytic derived fields, such as latitude and local solar time, are evaluated directly at the point. Grid-derived fields, such as a gradient, are computed per native cell from their sources: central differences, one-sided at the world's edge.

### Storage, deltas and refinement (T08, T10)

- **Base** data comes from a deterministic source keyed by seed and generator version, chunk by chunk, and is never saved (ADR-0008).
- **Stored** values are sparse per-cell writes (overrides) on top of base, saved with the time each was written. A write that restores the base value is kept too, so the time every cell last changed is saved state.
- **Dynamic** fields (later phases) save their state at the save time.
- **Derived** fields are never saved.

Refinement to a finer level is computed from native data and never modifies it. A conserve field's refinement terms (a correction and a limiter per native cell) are cached per native chunk as resident state "refined", and can be discarded at any time. Interpolated fields need no terms: their finer levels are the native surface. Persistent tactical modifications that write back to native data are Phase 12 and Phase 37 work, under these rules.

### Chunks, residency and memory (T07)

Every cached chunk is recomputable from base, overrides, the clock and identity. Kinds of cached chunk:

- native cells;
- derived cells;
- coarse means;
- refinement terms.

These sit in one least-recently-used cache with a byte budget (64 MiB by default). Between calls an area is **unloaded**, **loaded** or **refined**. ADR-0007's **loading** happens synchronously inside the query that needs it, so no caller observes it. Results never depend on residency, budget or eviction. Tests compare:

- a starved cache, a generous one and repeated full unloads;
- writes interleaved with queries at every level, against unloading before every query.

The cache is never saved or checksummed.

A chunk is recognized as uniform without materializing it when its base is uniform and it has no writes. A derived field's chunk counts as uniform only when its eight neighbours also are and all share the same value. That keeps sparse fields cheap to aggregate. Coarse means and `Aggregate` visit native chunks one at a time. A computation therefore holds at most one native chunk beyond the cache (nine for a derived stencil), whatever the world's size.

### Aggregation (T09)

`Aggregate(field, rectangle)` returns the mean, minimum and maximum over a rectangle. Each native cell is weighted by the area of its overlap with the rectangle and the world, so mean × area is an exact total. Coarse-level values are exact means of native cells, weighted the same way. Conserve refinement keeps native means, so totals are preserved down to the tactical level.

### Invalidation (T11)

Writes go through the field's owner. A write invalidates cached chunks of that field, and of every field depending on it transitively, that intersect the changed area plus a halo. The halo is one native cell of the coarsest affected field, which covers finite differences and refinement terms. Distant cached chunks are reused. For example, painting scratch refreshes the nearby gradient and leaves a far gradient chunk cached.

### Change times

A sample's change time is the latest kept write among the native cells it depends on:

- for native samples, its interpolation inputs;
- for derived samples, also their finite-difference neighbours;
- for refined samples, also the 3 × 3 neighbourhood behind the refinement terms;
- for coarse samples, at the granularity of native chunks: possibly later than the exact time, never earlier.

Unwritten cells date from the world's start, when base data was generated. Change times derive from saved writes, so they are identical after a save and load.

### Continuous features (T06)

`MapFeature` holds a typed ID text, a kind (river, bank, shoreline, ridgeline, road, survey line), a polyline in world coordinates, a label and a creation time. Its spatial index is a derived cache over regional cells, built by walking each segment, so its size grows with length rather than bounding box. Queries return the nearest segment and its distance. Refinement (`Densify`) inserts points between vertices but never moves or drops one, so a feature keeps its identity and shape at every level. Features are saved and checksummed.

### Inspection and overlays (T13, T14)

The inspector shows every field of every family. For available fields it shows the value, level, classification, inputs, weights, basis, owner and change time. Unavailable fields show their owner and phase. Overlays sample `SampleArea` at the view's resolution. No visual value is computed outside the atlas.

### Migration (T15)

Save format 2 adds stored field overrides and features. The declared migration 1 → 2 adds empty sections. It is tested on a real format-1 save, which, migrated and continued, reaches the uninterrupted run's checksum.

A new field needs no migration while its overrides are absent: base regenerates. Changing a field's saved meaning bumps its schema version and needs a migration. Older saves are refused with the reason until one exists.

### Developer content in Phase 03

No physical field has a generator until Phase 04. To exercise and show the atlas end to end, Phase 03 adds:

- latitude and local solar time as analytic fields;
- a developer **scratch** field (stored, local level, dimensionless 0–1), painted by the journaled `PaintScratch` command;
- its gradient (a derived vector field);
- developer **survey lines** (`survey:N`).

They are tools, like markers: they are labeled as developer content and never feed physical processes.

### Measurement (T16)

`sovereigns-headless bench` measures query throughput per field and level, chunk churn under a small budget, whole-world aggregation and memory, and prints JSON. There are no thresholds until budgets are agreed on reference hardware (QUALITY_BAR). First measurements are in `tasks/evidence/P03.md`.

## Alternatives considered

- **One grid for all fields:** rejected by spec §3.4. Atmosphere, terrain and battlefield need different resolutions.
- **A quadtree with variable depth per area:** adaptive, but harder to make deterministic and to invalidate. Fixed levels with on-demand chunks give the same locality, with simple keys.
- **Storing every chunk:** simple, but memory grows with the world, and caches could drift from their sources. Overrides on regenerable bases keep saves small and caches honest.
- **Bicubic interpolation:** smoother, but it overshoots, which is unphysical for depths and fractions. Owners may add it per field if they need it.
- **Area-weighted, mass-preserving refinement for every field:** it steps at cell edges. Fields choose interpolate (continuous) or conserve (totals).

## Consequences

- Phase 04 registers elevation as a base field with a generator source. Its derivatives (slope, aspect, curvature) become derived fields with `DependsOn: elevation`, refreshed by the spatial invalidation above. Phase 04 also implements majority-class aggregation for its category fields.
- Every later physical subsystem registers its fields here, declares contracts consistent with the catalogue, and gets sampling, aggregation, invalidation, persistence, inspection and overlays without new machinery.
- Random global access at local resolution churns the cache: the whole 100 km world is 312 MB of local chunks against 64 MiB. Real access is localized. Pooling chunk arrays to reduce garbage-collection pressure is a measured candidate for Phase 51.
