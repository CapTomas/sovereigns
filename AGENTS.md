# Sovereigns agent entrypoint

Read this first; then use task-specific routing. Avoid broad document scans.

1. **Find one task:** `python3 tools/task_context.py SOV-Pxx-Tyy --list`. Read the phase's target task and the routed normative spec chapters. Additional upstream/downstream chapters become mandatory if your changes affect them. Never assume the routed list is exhaustive after scope changes.
2. **Authority:** `docs/spec/` governs game behavior; `docs/architecture/` governs reviewed implementation decisions; `tasks/phases/` governs delivery and verified task status. Briefings and machine indexes are navigational, not new gameplay authority.
3. **Architecture:** Godot 4 .NET is the client; independent C#/.NET simulation owns physics, campaign, economy, warfare, world state, time and save semantics. No simulation dependency on Godot types or scene tree. Explicit commands and versioned data contracts cross the boundary.
4. **Production-quality increments only:** no mocked implementation masquerading as delivered functionality, magic numbers without provenance, duplicate authoritative state, commented-out failing tests, deliberately ignored save/load, or unmeasured high-risk performance. Build the correct foundation rather than disposable prototypes. Read `docs/agents/QUALITY_BAR.md`.
5. **Before coding:** identify authoritative inputs/outputs, owning subsystem, units/coordinate convention, time cadence, observability, determinism, failure modes, persistence, performance risk and acceptance criteria. If a fundamental decision is missing, record and resolve it via ADR/design process, not an undocumented guess.
6. **After coding:** run targeted and existing regression tests, check the reproducible scenarios, review invariants and commit evidence. Do not mark checkboxes verified yourself without accepted review evidence.
7. **Controlled changes:** edit only task-scope areas; resolve interface changes with consuming subsystems, update spec/tests/ADRs when needed. Never silently reduce requirements to pass a phase gate.
8. **Current scaffold:** Phase 00 documents/tools are real; simulation source and Godot project are not implemented yet. Do not assert game or integration tests passed until they actually exist and ran.

To list all numbered gameplay modules: `docs/spec/README.md`. For agent work instructions: `docs/agents/WORKFLOW.md`.
