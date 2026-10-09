# Change control and escalation

Keep decisions as small as the change permits. A routine fix, refactor, documentation edit or repository improvement needs a clear diff and appropriate validation; it does not need a new ADR or invented gameplay task ID.

- **Gameplay rules:** `docs/spec/` governs behavior. A requested rule change must update the relevant chapter, affected contracts and acceptance criteria. An implementation cannot silently rewrite a requirement to make its tests pass.
- **Architecture:** write or update an ADR for a consequential decision: authoritative ownership, persistent formats, public subsystem boundaries, runtime/toolchain choices or a tradeoff expensive to reverse. Record context, decision, meaningful alternatives and relevant compatibility/migration consequences. Local implementation choices belong in code or the ordinary change summary.
- **Dependencies:** resolve prerequisites within the authorized scope when practical. Preserve stable task IDs when changing delivery order. If work requires unavailable input or an unresolved fundamental decision, continue independent work and report the exact blocker, evidence and proposed resolution.
- **Conflicts:** identify competing authoritative clauses and propose a focused reconciliation. Stop only the work that depends on that decision; do not invent gameplay semantics.
- **Performance targets:** measure material risks and explain infeasible budgets. Do not fabricate measurements, bypass a consumer or change authoritative state to manufacture a passing result.
- **Regressions:** fix the owning behavior, rerun the affected checks, and record any impact on previously accepted task evidence.
- **Visual rules:** use the applicable visual authority and gameplay constraints when presentation changes visibility, selection or spatial interpretation. Routine asset edits do not require unrelated architecture paperwork.
- **Parallel work:** assign explicit file ownership and coordinate shared contracts before edits. Integrate each result once; run checks justified by the combined change. Separate worktrees are useful for independent branches, not a prerequisite for every read-only or disjoint worker.

Tracked gameplay tasks become **Verified** only after the required review accepts their acceptance evidence. Repository housekeeping can finish with its own review and validation summary. Actual branch protections and required host checks remain Phase 00 enforcement work; writing these instructions does not configure them.
