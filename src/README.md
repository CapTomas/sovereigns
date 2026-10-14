# Independent simulation source

All authoritative world, economy, political and combat logic belongs here, in ordinary C#/.NET libraries with no Godot dependency ([ADR-0001](../docs/architecture/ADR-0001-runtime.md), [ADR-0005](../docs/architecture/ADR-0005-toolchain-and-repository-layout.md)).

`Sovereigns.Simulation` provides the Phase 02 kernel. Its contracts are in [ADR-0006](../docs/architecture/ADR-0006-world-identity-geography-calendar.md) (identity, geography, calendar), [ADR-0007](../docs/architecture/ADR-0007-simulation-kernel.md) (kernel) and [ADR-0008](../docs/architecture/ADR-0008-save-format-and-persistence.md) (saves).

| Area | Contents |
|---|---|
| `World` | One world: the clock and event-driven `AdvanceTo`, journaled world commands (`Submit`), pacing commands (`Control`), decisions, location inspection with provenance, state checksum, fault state |
| `Identity/` | `WorldId`, `WorldIdentity`, `GeographicReference` (latitude and local solar time from the planar frame), `SimulationRules.Version` |
| `Time/` | `SimTime`, the signed millisecond clock ([ADR-0004 §4](../docs/architecture/ADR-0004-units-coordinates-time-identifiers.md)), and `WorldCalendar` |
| `Kernel/` | `ISimulationSystem` and the scheduler, system priorities, the calendar system, `TimeControl`, breakpoints and decisions, `RealTimePacer` |
| `Events/` | The ordered `EventLedger` of `WorldEvent`s, each with a cause and subjects |
| `Ids/` | Typed `EntityId<TKind>` and the per-kind `IdAllocator` |
| `Random/` | Counter-based `RandomStream`, keyed by seed, purpose, place and identity |
| `State/` | `OwnershipRegistry`, `StateVersion` and `DerivedCache` (single writer, version-stamped invalidation) |
| `Persistence/` | `SaveFile`: versioned, hashed, reference-validated saves |
| `Geometry/`, `Markers/`, `Fields/` | World frame types, developer markers, field-family availability and readings |
| `Commands/` | Command types, the `CommandJournal` and `Replay` |
| `Diagnostics/` | Structured `Logger`, sinks and `FailureReport` |
| `Content/`, `Scenarios/` | Content IDs, validated content loading, scenarios, fixtures and launch selection |

A new subsystem registers an `ISimulationSystem` with a priority from ADR-0007, owns its components, records ledger events with causes, draws from keyed random streams, returns readings with provenance, and adds its state to saves under ADR-0008. Simulation code never reads wall-clock time, `System.Random`, `Guid.NewGuid` or `string.GetHashCode`; the banned-API analyzer turns any use into a build error.
