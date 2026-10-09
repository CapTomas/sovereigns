# 11 — 2D rendering, scalability and performance design

**Game runtime:** Godot 4 .NET client; simulation independent C#/.NET, per `docs/architecture/ADR-0001-runtime.md`. **Goal:** stable huge-world, huge-battle presentation on target desktop hardware without compromising true world state.

## Separation of ownership

The renderer receives world-state snapshots, field queries, entity state and permitted faction knowledge. It owns view caches, shader parameters, draw batching, camera, transient interpolation and visual settings. It does **not** own climate or water conservation, persistent foliage, tactical positions, units, time or political control. Shaders must never silently change gameplay ground conditions. Client random cosmetic variance must be seeded by stable entity/coordinate identifiers.

## Rendering strategy

**Terrain:** draw chunked data layers and mask/mesh/texture representations efficient for 2D. Shader relief can derive from height/slope and physical material fields. Avoid a standalone node for every terrain sample. Stream and cache in camera-appropriate regions; seam continuity, world coordinates and persistence remain invariant.

**Vegetation and buildings:** use instanced/batched sprites/geometry where possible, with stable placements from physical state and persistent IDs. Different LODs have distinct render presentations but the same world features. Dense occluders require contextual transparency near selected forces.

**Soldiers:** prefer batched instanced representation (such as Godot MultiMesh or appropriate custom draw pipeline after profiling) over a full scene/physics body/script per visual soldier. Regiment command and local combat remain authoritative in simulation, not per-sprite Godot nodes. Benchmark sprite batching, selection outlines, banners, projected shadows and rain together, not as isolated best-case tests.

**UI and overlays:** vector/screen-space primitives for boundaries and paths; chunk/texture overlays for spatial fields with legends and correct interpolation. Huge visible labels and paths require culling, prioritization and batching.

## Practical budgets, not unsupported promises

Choose reproducible hardware tiers and viewport targets in architecture/task ADRs before claiming budget compliance. Intended desktop play should target responsive camera/selection, stable tactical frame pacing and large-army views. **60 FPS at 1080p is an initial target for representative hardware, not a measured current achievement.** Large-battle visibility tests should include at least a scene with tens of thousands of drawn simplified soldier marks, but the specific supported maximum must be established by profiling and product review. Track CPU simulation time, render-frame p50/p95/p99, GPU draw cost, working-set RAM, upload/stream stalls, map generation time and save/load behavior. Optimize actual hotspots rather than speculating.

## Progressive fidelity without fake simulation

- Far world regions use coarse visual data and cached summaries but they still represent coherent authoritative physical state.
- Near battlefield data uses constrained refinement and consistent parent boundary conditions. No renderer-owned alternate rainfall/soil-water system.
- Particle density, microtexture density, soldier micro-animation and canopy detail can reduce with distance/performance mode. Mechanical consequences (e.g., wind effect, blocked ford, line of sight) do not disappear.
- A “low graphics” option can simplify ambient effects and micrographics while preserving essential hazard/visibility cues, silhouettes, color-pattern differentiation and selection feedback.
- Visual chunk loads must not block the simulation clock unpredictably or cause major world detail to visibly repopulate every camera pan.

## Determinism and visual stability

Verify identical scene/world/clock/knowledge snapshots produce stable geographic positions and layout across reload, zoom and window resizing. Shader micro-variation may be animated but must be bounded and must not change the apparent bank, crossing, structure or route. World visuals should avoid temporal flicker due to LOD thresholds, depth sorting or generated tree placement.

## Performance regression tests

A. Rapid pan along a long winding river during rainfall; B. zoom across three LOD thresholds in a forest town region; C. 20,000–30,000 depicted troops with selection overlays and moving formations; D. mass rout through forest; E. large snow/flood mask update; F. mixed night rain battle; G. multiple towns/castles and active administrative border overlays. Capture actual profiles on pinned Godot/runtime builds; maintain reference baselines and review justified changes. Frame rate alone is insufficient if command interaction stutters.

## Engineering caution

Do not preselect exotic custom rendering or native kernels solely because the game is ambitious. Choose an appropriate Godot 2D rendering path after representative benchmarks; document the choice and its fallbacks. The independence of authoritative C# simulation is non-negotiable.
