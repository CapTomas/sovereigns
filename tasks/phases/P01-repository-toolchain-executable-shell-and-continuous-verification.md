## Phase 01 — Repository, toolchain, executable shell and continuous verification
**Depends on:** 00.  
**Outcome:** A clean build launches a real application and runs repeatable tests on every change.  
**GDB:** §§29, 33.

- [ ] **SOV-P01-T01** — Initialize game-client, simulation-core, tooling, data-content and test areas with clear responsibilities.
- [ ] **SOV-P01-T02** — Establish reproducible dependency versions, development environments and bootstrap instructions.
- [ ] **SOV-P01-T03** — Produce a minimal native desktop executable that opens, runs an update loop and closes cleanly.
- [ ] **SOV-P01-T04** — Establish a renderer-independent simulation test runner usable without launching the game client.
- [ ] **SOV-P01-T05** — Wire a structured logger with severity, subsystem, world seed, simulation time and object IDs.
- [ ] **SOV-P01-T06** — Define debug/release build configurations and enforce warnings/errors appropriate to each.
- [ ] **SOV-P01-T07** — Configure automated build, unit tests, lint/static checks and packaging smoke checks on each mainline change.
- [ ] **SOV-P01-T08** — Add deterministic fixture invocation by seed and scenario name for local development and CI.
- [ ] **SOV-P01-T09** — Add a basic failure report that captures relevant seed, time, input history and log context.
- [ ] **SOV-P01-T10** — Provide a placeholder 2D orthographic viewport and camera pan/zoom using nonfinal debug graphics.
- [ ] **SOV-P01-T11** — Provide a debug HUD for seed, sim time, active world chunk, performance and warnings.
- [ ] **SOV-P01-T12** — Implement input-action abstraction, rebindable key mapping foundation and deterministic command capture.
- [ ] **SOV-P01-T13** — Establish content schema validation and actionable errors for unknown or malformed data references.
- [ ] **SOV-P01-T14** — Put license/attribution tracking in place for dependencies, future datasets and audiovisual assets.
- [ ] **SOV-P01-T15** — Establish memory, crash and leak detection in development builds.
- [ ] **SOV-P01-T16** — Run the full pipeline on a clean checkout and document successful recovery from a deliberately failed check.

**Exit gate 01:** A fresh clone builds, tests, launches and logs a seeded empty world; CI rejects a reproducible failure.
