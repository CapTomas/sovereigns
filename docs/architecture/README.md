# Engineering decision records

- [ADR-0001 — Runtime and separation](ADR-0001-runtime.md)
- [ADR-0002 — Document and task authority](ADR-0002-documentation-and-task-authority.md)
- [ADR-0003 — Supported platforms, input and distribution constraints](ADR-0003-platforms-input-distribution.md)
- [ADR-0004 — World units, coordinates, precision, time and identifiers](ADR-0004-units-coordinates-time-identifiers.md)
- [System map, authoritative owners and contracts](SYSTEM_MAP.md)

Use an ADR for consequential decisions about ownership, persistence, public boundaries, runtime choices or expensive-to-reverse tradeoffs. Routine fixes and local implementation choices need no ADR. Follow [change control](../agents/CHANGE_CONTROL.md); gameplay rules remain in `docs/spec/`.

Number ADRs sequentially and never reuse a number. Record status, date, scope, context, decision, meaningful alternatives and consequences. Supersede an ADR with a new one that links back; do not rewrite an accepted decision silently. A new ADR is accepted when the PR introducing it is merged under the [Definition of Done](../agents/DEFINITION_OF_DONE.md).
