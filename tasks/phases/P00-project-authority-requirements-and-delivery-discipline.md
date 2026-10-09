## Phase 00 — Project authority, requirements and delivery discipline
**Depends on:** The Game Design Bible.  
**Outcome:** Every agent starts with identical rules and a measurable definition of completion.  
**GDB:** §§0, 1, 27, 32–38.

- [x] **SOV-P00-T01** — Register the Game Design Bible as canonical for all gameplay behavior and physical simulation; link its version and location in project onboarding.
- [x] **SOV-P00-T02** — Create this roadmap as the canonical task hierarchy with stable IDs, statuses, verification records and issue/commit linking rules.
- [x] **SOV-P00-T03** — Create an engineering architecture decision record (ADR) index; separate implementation choices from game-design rules.
- [x] **SOV-P00-T04** — Create a change-proposal workflow for conflicts with the GDB; require upstream/downstream and saved-game impact analysis.
- [x] **SOV-P00-T05** — Define an agent task template covering interfaces, simulation inputs, outputs, tests, profiling, documentation and acceptance evidence.
- [x] **SOV-P00-T06** — Define the meaning of Done, Blocked, Verified and regression, including when to reopen previously completed tasks.
- [x] **SOV-P00-T07** — Decide supported initial desktop operating systems, input methods and distribution constraints as an engineering/release ADR.
- [x] **SOV-P00-T08** — Research and choose the 2D client runtime and simulation-core technology based on spatial data, profiling, testability and iteration speed.
- [x] **SOV-P00-T09** — Document the authoritative ownership boundary between simulation core, client renderer, tools, generated content and saved-game formats.
- [x] **SOV-P00-T10** — Choose a stable world unit system, coordinate conventions, precision rules, time and asset identifiers; ensure all agents reference it.
- [x] **SOV-P00-T11** — Define the test pyramid: deterministic unit/property tests, scenario tests, long runs, UI behavior tests and manual review.
- [x] **SOV-P00-T12** — Define a benchmark and telemetry policy (frame time, memory, simulation step, turn time, worldgen, save/load, battle complexity).
- [x] **SOV-P00-T13** — Define project build hygiene: no silent failed tests, no unreviewed changes to canonical schemas, clear diagnostic errors.
- [x] **SOV-P00-T14** — Create blank but versioned fixtures `F-VALLEY`, `F-RAIN-SNOW`, `F-TRADE`, `F-BORDER`, `F-BATTLE`, `F-MANY-SEEDS`, `F-LONG-RUN`.
- [x] **SOV-P00-T15** — Create a separate visual-direction document shell without deciding visual style; record the 2D orthographic constraint only.
- [x] **SOV-P00-T16** — Write the first delivery gate checklist and demonstrate a sample task flowing from request to verified evidence.

- [x] **SOV-P00-T17** — Create a modular authoritative spec index: each gameplay domain has a focused, stable document path and no competing source of truth.
- [x] **SOV-P00-T18** — Split the master roadmap into independently addressable phases; preserve all pre-existing task IDs and completion gates.
- [x] **SOV-P00-T19** — Define a context router mapping work items to the minimum mandatory relevant specifications; default to document discovery before broad reads.
- [x] **SOV-P00-T20** — Establish root and directory-scoped AGENTS.md rules for authority, boundaries, workflow, changes, tests and escalation.
- [x] **SOV-P00-T21** — Document the independent C# simulation core versus Godot 4 .NET client boundary, with permitted dependencies and test strategies.
- [x] **SOV-P00-T22** — Create a production-quality Definition of Done and a reusable task evidence/handoff record, including integration and persistence obligations.
- [x] **SOV-P00-T23** — Implement repository-document validation enforcing spec coverage, task uniqueness, links, routing integrity and stable identifiers.
- [x] **SOV-P00-T24** — Add a CI workflow to enforce the repository checks and block invalid documentation/task changes.
- [x] **SOV-P00-T25** — Establish an ADR/change-control policy that keeps implementation decisions separate from gameplay authority and requires impact analysis.
- [x] **SOV-P00-T26** — Define how agents handle missing specifications, conflicting requirements, failures and blocked work without inventing shortcuts.
- [x] **SOV-P00-T27** — Create an explicit dependency matrix identifying authoritative owners and consumers of simulation fields and cross-system interfaces.
- [x] **SOV-P00-T28** — Document how phase exit gates and completed work are reverified after changes to upstream subsystems.
- [x] **SOV-P00-T29** — Establish protected-branch, review and task-scope rules for parallel agents to avoid cross-feature collisions.
- [x] **SOV-P00-T30** — Verify a clean checkout can locate any task, load its scoped context and pass documentation checks offline without third-party dependencies.

**Exit gate 00:** A new agent can locate one task by ID, retrieve the smallest authoritative context sufficient to begin, identify owners and consumers, explain the production-ready Definition of Done, validate documentation/links/task IDs offline, and submit a reviewable evidence-bearing change. A live repository must enforce branch protections, CI, ownership and review requirements before Phase 00 is VERIFIED. Do not mark tasks complete merely because generated files exist.
