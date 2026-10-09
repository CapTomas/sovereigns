# 07 — Camera, zoom and visual level of detail

**Normative commitment:** true 90° orthographic top-down camera, world-coordinate fidelity, same world at all resolutions. **Game authority:** §§3, 22, 26–27. **Implementation owner:** Godot client and visual LOD pipeline; simulation remains independent.

## Why multiscale visual design matters

One player traverses a world from continent to a field of thousands of soldiers. Trying to draw everything at tactical detail causes visual noise and cost; drawing tactical maps as unrelated backdrops destroys the game's unique selling point. All visual LODs must share geographical anchors, masks and persistent feature IDs.

### Visual scale families

| View | Important visuals | Hide or aggregate |
|---|---|---|
| World | Relief, major watersheds, coast, climate, kingdom groups, key cities | Individual trees, roofs, tiny streams, individual soldiers |
| Region | Ridge/pass, settlements, roads, forest boundaries, significant bridges, armies | Fine soil motifs, individual farm clutter |
| Local campaign | Farm plots, small streams, passes, fortress footprints, military paths | Low-priority tiny rocks/particles |
| Tactical | Ground slope, water depth/crossings, trees/obstacles, walls and regiment formations | World labels, distant irrelevant region detail |
| Close tactical | Soldier silhouettes, front-rank weapons, micro-obstacles, local combat effects | Most distant atmosphere and map labels |

These are conceptual semantic LOD tiers. Numerical distance or zoom thresholds belong to measured implementation tuning. Each transition must be visually stable with hysteresis to avoid flicker.

## Geospatial continuity

The chosen point, river bank, road connection and fortress gate cannot jump when changing zoom or entering tactical mode. Symbolization can change, but location, orientation and navigability remain anchored to authoritative geographic coordinates. Stable seeds/IDs ensure foliage/rock positions do not change whenever the camera moves; the renderer may add constrained detail only inside permitted footprints. Height shading can simplify, but major pass gradients and battle-relevant local ridges cannot disappear or flip shape.

## Semantic camera and movement

- The camera is orthographic, facing vertically down; zoom and translation only by default. Optional map north-up lock is the baseline. If later camera rotation is added, it must not create angled projection, disorient military orders or rotate physically true wind indicators incorrectly.
- Screen-space labels and UI face the display; ground-aligned objects and directional symbols align with world coordinates.
- Zoom retains cursor/focal position predictably. Camera interpolation affects views, never authoritative army movement or world time.
- Framing controls support fit-to-army, fit-to-battle, center-on-settlement and return-to-selection. Do not change battle deployment with zoom or camera framing.

## Visual population scaling

At far tactical range, render regiment mass as compact soldier distributions/textured group shapes with clear formation edges, fronts and banners. At medium range reveal individual soldier marks; at close range use varied helmet/shield/weapon silhouettes and limited locally meaningful animation. Any soldier compression/aggregation must preserve an honest relationship to actual formation shape and approximate manpower. Reinforcement, casualties, cohesion and spacing must remain visually apparent at all scales.

## Transitions and continuity tests

For at least one river valley fixture: compare a local campaign view and tactical view at same geographic coordinates; verify bridge, river branch, road, fields, hill crest and forest margin. Reload and repeat; vary season and water stage. For army formations: gradually zoom while rotating a regiment, killing a portion, breaking cohesion and selecting it. Prohibit disappearing orientation, unit jumps, banner flip, abrupt count mismatch or high-density shimmering.

## Renderer clarity guardrails

Do not use faked perspective to make tall objects legible. When roof/wall/forest occlusion makes units unreadable, selectively fade roofs/canopy and show tactical overlays instead. Terrain material detail must reduce before orders become illegible. The camera never reveals entities outside faction knowledge solely due to zoom.
