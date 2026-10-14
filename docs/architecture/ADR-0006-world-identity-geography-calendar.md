# ADR-0006 — World identity, geographic reference and calendar

**Status:** Accepted on merge of its introducing PR (SOV-P02-T01, T02).
**Decision date:** 2026-10-14
**Scope:** What identifies a world, how the planar frame of ADR-0004 maps to latitude and local solar time, and the calendar and chronology every system uses. Solar geometry (sun position, day length) is Phase 06; it builds on these definitions.

## Context

Spec §3.2 lists the geographic reference (seed, scale, coordinates, latitude, sea-level datum, local time and season) as the first field family. Spec §5.1 sets the default calendar: 365 days, twelve recognizable display months, a stable seasonal model and a day–night cycle appropriate to the generated latitude. Spec §2.3 makes a strategic turn seven days, with seasons of about 13 turns. Spec §3.11 and §29.8 require determinism and saves to be defined by seed, rules version, inputs and saved state. ADR-0004 fixed SI units, the y-up planar frame, millisecond timestamps since the world epoch, and typed IDs.

## Decision

### World identity

`WorldIdentity` is created once and never changes:

| Member | Meaning |
|---|---|
| `Id` | Durable 128-bit world ID (32 hex digits). The host draws it from operating-system randomness when it creates a world; the simulation never generates or reads randomness for it. |
| `Seed` | 64-bit world seed; the root of every random stream (ADR-0007). |
| `Scenario` | Content ID of the scenario that set the world up. |
| `RulesVersion` | `SimulationRules.Version`, increased with any change that alters outcomes. |
| `GeneratorVersion` | World-generator version; `0` means "set up by a scenario without generation" until Phase 04. A save loads only if this build can regenerate its version (ADR-0008). |
| `Extent`, `Geography`, `StepMs`, `Start` | The coordinate contract: frame size, geographic reference, fixed step, and start time. |

The state checksum covers everything except `Id`. Two worlds from the same seed and commands are equal in state but remain distinct worlds, and a replay can check equality without copying an identity.

### Geographic reference

The world is planar (ADR-0004 §2). `GeographicReference` stores only two authored inputs, and every transcendental result is a derived query, never saved state:

- `SouthEdgeLatitudeRad`: latitude at y = 0. Latitude is φ(y) = φ₀ + y / R, with R = 6 371 000 m (Earth's mean radius, IUGG), so north–south distance is true distance.
- `StandardMeridianX`: the x whose local mean solar time is **world standard time**, the time on the world clock.
- Local mean solar time at x = standard time + (x − x_std) / (R · cos φ_mid) · 86 400 s / 2π, where φ_mid is the latitude at the world's mid y. East–west scaling therefore uses one latitude, so longitude never depends on y. The equation of time is ignored, so local solar time is mean time.
- Hemisphere is the sign of φ. The whole world must lie within ±85°, and the standard meridian must lie inside it.
- The sea-level datum is z = 0 (ADR-0004); elevation arrives in Phase 04.

Until world generation exists, a scenario authors the placement in degrees (`south_edge_latitude_deg`, `standard_meridian_east_m`).

### Calendar and chronology

- **Day:** 86 400 000 ms. **Week:** 7 days, one default strategic turn (spec §2.3).
- **Year:** 365 days, with twelve months of Gregorian common-year lengths (31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31) and **no leap days**, so seasons do not drift.
- **Epoch:** `SimTime` 0 is 00:00 world standard time on 1 January of year 1. Years use astronomical numbering: the year before 1 is year 0, and negative times are earlier years, which generated history may use (ADR-0004 §4).
- **Day boundaries** are standard-time midnights. Systems with a daily cadence run there, so a daily system neither skips nor repeats a day, whatever steps a host takes (spec §30.2).
- **Seasons** are meteorological. In the northern hemisphere, winter is December–February, spring March–May, summer June–August and autumn September–November; the southern hemisphere is mirrored. They last 90, 92, 92 and 91 days, which is 12.9–13.1 turns. Seasons are calendar conventions for scheduling and display. Physical forcing comes from solar geometry (Phase 06), not from the season label.
- **Canonical text** for logs and files is `YYYY-MM-DDThh:mm:ss.fff`. Month names and other display formats belong to the client and localization (ADR-0004).
- **Campaign start:** each world has a start time in its identity; the scenario sets it now, and world generation will later.

## Alternatives considered

- **Leap years (Gregorian 365.2425 days):** more familiar dates, but seasons would drift against a fixed axial model, or the solar model would need leap logic. The spec asks for a stable seasonal model.
- **Twelve 30-day months plus five extra days:** regular arithmetic, but less "recognizable" than real month lengths (spec §5.1).
- **Longitude and latitude as the primary frame:** rejected in ADR-0004. A derived mapping keeps the plane authoritative.
- **Storing derived trigonometric values in the identity:** faster queries, but `cos` may differ in the last bit across math libraries, and that difference would enter checksums.
- **Season starts at solstices and equinoxes:** astronomically exact, but the dates depend on the solar model. Meteorological seasons are stable calendar conventions; Phase 06 owns solar forcing.

## Consequences

- Every timestamp in logs, saves and reports can be read as a calendar date. Human-readable logs print calendar time; machine logs keep milliseconds.
- Phase 06 computes solar declination, sun angles and day length from this calendar and geographic reference, and must not redefine either.
- The scenario schema gains `geography` and `start`; fixture `F-EMPTY` moved to version 2 because its scenario gained them.
