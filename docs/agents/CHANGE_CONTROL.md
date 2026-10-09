# Change control and escalation

- **Gameplay change:** edit relevant normative chapter through explicit issue/decision, reconcile other chapters, downstream tests and affected task requirements. Implementation cannot silently rewrite behavior.
- **Engineering change:** propose an architecture decision record (ADR) with context, decision, alternatives, quality/performance implications, migrations and rollback. Update the system map and owned interfaces.
- **Unexpected dependency:** link the blocking task; move prerequisite earlier through a reviewed task dependency change without renumbering IDs.
- **Spec conflict:** do not choose whichever wording is easier. Mark BLOCKED and produce competing clauses, affected systems, suggested reconciliation and reproducible acceptance case.
- **Infeasible target:** measure and document why; offer an equivalent architecture preserving invariant behavior. Never falsify environmental state or bypass a consumer to meet speed.
- **Regression:** reopen affected verified task/fixture, identify upstream ownership, fix and rerun evidence; avoid only patching a UI symptom.
- **Visual changes:** not authorized by gameplay module. Final art direction lives in separate visual spec when approved; 2D orthographic is the only immutable presentation constraint here.
- **Parallel PRs:** source boundaries, interface reviews, tests and traceable merges are mandatory; enable required reviews/branch protection in the actual Git provider during Phase 00.
