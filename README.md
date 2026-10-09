# Sovereigns

**Procedurally generated, physically interconnected medieval grand strategy with persistent, real-time regiment battles.**

Phase 01 foundation: game specifications, delivery tasks, a .NET simulation kernel running a seeded empty world, a headless runner, a Godot development inspection shell, and CI. Physical fields, world generation and gameplay start in Phase 02 and later phases.

## Build and run

Install .NET SDK 10.0.401, Godot 4.7.2 (.NET build) and Python 3.11+ ([setup](docs/development/SETUP.md)), then:

```sh
python3 tools/doctor.py                                               # check the machine against the pins
dotnet test Sovereigns.slnx                                           # simulation tests, no Godot needed
dotnet run --project tools/Sovereigns.Headless -- run --fixture F-EMPTY   # headless seeded run, prints the state checksum
godot --path game -- --fixture F-EMPTY                                # inspection shell on the same world
```

Inspection steps are in [game/README.md](game/README.md); logs, failure reports and replay are in [DIAGNOSTICS](docs/development/DIAGNOSTICS.md).

## Start working

Read [AGENTS.md](AGENTS.md), the shared engineering policy for Codex and Claude Code. It covers KISS, YAGNI, pragmatic SOLID, scoped context, efficient delegation and proportional verification. Read deeper guides only when needed.

```sh
# Short navigation guide; no design-document dump
python3 tools/task_context.py

# Find a task without knowing its ID
python3 tools/task_context.py --search "simulation boundary"

# Read one task, then find its authoritative context
python3 tools/task_context.py SOV-P00-T21 --task
python3 tools/task_context.py SOV-P00-T21 --list --path src

# Find local instructions for an unnumbered repository change
python3 tools/task_context.py --path tools
```

Choose the user's requested scope. Gameplay delivery follows relevant dependencies in [the backlog](tasks/README.md); repository maintenance does not require beginning a game phase. Preserve stable task IDs and reviewed checkbox status.

## Agent setup

- **Codex:** [project adapter](.codex/README.md) with model-selectable scout, implementer and reviewer roles.
- **Claude Code:** [shared import](CLAUDE.md) and [project agents](.claude/README.md) with Haiku/Sonnet defaults and invocation overrides.
- **Model decisions:** [routing policy](docs/agents/MODEL_ROUTING.md). The user's front model orchestrates; workers use the least expensive available tier suitable for their task, with escalation for difficult decisions.
- **Working protocol:** [agent guide map](docs/agents/README.md). One writer per file/contract, bounded handoffs, no mandatory agent pipeline for small work.

Start a new client session after adding adapters. Model availability and project trust depend on the client/account. See the routing guide for the dated model reference and client requirements. Repo configuration does not install models or create a cross-provider bridge.

## Check the change

Use Python 3.11+; Python tooling has no external dependencies. Select checks by [impact and acceptance](docs/agents/QUALITY_BAR.md). The full CI-equivalent commands are in [SETUP](docs/development/SETUP.md#run-the-ci-checks-locally).

```sh
# Documentation, hashes, IDs and routing
python3 tools/validate_repo.py

# Context/tool behavior changes
python3 -m unittest discover -s tools/tests -v

# Simulation, headless runner and client code (CI=true makes warnings errors)
dotnet format Sovereigns.slnx --verify-no-changes
dotnet test Sovereigns.slnx
godot --headless --path game res://tests/ClientTests.tscn
```

Reuse meaningful checks, run them after coherent changes, and stop once acceptance and material review concerns are resolved. Save/replay, integration and profiling checks apply when their behavior is affected or an explicit delivery gate requires them.

World/simulation increments also need continuous human-visible feedback: the reusable development viewer, real data layers/inspectors, time controls when implemented, and short launch/seed/scenario instructions. You should be able to inspect generated terrain, added elevation, rivers, weather and later nonspatial data as each arrives. The Phase 11 laboratory extends this viewer; it is not the first opportunity to see the simulation. See [the human-feedback policy](docs/agents/QUALITY_BAR.md#human-visible-feedback-during-development). The viewer is the Phase 01 inspection shell in `game/`; each subsystem adds its layers and inspector values to it.

## Structure and authority

| Location | Responsibility |
|---|---|
| `docs/spec/` | Authoritative game mechanics (Game Design Bible 1.1.0), 39 stable numbered chapters |
| `docs/architecture/` | Accepted implementation decisions and state ownership |
| `tasks/phases/` | 55 delivery phases; source of reviewed checkbox status |
| `meta/` | Generated task/spec routing metadata |
| `docs/agents/` | Current engineering workflow, model routing and quality policy |
| `.codex/`, `.claude/` | Client adapters and focused worker definitions |
| `src/` | Independent C# simulation (`Sovereigns.Simulation`) |
| `game/` | Godot 4 .NET client and development inspection shell |
| `content/` | Versioned content definitions (`namespace:kind/name`) |
| `tests/` | Simulation tests and versioned reference fixtures |
| `tools/` | Headless runner (`Sovereigns.Headless`), context/validation CLI, bootstrap doctor, CI helpers |
| `docs/development/`, `docs/legal/` | Setup and diagnostics guides; third-party license registry |
| `docs/visual/` | Separate visual direction and art constraints |

Godot 4 .NET presents the world and accepts commands; independent C#/.NET owns simulation, time and saves, without Godot dependencies. Exact versions and layout are in [ADR-0005](docs/architecture/ADR-0005-toolchain-and-repository-layout.md). Preserve one authoritative world across strategic and tactical views.

This checkout is connected to the [CapTomas/sovereigns repository](https://github.com/CapTomas/sovereigns). The `Build and test` workflow runs the .NET, client and (on `main`) packaging checks. Offline validators alone do not establish game execution, remote CI or phase completion. Tracked tasks remain unchecked until their required review accepts the evidence.
