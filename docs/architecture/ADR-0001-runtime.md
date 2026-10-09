# ADR-0001 — Game runtime and simulation separation

**Status:** Accepted design choice; exact dependency/runtime versions await reproducible Phase 01 verification.  
**Decision date:** 2026-10-08  
**Scope:** Runtime selection, source ownership and developer workflows.

## Decision

Use **Godot 4 .NET** as the 2D orthographic client and **independent C#/.NET** code for the authoritative simulation. Game-client code calls explicit simulation interfaces/commands and receives view snapshots; the simulation contains no Godot scene, Node, Vector2, or other Godot API dependencies.

Core responsibilities: world identity, coordinates, deterministic time and random streams, geometry, atmosphere, hydrology, ecology, people, economy, politics, armies, combat, state persistence. Client responsibilities: input, 2D renderer, camera, UI, map overlays, graphics and audio; it can interpolate visuals but cannot invent authoritative game state.

Use headless .NET tools for procedural-world batch generation, field inspectors, deterministic integration tests, performance measurement and replay. Godot CLI/headless tests verify client integration. Rendering performance should use batched drawing/instancing with actual measured budgets; not one expensive physics node per simulated soldier.

## Constraints

- Separate simulation and rendering clocks; only simulation time advances authoritative world state.
- Same world coordinates, road networks, elevations, weather, river state and lasting changes feed tactical and strategic modes.
- No requirement for a network server in the initial single-player game; do not add microservices without measured need.
- Pin exact Godot/.NET SDK and dependencies in Phase 01, with clean-build reproducibility on supported platforms.
- Native kernels may be considered only after profiling identifies a real hotspot and an ADR supports the integration cost.

## Rationale

Code-first deterministic tests and standalone numerical tooling are compatible with agentic workflows; Godot supplies mature 2D rendering/UI, so the team does not need to maintain an entire custom editor/asset stack. C# provides a coherent language and standard testing toolchain.

## Consequences and validation

Positive: simulation can be tested without launching editor, independent performance profiling, stable ownership, simpler agent task boundaries. Costs: explicit client-to-core bindings, serialization and snapshots, render synchronization. Acceptance: actual Godot client runs one authoritative .NET simulation and reads its state; headless runner reproduces matching checksums under an agreed seed; no Godot dependencies in core project references.
