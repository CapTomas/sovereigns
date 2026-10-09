# 03 — Tactical battlefield presentation

**Purpose:** deliver large army command on the authentic terrain of the engagement. **Game authority:** §§3, 20, 22–24, 26. **Related:** terrain, soldiers, LOD, effects, UI.

## Battlefield appearance

The view is true **90° overhead** in a 2D projection. The battlefield is a larger-detail atlas of the region beneath it, not a new painted arena. Rivers, field boundaries, roads, ridges, walls, forests and bridges correspond to the actual campaign world at the battle timestamp. The tactical camera can zoom/pan without tilt. Terrain material differences remain readable even when populated by many formations.

## Ground and obstacles

- Gentle hills use relief shading and surface/contour cues; steep terrain exposes credible visual breakpoints without changing simulation height.
- Mud, frost, snow, puddling and floodwater are tied to local environmental fields. Blend gradual changes and preserve unit visibility.
- Rivers have water edges, connected channels, readable crossings and current conditions. Fords/bridges must visually reflect the same passability/capacity data used by command logic.
- Forests provide visually meaningful cover/density and allow selected units to remain visible through controlled canopy treatment. Shrubs/rocks/fences are shown only when physical obstacle/cover state supports them, or explicitly marked as purely decorative nonblocking forms.
- Fortifications, trenches, stakes and destroyed bridges render at authoritative coordinates and retain their conditions after battle.
- Battle perimeter is communicated as an operational command area/engagement bounds when needed. Never suggest the landscape ends at the edge of the rectangle.

## Formations and combat readability

- Regiment silhouette and **facing** outrank any individual soldier detail.
- Show frontage, depth, spacing, movement direction and rough unit type clearly before using banners/selected outlines to distinguish regiments.
- Soldiers may visually compress at lower zoom, but blocks must preserve rough count meaning and mass; a regiment cannot look full-strength after severe casualties with no feedback.
- Selected formations show move/order ghosts, target direction, command obstruction and expected path. The route is a visual of the movement order, not a separate client-side pathing authority.
- Friendly versus hostile and hidden versus revealed states must remain distinguishable in all lighting/weather. Unit roster selection exists when click targets overlap.
- Dense fighting can show localized front-rank clashes, disturbance, casualty gaps, dust and small flashes of equipment; avoid continuous large explosions, thick fog or clutter hiding frontline geometry.

## Battle phases

**Preparation:** clear deployment zones, approach corridors, relevant terrain and forecast uncertainty. Never render unseen defenders' positions just because the client knows world state.

**Contact:** emphasize command states, line overlap, flank/rear direction, forest obstruction, mobility constraints, missiles and morale signals. Environmental cues reveal why a unit slows, disorganizes or loses visibility.

**Outcome and aftermath:** rout paths, captured positions, destroyed structures, fires, equipment debris and terrain damage appear where they happened. They persist into future visits where the game state persists them. The battle's closing UI summarizes consequences instead of wiping the field as an unconnected animation.

## Tactical information overlays

Always available: relative elevation/slope, ground firmness, water depth and crossing accessibility, visibility/fog, wind direction/speed, snow/ice, unit fatigue/cohesion and order queues. These overlays use the same field queries as the simulation, with clear units and legend. Advanced physical fields may live in inspector/debug view but must be reproducibly viewable by developers.

## Critical acceptance test

Choose a real campaign location; record coordinates, timestamp, river centerline, bridge, forest, hill crest and road; enter battle; compare positions and environmental fields. Fight during a passing rain front; river/soil/wind visuals must change from the shared state. Damage a bridge; exit battle; inspect campaign map and re-enter. No reshuffle or reconstruction of the missing bridge is permitted.
