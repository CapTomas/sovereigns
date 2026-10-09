# Tools implementation scope

Scripts in this folder operate on repository documentation and agent navigation. Keep execution deterministic, offline-capable and standard-library-only where practical. Prefer a small command or extension to an existing script over a new framework. Default output should be bounded and useful without loading whole documents.

Use the existing `tools/tests/` suite for meaningful behavior regressions. Add tests for new parsing, routing or failure behavior; avoid tests that merely copy implementation details. Run the affected test suite once after the final change, and rerun only for a change, failure or unresolved concern. See [README.md](README.md) for commands.

Fail clearly on invalid tasks or references and avoid rewriting reviewed design without user authorization. Do not treat success of document validators as proof that unimplemented Godot/.NET systems work.
