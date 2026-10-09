# Agentic delivery workflow

## Single focused work item

1. Locate task `SOV-PNN-TNN` in `tasks/phases/` and run `python3 tools/task_context.py ID --list`.
2. Read `AGENTS.md`, `QUALITY_BAR.md`, the active phase gate and routed chapters. Read exact subsections before broad chapters where sufficient. The route is a **starting context**, not a license to ignore newly affected systems.
3. Produce a short implementation plan with owners/consumers, technical boundary, acceptance tests, estimated performance risks and save/load/replay implications.
4. Ensure prerequisites and necessary decisions exist; if they do not, return BLOCKED with a precise missing prerequisite and proposal.
5. Implement the smallest complete production-quality increment that satisfies the task. Do not preemptively implement unrelated task IDs.
6. Add and run tests at the right layers; check integration and explicit false/edge cases. Re-run affected previously verified fixtures.
7. Prepare a task handoff and evidence with commands, results and measured impacts.
8. Reviewer inspects behavior, tests and contract compatibility. Only after acceptance mark task VERIFIED in its phase file; preserve issue/PR evidence.

## Agent context policy

- Context budget by relevance, not arbitrary omission. Read the tiny root `AGENTS.md`, local task, applicable chapters, and scoped module policies.
- Discover other chapters by `docs/spec/README.md` when adding data dependencies/consumers.
- Do not load all 39 specifications or all 55 phases for a normal task.
- If task spans multiple domains (e.g. runoff affecting tactical movement), inspect each authoritative domain regardless of token cost.
- Never paste entire documents into persistent task notes. Cite paths and section IDs; store concise decisions and tests.
- When agent memory is reset, recover from task ID, design chapters, ADRs, source/tests, and last accepted handoff; do not rely on an agent's private chat state.

## Parallel work

Start with one integration task at a time. Parallel work requires non-overlapping ownership or reviewed contracts, separate branches/worktrees, explicit reviewers, and integration tests. Never let two agents independently modify the same authoritative field or save schema without coordination.

## Status vocabulary

`Not started → In progress → Ready for review → Verified`; alternatively `Blocked` or `Superseded` with recorded reason. Checkbox means VERIFIED only. CI success is required, but not by itself sufficient.

## Tooling

- `python3 tools/task_context.py SOV-P00-T21 --list`: list relevant documents, no massive dump.
- `python3 tools/task_context.py SOV-P00-T21 --bundle`: print a self-contained bundle when an isolated worker truly needs it.
- `python3 tools/task_context.py SOV-P00-T21 --task`: show only task + phase gate.
- `python3 tools/validate_repo.py`: verify routing/ID/content structure with standard-library Python.
- `python3 tools/update_manifests.py`: refresh reviewed chapter hashes and existing task-title metadata after authoring changes. New tasks require a manually chosen context route.
