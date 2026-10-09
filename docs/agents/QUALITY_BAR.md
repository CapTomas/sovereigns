# Quality and proportional verification

Deliver the smallest complete solution that can remain in the intended architecture. Production quality means correct contracted behavior, maintainability and evidence appropriate to risk; it does not mean implementing future phases or pursuing perfection indefinitely.

## Engineering standard

- Apply KISS, YAGNI and SOLID with judgment: cohesive modules, clear ownership, direct flow, abstractions for real boundaries and no speculative frameworks.
- Keep one authoritative owner for state, clocks and formulas. Honor explicit contracts, units and downstream behavior. Validate external inputs and define meaningful failure behavior.
- Implement real agreed behavior. Visible stubs may decline unimplemented operations; they may not fabricate success or masquerade as delivered features.
- Preserve determinism, causality and save/replay compatibility when those contracts are affected. Name constants and their provenance; avoid unexplained numerical tuning.
- Keep dependencies justified and reproducible, secrets out of source, and diagnostics sufficient to explain material failures.
- Do not weaken tests, ignore failures, silently shrink acceptance or drift from gameplay authority to finish faster.

## Choose checks by impact

Choose the smallest set that can reveal the plausible defects introduced by the change. Explicit acceptance criteria and release/phase gates take precedence over this default table.

| Change | Normally sufficient | Broaden when |
|---|---|---|
| Prose, agent instructions, navigation links | Inspect consistency; relevant docs/link validator | Routing, CLI or configuration behavior changed |
| Small reversible formatting/configuration edit | Parse/validate or smoke-check the affected path | Client/version semantics, permissions or runtime behavior changed |
| Local code behavior or bug fix | Existing focused tests; add a regression if it protects meaningful behavior | Other modules consume the changed behavior |
| Public contract or cross-system behavior | Producer/consumer integration and affected regression tests | Shared assumptions or many callers changed |
| Numeric simulation, scheduling or conserved state | Deterministic representative cases, boundaries/tolerances and relevant invariants | Time skips, spatial edges or downstream causality changed |
| Durable state, migrations or replay | Save/load, compatibility and replay cases affected | Schema or recovery policy changed |
| Hot path, large-world scaling or memory ownership | Representative benchmark/profile against an actual budget | Measured cost or algorithmic complexity suggests broader impact |
| Security-sensitive or destructive behavior | Targeted adversarial/failure cases and independent review | Inputs, trust boundaries or persistent data risk widened |

Broad regression suites belong at affected integration boundaries and explicit acceptance gates. A doc edit does not require game seeds, save/load or benchmarks. A save-schema change does. Record a skipped material check and its reason; omit irrelevant checklist fields.

## Avoid verification overhead

1. Reuse existing tests and fixtures. Add tests for observable behavior, a real regression or an important invariant; do not mirror implementation line by line.
2. Use the appropriate layer. A unit test is enough for isolated logic; an integration test earns its cost when it checks a real boundary. Do not add both for the same assertion without a distinct failure mode.
3. Batch checks after coherent edits. Prefer one owner for shared checks. Rerun only after relevant changes, failures or new evidence; do not rerun an unchanged passing suite for reassurance.
4. Do not write tests of tests, inflate snapshot/coverage counts or benchmark unaffected code. Change a test helper's checks only when its own nontrivial behavior has changed.
5. No fixed coding/testing percentage or compulsory test quota. If validation dominates the work, reassess duplication and test scope; retain checks justified by actual risk.
6. Stop when acceptance is met, required checks pass and material review findings are resolved. More rounds need a concrete unresolved concern.

## Human-visible feedback during development

Each meaningful generated-world or simulation behavior increment must have a usable inspection path in the development build before its visual acceptance can be considered satisfied. Automated tests alone do not establish that the result is understandable, plausible or useful. Build the reusable viewer shell early, then expose each subsystem as it is implemented; do not wait for the environmental laboratory, final campaign UI or art polish.

- Use one development viewer consuming actual simulation state. Add the smallest useful layer, location/entity inspector, table, chart or event trace to it; do not create a disposable demo or duplicate simulator for every task.
- World generation must be viewable by seed and parameters. Spatial results need pan/zoom and appropriate scalar/vector/geometry overlays. A selected location exposes actual values and units, relevant update time and provenance; unavailable fields are visibly unavailable, never fabricated.
- Dynamic systems use the authoritative clock's pause, step and time-advance controls as they become available. Developer commands travel through the simulation boundary; inspector/render code cannot maintain a separate physical truth. Comparing a repeatable scenario before/after a change should be straightforward.
- Nonspatial systems also need inspection: inventories, prices, population, attitudes/relationships, AI decisions and state transitions can use concise tables, timelines and traces. A new calculation need not get its own screen when the existing inspector already exposes its effect.
- Developer inspection may reveal complete diagnostic state. Normal player views still obey faction knowledge and uncertainty rules; debug access must not silently change gameplay visibility.
- Each applicable handoff includes the exact launch command, seed/configuration or fixture, location/entity/time, a few inspection steps and expected observations. Report what was actually observed; attach a capture when it materially helps review. A static screenshot cannot replace a runnable inspection path.
- If the build/view cannot be run in the agent's environment, state that limitation and leave visual acceptance unverified. Mock data, generated art and passing numerical tests cannot stand in for an executed simulation view.

Internal refactors with unchanged behavior reuse the existing inspection path. Repository maintenance needs no game display. This rule adds early visibility and a short reproducible demonstration, not a second broad test suite or production-art requirement.

## Evidence and acceptance

Report what changed, why, commands run/results, and material limitations. A task with affected numerical, persistence or performance contracts also needs the corresponding seed/scenario, compatibility or measured budget evidence. Use the [handoff](HANDOFF_TEMPLATE.md) only to the depth needed for review and recovery.

Independent review is warranted for consequential behavior/contract changes and is required for tracked VERIFIED status. Routine low-risk maintenance can finish with the orchestrator's inspection. Do not invent review, remote CI, game execution or performance results.

A phase is accepted only after its explicit tasks and exit criteria are reviewed, relevant regressions pass, integrated evidence is reproducible and blocking defects are resolved. Phase acceptance is separate from a small implementation's local checks. The present repo can validate documentation and Python tools; it cannot yet demonstrate unimplemented simulation or Godot behavior.
