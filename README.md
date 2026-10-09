# Sovereigns

**Procedurally generated, physically interconnected medieval grand strategy with persistent, real-time regiment battles.**

This repository foundation is organized for code-first AI-agent delivery. There is no requirement to read a 40,000-word bible or 1,000-task checklist to perform one focused job. Use the scoped routing instead.

## Begin here

1. Read [`AGENTS.md`](AGENTS.md) (small, universal rules).
2. Select a task from [`tasks/README.md`](tasks/README.md).
3. Run `python3 tools/task_context.py SOV-P00-T01 --list` to find exactly which design chapters and governance docs apply.
4. Read those files, declare affected authoritative owners and consumers, then implement the task.
5. Run `python3 tools/validate_repo.py` and any appropriate executable/code tests. Submit evidence according to [`docs/agents/QUALITY_BAR.md`](docs/agents/QUALITY_BAR.md).

## Technology decision

- **Client:** Godot 4 .NET, fully 2D orthographic; exact release pinned under Phase 01 after clean-environment verification.
- **Simulation:** independent C#/.NET library, no Godot dependencies, authoritative simulation, shared clock and persistence.
- **Verification:** headless .NET tools and tests, Godot headless/client checks, deterministic fixtures and performance budgets measured on representative machines.
- **Visual identity:** separate design authority; no art style is imposed by gameplay chapters.

## Structure

| Location | Responsibility |
|---|---|
| `docs/spec/` | Authoritative mechanical rules, one system per module |
| `tasks/phases/` | Ordered, locally scoped delivery checklist |
| `meta/` | Machine-readable task and specification routing indexes |
| `docs/agents/` | Agent working practices, review and quality contracts |
| `docs/architecture/` | Implementation decisions and authoritative ownership |
| `tools/` | Repository context and validation CLI |
| `src/` | Future independent simulation implementation |
| `game/` | Future Godot client implementation |
| `tests/` | Future automated simulations and integration verification |

Phase 00 creates and verifies the delivery system. **No feature is considered production-quality until its actual, completed scope has passing tests, persistence and interface guarantees, observability, and measured performance where relevant.** An incomplete product can consist of production-quality completed modules; a passing scaffold cannot pretend to be a completed game.

## Current status

This is a **Phase 00 repository foundation**, not a finished application. The documentation and routing tools can be verified offline. Godot and .NET builds are Phase 01 work, not claimed as completed here. Checkbox state must not be changed without review.
