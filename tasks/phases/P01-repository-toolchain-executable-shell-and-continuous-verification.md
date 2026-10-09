## Phase 01 — Repository, toolchain, executable shell and continuous verification
**Depends on:** 00.  
**Outcome:** A clean build launches a real application and runs repeatable tests on every change.  
**GDB:** §§26.13, 29, 33.

- [x] **SOV-P01-T01** — Initialize game-client, simulation-core, tooling, data-content and test areas with clear responsibilities.
- [x] **SOV-P01-T02** — Establish reproducible dependency versions, development environments and bootstrap instructions.
- [x] **SOV-P01-T03** — Produce a minimal native desktop executable that opens, runs an update loop and closes cleanly.
- [x] **SOV-P01-T04** — Establish a renderer-independent simulation test runner usable without launching the game client.
- [x] **SOV-P01-T05** — Wire a structured logger with severity, subsystem, world seed, simulation time and object IDs.
- [x] **SOV-P01-T06** — Define debug/release build configurations and enforce warnings/errors appropriate to each.
- [x] **SOV-P01-T07** — Configure automated build, unit tests, lint/static checks and packaging smoke checks on each mainline change.
- [x] **SOV-P01-T08** — Add deterministic fixture invocation by seed and scenario name for local development and CI.
- [x] **SOV-P01-T09** — Add a basic failure report that captures relevant seed, time, input history and log context.
- [x] **SOV-P01-T10** — Provide a reusable 2D orthographic inspection shell with pan/zoom, layer selection and a location inspector using nonfinal debug graphics; clearly mark fields unavailable until their owning subsystem is implemented.
- [x] **SOV-P01-T11** — Provide a debug HUD for real seed, sim time, active world chunk, performance and warnings; document one launch command, seed/scenario selection and basic inspection steps.
- [x] **SOV-P01-T12** — Implement input-action abstraction, rebindable key mapping foundation and deterministic command capture.
- [x] **SOV-P01-T13** — Establish content schema validation and actionable errors for unknown or malformed data references.
- [x] **SOV-P01-T14** — Put license/attribution tracking in place for dependencies, future datasets and audiovisual assets.
- [x] **SOV-P01-T15** — Establish memory, crash and leak detection in development builds.
- [x] **SOV-P01-T16** — Run the full pipeline on a clean checkout and document successful recovery from a deliberately failed check.

**Exit gate 01:** A fresh clone builds, tests, launches and logs a seeded empty world in the reusable inspection shell; unavailable physical fields are explicit, and CI rejects a reproducible failure.
