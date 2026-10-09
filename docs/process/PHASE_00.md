# Phase 00 execution contract — agent-operable repository

Phase 00 must finish before large subsystem implementation. Its output is a real, enforceable working environment, not a decorative folder tree.

## Required outcomes

- Clearly scoped normative modules with stable IDs and a navigable index; no agent needs the full old monolith for routine tasks.
- One-file-per-phase tracked work; one task can be found from its stable ID without searching a thousand-item checklist.
- Context routing and scoped `AGENTS.md` rules, plus mandatory escalation when new upstream/downstream dependencies appear.
- Approved Godot 4 .NET/C# simulation architecture; exact tooling versions pinned and verified during the executable Phase 01 setup.
- Production-quality Definition of Done, review rules and evidence template, including tests, persistence, profiling, observability and migrations as applicable.
- CI-enforced structural checks, source/PR protections and verified offline agent bootstrap.

## Foundation versus completion

The files in this repository provide much of the Phase 00 documentation and tooling. **All Phase 00 tasks remain unchecked** until reviewer evidence and actual repository policy checks exist (such as live required GitHub checks/branch rules). Do not infer that writing a policy automatically enforces it.

## Active prerequisites

1. Run `python3 tools/validate_repo.py` and `python3 -m unittest discover -s tools/tests -v`.
2. Run `python3 tools/task_context.py SOV-P00-T01 --list` and inspect the resulting targeted references.
3. Establish real Git provider branch protections and code ownership/review gates, with evidence from the actual provider.
4. Verify the GitHub workflow executes successfully on a clean checkout.
5. Close out each Phase 00 item against `QUALITY_BAR.md`; only then begin Phase 01.
