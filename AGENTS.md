# Sovereigns: shared agent instructions

These rules apply to Codex and Claude Code. Follow the user's active scope; repository preparation, tooling and documentation work do not require starting a gameplay phase or inventing a task ID.

## Start with the smallest useful context

- Inspect the working tree before editing; preserve unrelated changes. Use `rg` and targeted reads.
- Unknown task: `python3 tools/task_context.py --search "keywords"`. Known task: `python3 tools/task_context.py SOV-Pxx-Tyy --task`, then `--list` for its routes. With no arguments the router shows a short navigation guide.
- Read scoped `AGENTS.md` files for the paths you touch, even if the client did not load them automatically. Use the router's `--path` option to find them.
- Read relevant spec sections and affected owners/consumers; expand context when scope changes. Do not load every chapter, phase or process archive.
- `docs/spec/` owns game behavior; `docs/architecture/` owns accepted implementation decisions; `tasks/phases/` owns delivery and reviewed checkbox status. `meta/` is navigation. Current working practices live in `docs/agents/`; historical process copies are reference material.

## Exercise senior engineering judgment

- **KISS:** choose the simplest complete solution with explicit ownership and readable contracts.
- **YAGNI:** implement the requested behavior. Avoid speculative features, frameworks, extension points and dependencies.
- **SOLID, pragmatically:** keep cohesive responsibilities, small consumer-facing interfaces, substitutable implementations and dependencies pointing toward the domain. Add abstractions at real boundaries; do not create an interface for every class.
- Reuse established logic; keep authoritative state in one place. Prefer composition and direct data flow over deep inheritance and hidden coupling.
- Before a substantial change, identify scope, acceptance, inputs/outputs, owners and material risks. Units, cadence, determinism, persistence and performance matter when the change affects them. A small fix needs no elaborate design packet.
- Record consequential architectural decisions; resolve routine implementation choices yourself. Follow [change control](docs/agents/CHANGE_CONTROL.md) for real contract/spec conflicts.

## Orchestrate deliberately

- The front model owns scope, decisions, delegation, integration and the final result. Delegate independent useful work proactively when it saves time or improves quality; do small tightly coupled work directly.
- Choose the least expensive available model likely to finish each assignment correctly. Use [model routing](docs/agents/MODEL_ROUTING.md) for model tiers, overrides and escalation; do not assume every provider/model is available.
- Give workers a bounded goal, owned paths, exact context, acceptance/check scope and expected output. Assign one writer per file or authoritative contract; read-only discovery can run in parallel.
- Keep the front model on integration and unresolved decisions while workers work. Reuse workers for follow-ups, escalate repeated failures, and collect results before reporting completion. Full protocol: [workflow](docs/agents/WORKFLOW.md).

## Verify proportionally, then stop

- Follow [quality and verification](docs/agents/QUALITY_BAR.md). Reuse existing checks and add tests only for meaningful changed behavior or regression risks.
- Documentation/small reversible edits normally need inspection or a relevant validator. Behavior changes need focused tests; broad regression, integration, persistence and performance checks are triggered by impact or explicit acceptance gates.
- Deliver human-visible feedback with meaningful world/simulation changes: extend the reusable development viewer with real layers, inspected values or tables, and provide a reproducible launch/seed/scenario plus short inspection steps. Do not postpone the first usable view until later UI/polish phases. See the human-feedback rule in `QUALITY_BAR.md`.
- Batch checks after coherent changes. Repeat only after relevant edits, failures or new evidence. No tests of tests, coverage quotas, blanket benchmark runs or fixed coding/testing ratios.
- Report the change, actual commands/results and material limitations concisely. Never fabricate evidence, weaken assertions to conceal defects or self-certify a tracked task as VERIFIED.

## Architecture and present state

Godot 4 .NET is the client. The independent C#/.NET simulation owns physics, campaign, economy, warfare, world state, time and saves. No Godot types or scene-tree dependency in simulation; explicit commands and versioned data contracts cross the boundary. Units, coordinates, precision, time and identifiers follow [ADR-0004](docs/architecture/ADR-0004-units-coordinates-time-identifiers.md); platform targets follow [ADR-0003](docs/architecture/ADR-0003-platforms-input-distribution.md). Tracked status and evidence follow the [Definition of Done](docs/agents/DEFINITION_OF_DONE.md).

Phase 01 provides the build foundation: a .NET simulation kernel with a seeded empty world, a headless runner, a Godot inspection shell and CI. Toolchain pins and layout are in [ADR-0005](docs/architecture/ADR-0005-toolchain-and-repository-layout.md), and commands are in [SETUP](docs/development/SETUP.md). Physical fields, world generation and gameplay are still future phases. Report only the builds, tests, launches and CI runs you actually executed. If this checkout has no `.git`, work in place and report that pull, commit and worktrees are unavailable.
