# Definition of Done, status and verification records

This applies to tracked `SOV-PNN-TNN` work and phase gates. Repository maintenance without a task ID only needs the [quality bar](QUALITY_BAR.md) and a clear PR.

## Statuses

| Status | Meaning | Required record |
|---|---|---|
| Not started | No owner or branch | None |
| In progress | Owned; a branch exists | The branch name, PR title or PR body names the task ID |
| Ready for review | Done (below); checks run; PR open | Evidence entry with results |
| Verified | Evidence accepted (below) | Phase checkbox `[x]` plus evidence entry with the reviewer report |
| Blocked | Cannot proceed without an external prerequisite or decision | Reason, prerequisite and its owner, proposed next action, date |
| Superseded | Replaced by other work; the ID is kept forever | Link to the replacing task/PR and the reason |

The phase-file checkbox is the only authority for Verified. Other statuses live in the evidence file and PRs. `meta/tasks.json` status fields are not status.

## Done

A tracked task is Done, and can move to Ready for review, when all applicable items hold:

1. **Acceptance:** the task text, relevant spec sections and the phase gate clauses it touches are met. Scope has not shrunk silently, and any deviation is recorded and justified.
2. **Real integration:** the behavior works through its actual producers and consumers. Stubs only decline unimplemented operations visibly.
3. **Checks:** focused automated tests for the changed behavior ([quality bar](QUALITY_BAR.md)). All required CI checks pass, and no test was skipped, weakened or disabled without a recorded reason.
4. **Determinism:** world/simulation behavior reproduces from a recorded seed, fixture or scenario.
5. **Persistence:** added or changed authoritative state is covered by save/load and migration checks once a save format exists (SOV-P02-T17 onward).
6. **Performance:** measured against a stated budget when the task touches a hot path or has a performance clause. Otherwise no benchmark.
7. **Observability:** a human inspection path for changed world/simulation behavior, plus diagnostics that explain material failures.
8. **Documentation:** spec, ADR, system map, README and manifests are updated when contracts or decisions changed.
9. **Evidence:** recorded as described in [Records and linking](#records-and-linking).

## Verified

A task is Verified when:

1. it is Done;
2. an independent reviewer (the `repo-reviewer` role or a human, never the implementer) has reported on the evidence with no unresolved material finding;
3. required CI is green; and
4. the maintainer merges the PR that contains the evidence, the reviewer report and the checkbox change.

The merge is the acceptance. Agents never check boxes outside such a PR and never self-certify.

## Regressions and reopening

A **regression** is any previously Verified acceptance criterion that no longer holds on `main`: a failing required check, a broken invariant or observed wrong behavior, whatever the cause.

- Fix it at the owning module and add a regression check if nothing caught it.
- **Reopen** the task (uncheck it in a PR and record cause, link and date in its evidence entry) when the fix does not land in the same PR, when an upstream contract change invalidates the task's evidence, or when a review finds the evidence was wrong.
- Do not reopen for refactors that leave the evidence valid.

## Reverification after upstream changes

A PR that changes an authoritative contract must list the Verified tasks and phase gates whose evidence depends on it, and must rerun their checks or reopen them. Authoritative contracts include spec chapters, ADRs, public interfaces, field units or cadence, save schemas and fixture versions. Find dependents through task spec routes in `meta/tasks.json` and consumers in the [system map](../architecture/SYSTEM_MAP.md).

- Automated checks of Verified work keep running in CI on every change. This is the default reverification.
- Manual or visual evidence must be redone when its inputs change, for example a fixture version or an upstream field contract.
- A phase exit gate is re-evaluated when one of its tasks is reopened or when an earlier phase's gate evidence it relies on changes.

## Blocked work, missing specs and failures

- Missing or ambiguous gameplay rules: do not invent behavior. Continue on an explicitly marked assumption only where spec §0.2 permits it. Otherwise propose an amendment under [change control](CHANGE_CONTROL.md) and continue independent work.
- Failures: diagnose once, then narrow the task or escalate ([workflow](WORKFLOW.md) §4). Never hide a failure to reach Done.
- Record a block as listed in the status table and surface it to the maintainer.

## Records and linking

- **Branches:** `sov-pNN-tNN-short-slug` for one task. Use a descriptive name such as `p00-closeout` or `chore/...` for task groups and maintenance.
- **Commits and PR titles:** cite task IDs, for example `SOV-P01-T03: open and close the desktop shell`. A PR covering several tasks lists every ID in its body. Maintenance without an ID uses a plain prefix (`docs:`, `tools:`, `chore:`).
- **Issues** (optional, for bugs, blockers or discussion): title starts with the task ID when one applies. The resolving PR links the issue. Issues never carry Verified status.
- **Evidence:** `tasks/evidence/PNN.md`, one section per task or task group, using the [handoff fields](HANDOFF_TEMPLATE.md) that apply. Append the reviewer report (verdict, reviewer role/model, date, findings and their resolution). The checkbox and its evidence change in the same PR. `tools/validate_repo.py` rejects a checked task with no evidence entry.
- **Never** renumber, delete or reuse task IDs (`tasks/AGENTS.md`).

## Phase exit gate checklist

1. Every task in the phase is Verified or Superseded. None is Blocked.
2. The exit gate statement is evaluated clause by clause in an "Exit gate" section of `tasks/evidence/PNN.md`, with commands, results, links or captures.
3. Required CI is green on `main` at the gate commit, and a clean-clone run of the checks has passed.
4. From Phase 01 on, reference seeds, fixtures and the human inspection path are recorded and were actually run.
5. Earlier phase gates still hold; any affected gate has been reverified.
6. Independent review of the gate evidence is complete, and the maintainer's merge accepts it. Tag the accepted commit `phase-NN`.

## Parallel work and branch rules

- `main` accepts changes only through PRs with green required checks. No force pushes or deletion (see [repository enforcement](../process/REPOSITORY_ENFORCEMENT.md)).
- One task or coherent task group per branch and PR. One writer per file and per authoritative contract. Parallel writers use separate branches or worktrees.
- Shared contract changes land first, in their own PR, before dependent work merges.
- Branches must be up to date with `main` before merge. The branch owner resolves conflicts by integrating, never by discarding another branch's work.
