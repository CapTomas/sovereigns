# Task handoff and acceptance record

Use this for a tracked task, a multi-agent handoff or a change that needs durable review evidence. A small housekeeping change can use the same information in its PR or final summary. Delete optional fields that do not apply; do not create an empty evidence packet.

**Task / objective:** existing `SOV-PNN-TNN`, or the requested repository change  
**State:** In progress / Ready for review / Verified / Blocked / Superseded, if tracked ([Definition of Done](DEFINITION_OF_DONE.md))  
**Change reference:** commit/PR, or changed files when Git is unavailable  
**Author / reviewer:** when applicable

## Outcome

- What changed and which acceptance criteria it satisfies:
- Relevant authority paths/sections or decisions:
- Remaining limitations or blockers:

## Validation

- Commands/checks actually run and their result:
- Any required check not run, with the concrete reason:
- Review findings and their resolution:

## Add only when the change affects them

- Interfaces: inputs consumed, outputs produced, authoritative owner, producer/consumer contract and units/cadence (for physical-world features, also the spec §33.4 and §33.9 checklist):
- Deterministic seed or reproducible scenario:
- Human inspection path for changed world/simulation behavior: exact launch command, seed/configuration, location/entity/time, view/layer, short steps and expected observations; actual observation or execution limitation:
- Persistence, replay or migration evidence:
- Performance risk, measurement environment and result:
- Helpful screenshot/capture or other manual observation, when applicable:
- ADR, schema or documentation updates:

## Tracked task acceptance

- Required reviewer verdict and evidence reference:
- Relevant phase gate or previously accepted task evidence affected:

**Report observed results only. A generated checklist, worker assertion or passing scaffold cannot establish gameplay completion. Keep a tracked task unchecked until its required review accepts the evidence.**
