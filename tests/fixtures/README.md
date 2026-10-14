# Reference fixtures

Versioned manifests for the reference worlds defined in [the original delivery rules](../../docs/process/ORIGINAL_DELIVERY_RULES.md#a4-reference-worlds-and-cumulative-demonstrations), plus `F-EMPTY`, the Phase 01 seeded empty world. The reference worlds stay **blank** until the simulation can produce them: no seed, scenario or world content exists, and a blank fixture asserts nothing about gameplay.

Run a defined fixture by ID, headless or in the inspection shell:

```sh
dotnet run --project tools/Sovereigns.Headless -- run --fixture F-EMPTY
godot --path game -- --fixture F-EMPTY
```

| Field | Meaning |
|---|---|
| `id` | Stable fixture ID; equals the file name. Never renamed or reused. |
| `version` | Positive integer. Increment it when a change to the fixture's seed, scenario or expected content would invalidate evidence recorded against the previous version. |
| `status` | `blank` (seed and scenario are `null`) or `defined` (both are set). |
| `purpose` | What the fixture must demonstrate. |
| `seed` | World seed once defined (SOV-P01-T08, Phase 02). |
| `scenario` | Repository path of the scenario definition once defined, for example `content/core/scenario/empty_world.json`. |

Evidence cites fixtures as `ID@version`, for example `F-VALLEY@3`. A version bump requires reverifying evidence pinned to the old version ([Definition of Done](../../docs/agents/DEFINITION_OF_DONE.md#reverification-after-upstream-changes)). Add new fixtures with new IDs whenever an important interaction appears. `tools/validate_repo.py` checks the manifests.
