# Sovereigns — Visual direction

**Authority:** normative for visual appearance, rendering presentation, asset language, animation, art production and visual acceptance. **Status:** adopted first-edition direction. **Target:** Godot 4 .NET, fully 2D orthographic. **Last decision:** illustrated-naturalistic terrain + restrained stylization + scalable mass-army visuals.

This is the focused visual counterpart of `docs/spec/27-presentation-requirements-and-visual-design-boundary.md`. It does **not** alter physics, combat, weather, geography, economy, political control, or other simulation rules. Those remain authoritative in `docs/spec/`. The document defines how those states look and how players read them.

## Visual identity in one sentence

**A living, believable medieval atlas that becomes an elegant, readable, enormous 90° overhead tactical battlefield.** Nature has convincing continuity and nuanced texture; man-made and military forms are composed from clear, restrained shapes; the UI speaks in precise modern cartography with subtle medieval character.

## Decisions that are fixed

- **2D orthographic, true overhead 90°**, in campaign and tactical views. No perspective tilt, isometric projection, faux-three-quarter units, 3D terrain or angled soldier assets.
- **Hybrid of naturalistic illustration and minimal stylization.** Geological forms, waterways, vegetation and climate feel convincing; detail is filtered for readability. Not photographic, not procedural noise wallpaper, not deliberately retro pixel-art, not pastel toy miniatures.
- **Army scalability comparable to high-density overhead strategy games:** thousands of visible soldiers are a *formation texture*, not thousands of individually elaborate animated characters. Mass, frontage, banners, equipment silhouette, facing and motion convey combat.
- **One world, one visual truth.** Campaign and battle read the same authoritative geography and weather. LOD adds visual detail but cannot invent a contradictory river, road, building, slope or forest.
- **Simulation-driven season, weather and damage.** Art follows state; visuals never produce their own weather, water depth, forests, burned buildings or political boundaries.
- **Style scales with zoom.** One visual family across continental, regional and tactical views, with different symbolization and density.
- **No mandatory paper-cutout/watercolor treatment.** Painterly restraint is welcome, heavy paper grain, thick outlines, decorative wash and imitation parchment across the world are not. This supersedes exploratory art prompts; there is no fixed watercolor requirement.
- **Visual readability is part of functionality.** Infantry, cavalry, archer formations, elevation, crossings, control, selected orders and fog of war must be legible, including without color alone.

## Agent context without opening the entire folder

From repository root, run `python3 docs/visual/visual_context.py --topics` and then `python3 docs/visual/visual_context.py soldiers` (or another topic) for the minimum file list. Add affected authoritative gameplay chapters as required by the task; this router never authorizes skipping a cross-system contract. To print the routed text directly, append `--show`.

## Read only the relevant file

| Working on | Read first | Read if affected |
|---|---|---|
| Visual identity and style approval | `SOVEREIGNS_VISUAL_DIRECTION.md` | `08-COLOR-TYPOGRAPHY-AND-TEXTURE.md` |
| World terrain, rivers, forests, fields | `01-TERRAIN-AND-NATURE.md` | `05-WEATHER-SEASONS-AND-LIGHT.md`, `07-CAMERA-ZOOM-AND-LOD.md` |
| Strategic map, settlements, borders | `02-CAMPAIGN-MAP.md` | `04-SETTLEMENTS-AND-FORTIFICATIONS.md`, `06-UI-AND-CARTOGRAPHY.md` |
| Tactical battlefield terrain | `03-TACTICAL-BATTLEFIELD.md` | `01-TERRAIN-AND-NATURE.md`, `07-CAMERA-ZOOM-AND-LOD.md` |
| Soldier, regiment, horse, siege sprites | `09-ARMIES-AND-ANIMATION.md` | `03-TACTICAL-BATTLEFIELD.md`, `11-PERFORMANCE-AND-RENDERING.md` |
| Villages, buildings, roads, castles | `04-SETTLEMENTS-AND-FORTIFICATIONS.md` | `01-TERRAIN-AND-NATURE.md` |
| Clouds, snow, rain, lighting, fires | `05-WEATHER-SEASONS-AND-LIGHT.md` | `10-EFFECTS-AND-FEEDBACK.md` |
| Menus, overlays, data visualization | `06-UI-AND-CARTOGRAPHY.md` | `08-COLOR-TYPOGRAPHY-AND-TEXTURE.md` |
| Camera, zoom, screen-space mapping | `07-CAMERA-ZOOM-AND-LOD.md` | relevant campaign/tactical module |
| Art palette, graphic tokens, iconography | `08-COLOR-TYPOGRAPHY-AND-TEXTURE.md` | `tokens/palette.json` |
| Procedural asset pipeline or shaders | `11-PERFORMANCE-AND-RENDERING.md` | `12-ASSET-PIPELINE-AND-GOVERNANCE.md` |
| Visual review and art verification | `13-LOOKDEV-AND-VISUAL-QA.md` | `reference/SCENE_BRIEFS.md` |

## Authority and acceptance

1. Visual documents govern only appearance and presentation. Gameplay/physics remains in `docs/spec/`, especially §§3–6, 21–22, 26–27. Architecture remains in `docs/architecture/`.
2. A new look-dev render or asset is **not** canon solely because it looks pleasing. Art approval requires the side-by-side acceptance cases in `13-LOOKDEV-AND-VISUAL-QA.md`.
3. No asset should be accepted merely because it works at one zoom, one season or one faction color.
4. Keep files modular; do not create a second monolithic bible. Record changes in the relevant file and decision log within `SOVEREIGNS_VISUAL_DIRECTION.md`.
5. Art/visual agents should read `AGENTS.md` in this directory, and repository-level `AGENTS.md`.

## Reference documentation

- [Visual direction](SOVEREIGNS_VISUAL_DIRECTION.md)
- [Terrain and nature](01-TERRAIN-AND-NATURE.md)
- [Campaign map](02-CAMPAIGN-MAP.md)
- [Tactical battlefield](03-TACTICAL-BATTLEFIELD.md)
- [Settlements and fortifications](04-SETTLEMENTS-AND-FORTIFICATIONS.md)
- [Weather, seasons and light](05-WEATHER-SEASONS-AND-LIGHT.md)
- [UI and cartography](06-UI-AND-CARTOGRAPHY.md)
- [Camera, zoom and LOD](07-CAMERA-ZOOM-AND-LOD.md)
- [Color, typography and texture](08-COLOR-TYPOGRAPHY-AND-TEXTURE.md)
- [Armies and animation](09-ARMIES-AND-ANIMATION.md)
- [Effects and feedback](10-EFFECTS-AND-FEEDBACK.md)
- [Performance and rendering](11-PERFORMANCE-AND-RENDERING.md)
- [Asset pipeline](12-ASSET-PIPELINE-AND-GOVERNANCE.md)
- [Look-dev and visual QA](13-LOOKDEV-AND-VISUAL-QA.md)
- [Reference scene briefs](reference/SCENE_BRIEFS.md)
- [Palette tokens](tokens/palette.json)

## Operational aids

- [Machine-readable context map](VISUAL_CONTEXT.json)
- [Local context router](visual_context.py)
- [Asset handoff template](reference/ASSET_DELIVERY_TEMPLATE.md)
