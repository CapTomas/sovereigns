# 02 — Campaign map

**Focus:** a grand, legible atlas of a physically real, procedurally generated medieval world. **Game authority:** §§2, 17, 20–21, 26. **Related:** terrain, settlements, UI, LOD.

## First impression

At continental scale show landforms and significant water before labels and borders. The map must feel like an inhabited landscape, not a flat political board. Geography explains why borders, settlements and travel corridors exist. Large mountains, river basins, deserts, fertile plains and coastlines remain recognizable without zooming in. Ambient clouds and day/night treatment do not obscure map use.

At regional scale show river bends, access corridors, villages, field parcels, forest edge, paved/unpaved roads, fortresses, passes and army movement. Settlements have plausible footprints and are placed at their actual world locations. An army symbol should always be geographically anchored, not floating over an arbitrary province centroid.

## Political and military information

- **Political sovereignty**: discrete, restrained border delineation with shaded controlled color; terrain remains visible below. Claimed territory and legal ownership are distinct from military access/control.
- **Military control**: transparency/gradient/edge style and geographically credible corridors/zones tied to armies, garrisons, roads, bridges and forts. Show contested or uncertain control without solidly repainting the map as legal annexation.
- **Passage rights**: clear symbols/route styling for permitted, unauthorized, blocked or disputed transit, not a color that confuses access with conquest.
- **Encirclement**: cut supply approaches highlighted, interior areas identified as isolated if model says so; do not fill enclosed polygons as instantly conquered territory.
- **Supply**: roads and river routes have meaningful hierarchy, capacity bottlenecks, reachable depots and interrupted lines. Use sparing flow indicators and diagram overlays only when requested.
- **Fog of war**: map shows what the faction knows. Do not render hidden forces as normally visible. Differentiate explored terrain, uncertain intelligence, stale reports and currently observed threats.

## Armies and locations

At far zoom armies are readable symbols, footprint estimates or regiment/group markers with direction. At intermediate zoom show distinct group positions and approach routes. At near campaign zoom show world-ground association, baggage or movement as appropriate, without pretending individual combat occurs in campaign mode. Selection and threat/hazard marks sit atop ambient map elements.

Cultural differences appear in constructed features, settlement plans, banners and emblems generated from civilization history—*not* fixed biome stereotypes. A desert region may have stone or timber architecture if resource chains and culture justify it.

## Dynamic world readability

- Seasons affect actual leaf cover, snow cover, field state and river conditions. Zoomed-out representations may simplify but cannot invent a blanket snow line different from the physical state.
- Weather overlays separate forecast/knowledge confidence from actual simulation data. Animated cloud illustrations cannot expose hidden weather information if fog of war restricts it.
- Moving sun/time may alter ambient tone, but important topography and input targets stay identifiable at every play hour. The player may toggle cartographic clarity layers without changing actual daylight visibility mechanics.

## Labels and icon hierarchy

Settlement labels use adaptive density and collision prevention. Region labels are subordinate to current gameplay. Army and fortress markers get priority when hovered, threatened or selected; distant decorative location names are suppressed before crucial orders. Legends explain sovereignty versus occupation versus movement access.

## Must-have test scenes

(1) many small kingdoms on one river basin; (2) narrow mountain-pass campaign; (3) allied transit through foreign land; (4) isolated enemy valley with surviving fortress; (5) winter road closure; (6) fog-of-war intelligence disagreement; (7) far continent view with full seasonal shading. At each scene prove terrain, overlay and marker meaning survives 100% and reduced UI scale without relying exclusively on hue.
