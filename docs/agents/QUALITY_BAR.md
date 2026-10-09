# Production-quality Definition of Done

**Principle:** Each completed increment must be suitable to remain in the final architecture. Production quality is not equivalent to the entire game being finished. The strictness applies to *delivered functionality*, not to unimplemented future phases.

## Mandatory checks for every implementation task

1. **Correct authority:** the right subsystem owns state; no duplicate clocks, terrain, inventories, weather or sovereignty.
2. **Complete contracted behavior:** deliver the full agreed scope of the task, including ordinary and adversarial cases. Do not label an unfinished imitation "MVP," "prototype," or "temporary" and declare it done.
3. **Interfaces and data:** contracts are explicit, named, unit-consistent, validated, versioned where persistent, and compatible with downstream readers.
4. **Determinism and causality:** seeded tests are repeatable; clocks and random streams are controlled; derived fields come from their actual inputs.
5. **Verification:** unit/property tests for numerical logic, scenario/integration tests for consumer behavior, plus required manual inspection where applicable. Assertions must test behavior, not mere initialization.
6. **Persistence/recovery:** changed state survives save/load and deterministic replay; failures and incompatible schemas have defined behavior where applicable.
7. **Performance:** benchmark memory, CPU and frame/turn cost where the task introduces significant work; agree budgets using representative hardware and scale, record actual data; no speculative performance claims.
8. **Observability:** diagnostics can explain authoritative inputs, causality, edge conditions and failures; no silent corruption or untraceable fallback.
9. **Maintainability:** no duplicated physical formulas across subsystems, unexplained constants, disabled failing tests, generated build artifacts in source control, or undocumented architecture drift.
10. **End-to-end impact:** verify upstream owner and downstream consumer integration; visual plausibility alone is not evidence of environmental or gameplay correctness.
11. **Security and operations:** use pinned reproducible dependencies where feasible, validate untrusted data, safe file handling, no secrets in source, CI checks required.
12. **Review:** reviewer confirms acceptance evidence and phase gate. Agent implementation alone cannot self-certify VERIFIED.

## No-shortcut rule, with necessary nuance

- A simple algorithm may be fully production-quality **if** it meets the specified physical/modeling behavior with verified error limits and future growth paths.
- A limited scenario may be tested early, but its core path must not be deliberately thrown away. Production modules can serve a small initial dataset without being toy implementations.
- Stubs are allowed only to describe **unimplemented** external contracts and must visibly fail/decline unavailable operations; they cannot emit fabricated success or be counted as implemented features.
- Sophisticated features may be staged by task boundaries, never by secretly shrinking an individual task's stated acceptance scope.
- No unbounded "perfect before next task" requirement: use explicit gates. Revisit an earlier module when later integrated tests expose real issues, not to endlessly tweak speculative details.

## Required completion evidence

Attach task ID, commit/PR, authored documents read, upstream/downstream owners, commands executed + outputs, deterministic seed/scenario, saved-state verification, profile results if applicable, defects discovered, decisions/ADRs, and reviewer acceptance. Use [`HANDOFF_TEMPLATE.md`](HANDOFF_TEMPLATE.md).

## Phase gate

All tasks reviewed and checked; representative integrated demo working; required regression suite green; documentation and design references consistent; reference fixture reproducible; no unresolved blocking defect; and evidence linking observed behavior to the requirements. A production-ready phase does not mean the product is feature-complete or shippable.
