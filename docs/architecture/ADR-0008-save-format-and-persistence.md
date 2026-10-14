# ADR-0008 — Save format, integrity and persistence policy

**Status:** Accepted on merge of its introducing PR (SOV-P02-T12, T17).
**Decision date:** 2026-10-14
**Scope:** The versioned save format, its integrity and compatibility rules, and how authoritative state and deltas are persisted. It is set before persistent gameplay objects exist so that every later system follows it from the start.

## Context

Spec §29.8 requires saves to capture the seed, generator and rules versions, calendar, all active and persistent entities, event state and random state. Load validation must detect missing or invalid references rather than inventing replacements, and engine changes need explicit migration or declared compatibility limits. Spec §3.11 adds that procedural algorithm updates must not silently change existing saves, that unchanged detail may be regenerated, and that logged changes cannot be forgotten. The system map makes Persistence the only writer of save data, through versioned contracts.

## Decision

### Contents

A save is one JSON document (UTF-8, snake_case, the strict `SimJson` options):

```text
{ format: "sovereigns-save", format_version, rules_version, saved_at, payload_sha256,
  payload: { identity, state, ledger } }
```

- `identity` is the immutable `WorldIdentity` (ADR-0006).
- `state` is mutable authoritative state at one consistent simulation time: the clock, commands applied so far (so journal sequences continue), ID counters, markers, breakpoints and pending decisions. Each later system adds its owned components here, each in its own versioned section.
- `ledger` is the event history: the deltas that cannot be regenerated (spec §3.11).
- Never saved: derived values, caches (rebuilt from sources), host pacing (`TimeControl`), the command journal (replay input, not state) and anything regenerable from seed and generator version.

Payload JSON is deterministic: fixed member order, shortest round-trip number formatting, ID counters in ordinal key order, and event data in the order the simulation built it. Saving the same world twice gives identical bytes.

### Integrity and compatibility

Loading checks, in order, and reports every problem it finds instead of stopping at the first:

1. The `format` name. A newer `format_version` is refused ("use a newer build"). An older one needs a declared migration chain, `format_version` N → N+1, applied in order; none exists yet.
2. `rules_version` must equal this build's `SimulationRules.Version`. A save from other rules needs a declared migration, or it is refused with the reason. Outcomes are never silently recomputed under new rules.
3. `payload_sha256` must match the recomputed hash of the canonical payload. A mismatch means corruption or editing.
4. The identity's `generator_version` must be one this build can regenerate exactly (`SimulationRules.RegenerableGeneratorVersions`). Otherwise unsaved base state, such as coastlines and rivers, would come out differently, and the save is refused unless a migration is declared (spec §3.11).
5. References:
   - ID counters name known kinds and are non-zero;
   - every ID was issued by its kind's counter and appears once;
   - event causes name only commands that were applied;
   - markers lie inside the world;
   - breakpoints lie on future step boundaries and point to their `BreakpointSet` event;
   - pending decisions were raised at the save time, appear once, and point to their `DecisionRaised` event;
   - the ledger is chronological and ends at or before the save time.

   A missing or invalid reference is an error and is never replaced.

A faulted world (ADR-0007) has partial state and cannot be saved; hosts write a failure report instead.

### Equivalence requirement

Loading a save and continuing with the same later commands must produce the same state checksum as the uninterrupted run. Tests prove this on generated command streams and on fixture `F-KERNEL`. Every new persistent component must keep this property.

### Hosts

The headless runner writes `--save <file>` at the end of a run and continues a world with `--load <file>`. A failure report from a loaded session carries the save's payload hash. `replay` then requires that save (`--save`) and checks the hash. The Godot client does not read or write saves yet: persistence has no dependency on the client.

### Policy for later changes

- Adding a component: add a section with its own version, include it in validation and checksums, and show save, load and continue equivalence.
- Changing a persisted shape or meaning: bump `format_version` and add a migration with a test that loads a save of the older version.
- Changing outcomes: bump `SimulationRules.Version`, and either declare that older saves are incompatible or provide a migration.
- Renaming content or event types needs a migration entry (ADR-0004 §5); event types are append-only.

## Alternatives considered

- **A binary format such as MessagePack or a custom one:** smaller and faster, but opaque to agents and diffs while the schema is young. Revisit with measured save sizes (Phase 49, Phase 51), behind the same versioning and integrity rules.
- **Saving the command journal and replaying it on load:** small files, but load time grows with campaign length, and every rules change would break old saves. Saves hold state; journals stay replay tools.
- **Accepting rules-version mismatches with a warning:** contradicts §3.11's "existing saves must not silently change".

## Consequences

- Persistence is exercised from Phase 02, before gameplay data, so schema discipline exists when the first heavy state arrives.
- Save size grows with the ledger. Compaction, archives and chronicle summarization are Phase 49 work and must keep the deltas that cannot be regenerated.
