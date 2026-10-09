# Logs, failure reports, replay and leak checks

Development diagnostics for Phase 01 (SOV-P01-T05, T09, T15). Everything here stays on the developer's machine. Sending data elsewhere needs an ADR and player consent ([quality bar](../agents/QUALITY_BAR.md#benchmarks-and-telemetry)).

## Structured log

Every host creates one `Logger` with its sinks and passes it to the world. A record ([`LogRecord`](../../src/Sovereigns.Simulation/Diagnostics/LogRecord.cs)) carries:

| Field | Meaning |
|---|---|
| `severity` | `trace`, `debug`, `info`, `warning`, `error` or `fatal` |
| `subsystem` | Writer, for example `world`, `headless`, `client`, `client.input` |
| `message` | Short fixed text; variable values go in `data` |
| `world_seed`, `sim_time_ms` | Seed and authoritative time of the world the record is about; absent for records not tied to a world |
| `object_ids` | Typed IDs such as `marker:3` ([ADR-0004 §5](../architecture/ADR-0004-units-coordinates-time-identifiers.md)) |
| `data` | String key/value details |
| `wall_time_utc` | Host wall clock, diagnostic only; simulation code never reads it |
| `exception` | Exception text, when present |

| Host | Human-readable | Machine-readable (JSON lines) |
|---|---|---|
| `sovereigns-headless` | stderr, `info` and above | `--log <file.jsonl>` |
| Godot client | Godot output panel and console | `user://logs/sovereigns-<UTC time>.jsonl`, or `--log <file.jsonl>`; the client prints the path at startup |

## Failure reports

A failure report ([`FailureReport`](../../src/Sovereigns.Simulation/Diagnostics/FailureReport.cs)) is a JSON file named `failure-seed<seed>-t<ms>ms.json`. A later report from the same seed and time gets `-2`, `-3` and so on, and never replaces an earlier one. It contains:

- the reason and exception;
- the build and platform;
- the seed, scenario and simulation time;
- the applied command journal of the completed steps;
- the pending commands;
- recent host input actions;
- the newest log records.

When a step threw part-way (`failed_in_step`), the state is partial: there is no state checksum, and the pending commands are every input of the failing step. Otherwise the report carries the state checksum, and the pending commands are those queued for the next step.

Reports are written when:

- the headless runner's simulation throws. The report goes to `artifacts/reports/`, or to `--reports <dir>`, and the exit code is 1;
- `--fail-at-ms <ms>` injects a failure, to exercise this path on purpose;
- the client's simulation step or command submission throws. The client stops advancing the world, writes the report to `user://reports/` (or `--reports <dir>`), and shows a banner with the path and the replay command;
- `F9` in the client writes a manual report without stopping;
- an unhandled .NET exception escapes outside a Godot callback and reaches the client's `AppDomain` handler, which logs it as fatal and tries to write a report. Exceptions inside Godot callbacks such as `_Process` are caught and printed by Godot itself; that is why the client's own stepping and command submission have their own handlers.

Reproduce a report. Replay rebuilds the world from seed, scenario and journal, and advances it to the reported time. It then either compares checksums, or, for a failed step, submits the pending commands and steps once to reproduce the exception. Exit code 0 means the state matches or the failure was reproduced. Exit code 3 means the state differs, the journal no longer replays as recorded, or the failure did not recur.

```sh
dotnet run --project tools/Sovereigns.Headless -- run --fixture F-EMPTY --fail-at-ms 30000
dotnet run --project tools/Sovereigns.Headless -- replay artifacts/reports/failure-seed20261009-t30000ms.json
```

A recorded command journal (`--record <file.jsonl>` in either host) replays with `run ... --commands <file.jsonl>`. The same seed, scenario, journal and end time produce the same checksum.

## Crash, memory and leak detection in development builds

| Check | Where | What it catches |
|---|---|---|
| Overflow checking | `Debug`/`ExportDebug` builds | Silent integer wraparound in simulation and client code |
| Banned APIs | Simulation build (always an error) | Wall-clock time, `System.Random`, `Guid.NewGuid`, `string.GetHashCode` |
| `LeakTests` | `dotnet test` | A discarded `World` kept alive by static state or caches |
| `ArchitectureTests` | `dotnet test` | Engine or UI references in the simulation assembly |
| Orphan nodes and memory | Client debug HUD | Godot nodes removed from the tree but never freed; managed heap and Godot static memory growth |
| Orphan-node baseline | Client test (`godot --headless --path game res://tests/ClientTests.tscn`) | Nodes the shell fails to free on teardown |
| `--verbose` exit check | CI `client` job (`run_checked.py` requires `world created` and `shutdown`; forbids `leaked at exit`, `ObjectDB instances leaked`, `still in use at exit` and `ERROR:`) | A launch that never created a world, engine errors, and Godot objects or resources still alive at exit |
| Crash output | Godot crash handler and its `user://logs/godot.log` file log | Native engine crashes, with a backtrace |

The headless runner's JSON summary reports `managed_heap_bytes` and `working_set_bytes` for each run. These are measurements, not budgets; budgets come from agreed reference hardware ([quality bar](../agents/QUALITY_BAR.md#benchmarks-and-telemetry)).
