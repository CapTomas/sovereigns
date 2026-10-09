# ADR-0004 — World units, coordinates, precision, time and identifiers

**Status:** Accepted on merge of its introducing PR (SOV-P00-T10).  
**Decision date:** 2026-10-09  
**Scope:** Global conventions every simulation, client, tool and content author must use. Phase 02 implements them (world ID SOV-P02-T01, calendar T02, random streams T06, stable IDs T08). Subsystems add field-specific details without contradicting this record.

## Context

Spec §3.2 and §3.5 require one canonical coordinate and elevation reference, units for all physical quantities, and unambiguous meanings for values like zero wind or freezing. §29.3 requires reproducible results from seed, configuration, state and ordered inputs. Without one convention, independently built subsystems drift into incompatible units, axis directions and ID schemes.

## Decision

### 1. Units

Authoritative physical state uses **SI units**. Conversion to player display units happens only in the client and localization layer (§28).

| Quantity | Internal unit |
|---|---|
| Distance, elevation, geometric depth (water level, snow depth, mud) | m |
| Area / volume | m² / m³ |
| Mass (goods, biomass, water) | kg |
| Physical rates and durations inside formulas | s (see §4 for timestamps) |
| Temperature | K (fresh-water freezing point 273.15 K) |
| Pressure | Pa |
| Speed, wind, current | m s⁻¹ |
| Discharge | m³ s⁻¹ |
| Water stored per area (rain, snow water equivalent, ponding) | kg m⁻² (numerically mm of liquid water) |
| Water flux per area (precipitation, evaporation, infiltration, melt) | kg m⁻² s⁻¹ |
| Air moisture | specific humidity kg kg⁻¹; relative humidity is derived |
| Radiation / energy flux | W m⁻² |
| Angles | radians (degrees only in display and authored config) |
| Fractions, cover, probabilities | dimensionless 0–1, never percent |

Depth describes geometry and kg m⁻² describes water budgets. For liquid water, 1 kg m⁻² equals 1 mm of depth. Snow depth and snow water equivalent are separate fields (§3.5).

Wind is stored as a vector (east, north) in m s⁻¹ pointing in the direction the air moves, at 10 m above the local surface with open exposure. Displays may show the meteorological "from" direction.

Non-physical quantities (money, prices, morale, legitimacy) declare their unit, range and conservation rule in the owning subsystem's spec or ADR when introduced.

Every authoritative field declares its unit and its meaning at zero, at boundaries and when unavailable (`src/AGENTS.md`). Thresholds such as "dry soil" or "frozen ground" belong to the owning field, not to consumers.

### 2. Coordinates

- **One world frame:** a bounded, planar, right-handed frame in metres. **+x is east, +y is north**, and the origin is the world's south-west corner, so in-world positions are non-negative. World extent comes from world generation.
- **Elevation:** z in metres above the world's sea-level datum, negative below it.
- **Geography:** distances and slopes come from planar world metres. Latitude, longitude-like solar time and day length are derived from position by the world's geographic reference (SOV-P02-T01, Phase 06). The plane stays the authoritative geometry.
- **Grids:** each field declares its grid origin and cell size. Default convention: cell `(i, j)` covers `[x₀ + iΔ, x₀ + (i+1)Δ) × [y₀ + jΔ, y₀ + (j+1)Δ)`, and its sample represents the cell centre. A field that deviates must say so.
- **Tactical refinement** uses absolute world coordinates. Persisted battle results and terrain changes are written back in the world frame.
- **Client:** Godot's screen space is y-down. The client converts at its boundary (flip y) and renders relative to a camera-local origin in 32-bit floats. Render coordinates never flow back into simulation state.

### 3. Precision and determinism

- Authoritative continuous state and all conserved-quantity bookkeeping use IEEE-754 **double** (`double`). A field may store bulk samples as `float` only when it declares the quantization and shows the error is within its tolerance. Accumulation still happens in `double`.
- Countable entities that the spec treats as discrete (soldiers in a regiment, buildings) use integers.
- **Determinism target:** bit-identical state checksums require all of the following to match: build, .NET runtime, OS math library, CPU feature level, seed, configuration, initial state and ordered inputs. When any platform element differs, checksums are computed over quantized state and compared within declared tolerances, because math-library transcendental functions may differ. SOV-P02-T15 proves seed and command-stream repeatability. Cross-platform comparison is measured once CI runs on more than one OS, and the achieved level is recorded by amending this ADR.
- Simulation code must not use wall-clock time, unseeded randomness, `Guid.NewGuid()`, hash-set/dictionary enumeration order, or parallel reductions in nondeterministic order to decide outcomes. Fused multiply-add is used only through explicit calls.

### 4. Time

- **Authoritative timestamp:** signed 64-bit integer count of **milliseconds** since the world epoch (range about ±292 million years). Generated pre-campaign history may use negative times.
- SOV-P02-T02 defines the epoch, calendar, day length, seasons and local solar time. Authoritative state has no leap seconds and no time zones; local solar time is derived from the clock and position.
- Fixed steps (tactical, physics) are whole milliseconds. Integrators convert the step to seconds as `double`.
- Rendering and UI time are separate client clocks and never advance or alter authoritative state (ADR-0001).

### 5. Identifiers

- **Entity IDs** (natural features, settlements, facilities, armies, regiments, political entities, events, tactical objects): unsigned 64-bit values, wrapped in a distinct type per kind in code. The owning subsystem allocates them from a per-world, per-kind monotonic counter. They are never reused within a world and `0` means "none". Per-kind counters stop allocation in one subsystem from shifting IDs in another. Kinds and persistence: SOV-P02-T08.
- **Content and asset IDs** (crops, commodities, equipment, buildings, institutions, sprites, sounds, localization keys): stable strings `namespace:kind/name`, using lowercase `a–z`, `0–9` and `_` in each segment, for example `core:crop/wheat` or `core:sprite/unit_foot_spear_shield_top_01`. The base game uses namespace `core`; each mod uses its own namespace. Renaming requires a migration entry. Generated proper nouns are display data, not IDs.
- **Random streams** derive from (world seed, system/stream purpose, place, entity or event ID), as SOV-P02-T06 requires. They never depend on how many draws an unrelated subsystem consumed (SOV-P02-T06).
- **World identity:** world ID, seed and generator/rules versions are defined by SOV-P02-T01 and stored in every save (§29.8).

## Alternatives considered

- **Fixed-point integers for positions and conserved stores:** strongest cross-platform determinism, but every numeric routine needs custom arithmetic, and floating-point physics libraries become unusable. Revisit only if cross-platform measurements show unacceptable drift for a gameplay-relevant result.
- **float32 authoritative positions:** precision falls with distance from the origin. At an illustrative 4,000 km extent (world size is not yet decided), float32 spacing is about 0.25–0.5 m, too coarse for tactical refinement in shared world coordinates.
- **Geographic latitude/longitude as the primary frame:** spherical geometry everywhere for little gameplay value. The spec allows geographic simplifications, provided latitude effects are derived consistently.
- **Seconds or ticks of a fixed tactical step as the time unit:** seconds are too coarse for sub-second battle steps, and step-based ticks couple global time to one subsystem's cadence.
- **Global entity counter or random GUIDs:** a global counter couples unrelated subsystems' allocation order, and GUIDs are not deterministic.

## Consequences

- Phase 01–02 code introduces typed IDs, a millisecond clock type and unit-annotated field declarations from the start. Retrofitting later would break saves.
- Display code needs a units/localization formatter. Simulation code never formats for display.
- Cross-platform determinism is a measured property, recorded by amending this ADR once CI runs on more than one OS. It is not an assumption.
