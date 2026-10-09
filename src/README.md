# Independent simulation source

All authoritative world, economy, political and combat logic belongs here, in ordinary C#/.NET libraries with no Godot dependency ([ADR-0001](../docs/architecture/ADR-0001-runtime.md), [ADR-0005](../docs/architecture/ADR-0005-toolchain-and-repository-layout.md)).

`Sovereigns.Simulation` currently provides the Phase 01 kernel. Phase 02 extends it:

| Area | Contents |
|---|---|
| `World` | One seeded world: fixed whole-millisecond steps, command application at step boundaries, location inspection, state checksum |
| `Time/` | `SimTime`, the signed millisecond clock ([ADR-0004 §4](../docs/architecture/ADR-0004-units-coordinates-time-identifiers.md)) |
| `Geometry/` | `WorldPoint` and `WorldExtent` in the world frame (metres, +x east, +y north) |
| `Commands/` | `SimCommand` types, the ordered `CommandJournal`, and `Replay` |
| `Fields/` | Availability of the spec §3.2 field families. Every physical family is reported unavailable until its phase implements it. |
| `Diagnostics/` | Structured `Logger`, sinks and `FailureReport` |
| `Content/`, `Scenarios/` | Content IDs, validated content loading, scenarios, fixtures and launch selection |

Simulation code never reads wall-clock time, `System.Random`, `Guid.NewGuid` or `string.GetHashCode`. The banned-API analyzer turns any use into a build error. Hosts inject a `TimeProvider` only for log timestamps.
