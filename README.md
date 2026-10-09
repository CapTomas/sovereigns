# Sovereigns

**Procedurally generated, physically interconnected medieval grand strategy with persistent, real-time regiment battles.**

This is a Phase 00 foundation: game specifications, delivery tasks and offline Python navigation tools. The independent C# simulation and Godot client are future implementation work.

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

Use Python 3.11+; tooling has no external dependencies. Select checks by [impact and acceptance](docs/agents/QUALITY_BAR.md):

```sh
# Documentation, hashes, IDs and routing
python3 tools/validate_repo.py

# Context/tool behavior changes
python3 -m unittest discover -s tools/tests -v
```

Reuse meaningful checks, run them after coherent changes, and stop once acceptance and material review concerns are resolved. Save/replay, integration and profiling checks apply when their behavior is affected or an explicit delivery gate requires them.

World/simulation increments also need continuous human-visible feedback: an early reusable development viewer, real data layers/inspectors, time controls when implemented, and short launch/seed/scenario instructions. You should be able to inspect generated terrain, added elevation, rivers, weather and later nonspatial data as each arrives. The Phase 11 laboratory extends this viewer; it is not the first opportunity to see the simulation. See [the human-feedback policy](docs/agents/QUALITY_BAR.md#human-visible-feedback-during-development). These are delivery requirements; no runnable viewer exists in the current scaffold yet.

## Structure and authority

| Location | Responsibility |
|---|---|
| `docs/spec/` | Authoritative game mechanics, 39 stable numbered chapters |
| `docs/architecture/` | Accepted implementation decisions and state ownership |
| `tasks/phases/` | 55 delivery phases; source of reviewed checkbox status |
| `meta/` | Generated task/spec routing metadata |
| `docs/agents/` | Current engineering workflow, model routing and quality policy |
| `.codex/`, `.claude/` | Client adapters and focused worker definitions |
| `tools/` | Offline context and validation CLI |
| `src/`, `game/`, `tests/` | Future simulation, Godot client and game verification |
| `docs/visual/` | Separate visual direction and art constraints |

Godot 4 .NET presents the world and accepts commands; independent C#/.NET owns simulation, time and saves, without Godot dependencies. Exact runtime releases are Phase 01 decisions. Preserve one authoritative world across strategic and tactical views.

This checkout is connected to the private [CapTomas/sovereigns repository](https://github.com/CapTomas/sovereigns). Offline validators do not establish game execution, remote CI or phase completion. Tracked tasks remain unchecked until their required review accepts the evidence.
