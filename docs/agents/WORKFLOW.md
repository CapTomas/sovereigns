# Efficient agent delivery

The front model is accountable for the completed user request. Workers reduce elapsed time and context load; spawning agents is a means, not a completion criterion.

## 1. Route and size the work

1. Inspect current files and changes. Determine the requested outcome and smallest complete scope.
2. For tracked delivery, find the task with `python3 tools/task_context.py --search "keywords"`, then use `ID --task` and `ID --list`. Read the target task, relevant acceptance requirements, spec sections and affected owners. Repository maintenance can proceed without a gameplay task ID.
3. Read scoped instructions for touched paths (`--path src`, for example). Expand the route for newly affected consumers. Use `--section` for exact spec sections; reserve `--bundle` for isolated workers that need all routed context.
4. For a small fix, proceed with a short stated intent. For several steps or material uncertainty, keep a compact plan: outcome, ownership, boundaries, acceptance and applicable risks. For changed simulation/world behavior, include where the developer can inspect its actual output and the smallest viewer addition needed. Use an ADR only when [change control](CHANGE_CONTROL.md) calls for one.
5. Choose validation using [QUALITY_BAR.md](QUALITY_BAR.md). Do not apply an entire phase exit gate to every edit. Explicit task and phase acceptance requirements still apply when claiming their completion.

## 2. Decide what to delegate

| Work shape | Approach |
|---|---|
| Small, clear edit in one area | Front model completes it directly |
| Search, dependency trace, source verification | Bounded read-only scout if its result removes uncertainty |
| Independent implementation areas | Workers with disjoint owned paths and agreed interfaces |
| Shared contract, schema or coupled refactor | Front model settles the boundary; one writer implements it |
| Significant behavior/risk or tracked acceptance | Focused independent review after integration |

Use [MODEL_ROUTING.md](MODEL_ROUTING.md) to select the worker model. Start only workers with a concrete useful result. Prefer one or two workers initially; expand when independent work and client capacity justify it. Do not parallelize a dependency chain or create a mandatory scout/implementer/reviewer pipeline for every task.

## 3. Give each worker a complete, bounded assignment

Use this small packet in the spawn prompt; do not create a separate file for a short assignment:

```text
Goal / acceptance:
Role and model / why this tier:
Owned paths (or read-only) / interfaces to preserve:
Context: exact paths, sections, known decisions:
Checks: required commands or behaviors; who runs shared checks:
Human inspection, when applicable: launch/fixture, view, short steps and expected observations:
Return: changes/findings with file references, commands/results, unresolved risks:
```

- Supply relevant excerpts or references instead of the full conversation when the client allows it. Include user constraints and decisions the worker cannot infer from files.
- Give one writer each file and authoritative contract. A shared checkout is sufficient for coordinated, disjoint edits. Use branches/worktrees when useful for isolation and supported by an actual Git checkout; they are not required for read-only workers.
- Workers read shared and applicable scoped instructions, implement the assigned scope, and flag evidence-backed conflicts. They do not expand the task or spawn more workers without an explicit useful reason and available capacity.
- Supply routed paths/excerpts and check evidence to workers without shell access. A read-only reviewer need not run the same tests again; the orchestrator supplies results or delegates a distinct reproduction.
- The orchestrator owns shared validators/regression runs unless it delegates them once. A worker runs its focused check; avoid every worker running the same suite.

## 4. Keep moving and integrate

- Work on decisions, contracts or another independent slice while workers run. Avoid doing the same implementation twice.
- Reuse existing workers for follow-ups. Review concrete outputs rather than repeatedly polling unchanged state.
- Resolve routine prerequisites autonomously within scope. If a dependency or decision genuinely needs the user, state the precise blocker and a proposed resolution; continue unaffected work.
- After a failed approach, diagnose once. Repeated failure, scope drift or unsupported assumptions trigger a narrower assignment or stronger model, not an endless retry/review loop.
- Integrate outputs, inspect the whole change for ownership/contract conflicts, and run the agreed checks on the resulting state. Investigate failures; do not hide them.
- For review, ask for actionable defects with evidence and severity. Fix material findings; rerun affected checks. Style preferences are not a reason for another review round.

## 5. Close with evidence

Small maintenance work needs a concise final explanation, actual check results and material limitations. For meaningful world/simulation changes, also provide launch instructions and a few steps to see the result and inspect its data; disclose whether that view was actually run. Longer work uses [HANDOFF_TEMPLATE.md](HANDOFF_TEMPLATE.md) for durable recovery; retain paths/section IDs, decisions and outstanding work, not pasted documents or chat transcripts.

Tracked status is `Not started → In progress → Ready for review → Verified`, with `Blocked` or `Superseded` carrying a reason. A phase checkbox means accepted verification. Implementers and generated worker summaries cannot self-certify it. Phase gates and required CI are evaluated when accepting tracked delivery, not invented for unrelated maintenance.

On context reset, recover from the user's scope, task/handoff, relevant source/spec sections and accepted ADRs. Do not rely on private agent memory. Do not create a second authoritative status tracker.

## Useful commands

See [tools/README.md](../../tools/README.md) for discovery and scoped context. Run `python3 tools/validate_repo.py` for documentation/routing changes; run `python3 -m unittest discover -s tools/tests -v` for context/tool behavior changes. Refresh manifests only when authoritative chapters or task metadata changed. Current tooling is standard-library Python and offline-capable; .NET/Godot checks will be added when their projects exist.
