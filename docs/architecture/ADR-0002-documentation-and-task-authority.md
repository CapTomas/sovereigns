# ADR-0002 — Modular authority, routing and status

**Status:** Accepted for repository Phase 00.

- The 39 originally numbered bible chapters in `docs/spec/` are normative. Chapter IDs remain stable and agent tasks refer to specific IDs.
- Delivery tasks are 55 phase Markdown documents in `tasks/phases/`, using stable IDs. Their checkboxes represent accepted verification, not merely attempted work.
- `meta/` is generated search/routing metadata, not a replacement design authority.
- `AGENTS.md` and nested scope files give brief working instructions; they are not new gameplay rules.
- Root entrypoints and one task router reduce repeated agent context loading, while allowing mandatory cross-domain discovery when actual changes affect neighbors.
- Source monoliths were split without silently editing original normative rules; provenance records record source fingerprints in `meta/migration.json`.
- Changes to a chapter require downstream contract review and appropriate tests; changes to task texts/IDs require routing validation.
