# 13 — Look development, acceptance and visual QA

**Purpose:** produce evidence that an attractive isolated image also works as a large, readable, data-correct game scene. **Ownership:** visual reviewer signs off; agents provide tests and captures.

## Minimum look-dev sequence

1. **Environmental foundation:** one world valley with hills, tributaries, road, fields, forest and settlement, using real/fixture physical input. No soldiers yet. Match relief and geographic position.
2. **Readable tactical terrain:** same valley at battle scale. Preserve banks, fords, ridge and road. Verify material transitions and crossing affordance.
3. **Formation integration:** infantry, archer and cavalry regiments, selection/commands, two factions, overlap, route around obstacles.
4. **Environmental range:** clear noon, dusk, night, heavy rain, post-rain wet soil, patchy snow, seasonal foliage; query physical state to verify.
5. **Stress readability:** tens of thousands of simplified soldier marks, many units, banners, fog of war, falling rain, selection and control overlays.
6. **Production readiness:** assets have sources, legal rights, stable exports, actual Godot integration, profiling, accessibility, multi-resolution checks and review evidence.

## Reference fixture policy

See `reference/SCENE_BRIEFS.md` for ten canonical scene briefs. Each fixture has a fixed test world seed, time, camera, authoritative fields and participating entities once the relevant game systems exist. A scene brief is not an invented engine result. Bind fixtures to actual reproducible simulation saved states when implemented. Keep both polished render and debug overlay captures at the same world coordinates and time.

## Visual acceptance matrix

| Dimension | Pass question | Failure example |
|---|---|---|
| World identity | Same river/bridge/forest/hill in both modes? | Battlefield replaces valley with an unrelated map |
| Camera | Absolutely overhead 2D across all assets? | Soldiers reveal standing legs and faces |
| Terrain | Can player infer hills, obstacles, water, wet ground? | Mountains look flat, road vanishes under shading |
| Army | Facing/frontage/type and selection readable at battle zoom? | Dense markers are an indistinguishable blob |
| Weather | Current state is derived from physical simulation? | Rain animation with dry field instantly becoming muddy |
| Hierarchy | Orders and danger readable with effects active? | Clouds/particles hide selected regiment |
| LOD | Transition stable and identity preserved? | Trees jump, banners flip, river changes width without water change |
| Accessibility | Can critical cues be understood without color alone? | Allies/enemies only differ red vs green |
| Performance | Demonstrated on representative hardware? | 30k sprites performant alone, but frame drops in integrated view |
| Provenance | Can asset source/export/license be audited? | Unattributed web screenshots copied as runtime textures |

## Standard capture sets

For each integrated art feature, capture: `campaign-far`, `regional`, `tactical-overview`, `tactical-close`, `selected`, `overlap`, `grayscale`, `color-vision-simulation`, and `reduced-effects` where applicable. Include world seed, coordinates, clock, weather summary, army count, resolution, display scale, Godot build hash and relevant graphics settings in accompanying metadata. Never claim objective performance from captures alone; attach benchmark outputs for speed/memory assertions.

## Review outcome states

- **Draft direction:** design proposal, not authoritative.
- **Look-dev accepted:** visual principle demonstrated; integration still pending.
- **Asset production accepted:** source and export meet constraints; live binding may be pending.
- **Integrated production accepted:** real input/data, gameplay compatibility, LOD, accessibility, performance and review evidence passed.

No test fixture or absent game system may be “passed” by substituting manually drawn simulation output. If system isn't built, mark the test blocked and keep the visual work at its honest stage.

## Failure and revision process

Describe the violated invariant, associated world coordinates/scene, screenshot and inspector evidence, expected appearance, actual outcome, severity, root cause owner (rendering, source art, sim data or UI), fix proposal and regression scenario. Re-run related scene comparisons. Visual tuning should not alter simulated values to make a screenshot prettier.

## Sign-off roles

Visual art review approves tone/composition. UI/accessibility review approves interactive clarity. Engineering review verifies bindings, provenance, resource usage and visual-data ownership. Gameplay/design review verifies that affordance presentation doesn't misrepresent world physics or commands. One reviewer may cover multiple roles, but evidence for each perspective must exist.
