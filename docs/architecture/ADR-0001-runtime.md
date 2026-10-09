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

## Alternatives considered

Evaluated against the decision criteria (large spatial data, profiling, headless testability, iteration speed, 2D orthographic rendering):

- **Unity (C#):** same language benefits and strong profiling, but a closed-source engine, vendor licensing-terms risk and a heavier editor-centric workflow; simulation separation is equally achievable in Godot.
- **MonoGame/FNA (C#):** full control and a thin runtime, but UI, input mapping, tooling and asset pipeline would have to be built and maintained in-house.
- **Bevy (Rust) or a custom C++ engine:** high performance ceiling, but slower iteration, a smaller 2D UI ecosystem and a much larger engine-maintenance burden before any gameplay proof.
- **Godot with GDScript simulation:** fastest scripting iteration, but weaker static typing, numerics and headless test tooling for a large deterministic simulation; it would also couple simulation to the engine.

**Revisit trigger:** supersede this ADR if Phase 01–03 evidence shows the stack cannot meet the requirements. That evidence is headless simulation tests without Godot, matching seeded checksums, and a representative field-query benchmark against a measured budget.

The local development machine had .NET SDK 10.0.401 and Godot 4.7.2 stable (mono) installed on 2026-10-09. They are candidates for, not substitutes for, the exact pins and clean-build evidence required in Phase 01.

## Consequences and validation

Positive: simulation can be tested without launching editor, independent performance profiling, stable ownership, simpler agent task boundaries. Costs: explicit client-to-core bindings, serialization and snapshots, render synchronization. Acceptance: actual Godot client runs one authoritative .NET simulation and reads its state; headless runner reproduces matching checksums under an agreed seed; no Godot dependencies in core project references.
