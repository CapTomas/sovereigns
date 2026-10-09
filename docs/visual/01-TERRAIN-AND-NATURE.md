# 01 — Terrain and nature

**Read with:** `SOVEREIGNS_VISUAL_DIRECTION.md`. **Game authority:** §§3–6, 11, 15, 22. **Owns:** visual treatment of physical land and vegetation; never owns the physical fields themselves.

## Terrain as a continuous physical landscape

World appearance follows continuous elevation, geology, land cover, soil, exposure and active surface conditions. Visual layers must preserve cross-chunk continuity and same-coordinate appearance. Use seamless transitions in material and vegetation coverage rather than a tile palette with abrupt equal-width borders. Detail density is chosen by camera scale and importance, with broad landform readability always taking priority.

The three reading orders are: **landform** (mountain/valley/watershed), then **surface** (rock/soil/forest/field/water), then **human use** (roads/terraces/settlements/defenses). From above, the viewer should recognize the topographic skeleton before noticing stones or bushes.

### Elevation and slopes

- Large-scale terrain shading reveals ridge lines, basins, passes, gullies and floodplains. Relief shading may use a carefully separated clarity light, but when it indicates physical sun direction it must follow world solar position. Distinguish cartographic hillshade from live shadows in overlays/legend.
- Add local ground form in the tactical view from constrained detailed terrain; it cannot move a pass or place a ridge across the known river.
- Optional contour overlay is a deliberately requested layer, not a permanent jungle of lines.
- Steep ground requires visible transitions: changes in exposed material, vegetation density, runoff/erosion features, and optional hatching or contour edge enhancement. No fake stepped cliffs when the source data indicates a gentle slope.
- Hills remain legible under rain, night, snow and political overlays. Test on low-contrast rocky valleys and very gentle agricultural terrain.

### Material and soil

- Model a small, controlled visual material vocabulary: bare rock, scree, exposed soil, cultivated soil, grass/herb cover, wet soil, mud, sand, pebbles, alluvium, saturated marsh, snow/ice.
- Source each from meaningful physical attributes. Soil wetness affects darkening/sheen gradually; waterlogged terrain changes with soil saturation and standing water; bare ground corresponds to cover/exposure.
- Materials can blend smoothly, but gameplay-significant transitions such as riverbank edge, steep slope, road embankment and deep bog should be readable on demand.
- Use a restrained grain scale that decreases at zoom-out; a world-wide uniform brush-texture overlay is forbidden because it falsely implies all ground has identical structure.

### Rivers, lakes, sea and wetlands

- Channels are geographical paths with banks and local width tied to authoritative cross sections and current stage. Tributaries join visibly, water proceeds downhill or through explicitly modeled outlets, and lakes respect their basin boundaries.
- Render depth/turbidity cautiously; don't suggest shallow ford from bright color alone when the simulation marks it impassable. Use inspected crossing symbols and player-accessible water-depth legend.
- Moving ripples, foam and flow direction are subtle and synchronized to actual water velocity. Decorative waves do not reverse a river's visible current.
- Sea/coast shapes follow coastline data and tides where modeled. Shore wetlands, deltas, sandbars and exposed flats must reflect real shoreline features.
- Floodwater grows into low ground according to world state and gradually recedes. Seasonal and tactical water surfaces cannot conflict at the same timestamp.
- Frozen water must show meaningful ice state, not an instantaneous binary blue/white swap; unsafe thin ice has distinctly communicated risk.

### Vegetation, forests and agricultural land

- Forests form continuous regional masses with species- and biome-specific structure. Edges are irregular and habitat-driven, not random circles. At distant zoom a forest canopy mass is sufficient; at close zoom individual crowns and gaps appear without changing underlying coverage.
- Procedural foliage positions derive from persistent vegetative coverage and stable location seeds, not random placement each time the camera moves.
- Dense canopy obscures ground visually only as far as tactical readability permits; use temporary canopy transparency, units/selection outlines or contextual cutaways to reveal selected formations. Do not erase actual cover or change simulated line of sight.
- Farms follow field parcels, roads, slopes, rivers and human history. Patchworks can look varied but must respect actual cultivated land, crop maturity, crop rotation, hedges, terraces and ditches.
- Season effects are species- and climate-specific, not one universal autumn-orange tint. Evergreen, deciduous, high-altitude and arid vegetation respond differently.
- Forest clearing, cultivated expansion, erosion and burning persist in appearance across campaign and subsequent battles.

## Procedural graphic variety rules

Variation is seeded, sparse and layered: a distinct silhouette for significant rocks/trees, controlled variations in canopy size and shade, a few consistent ground motifs. Repetition must be hard to notice at representative zoom. Determinism alone is insufficient if patterns visibly repeat every chunk.

## Must not happen

- A river appears to flow uphill or its ford changes position between modes.
- A mountain is shown as a standalone isometric icon with a different footprint from real height data.
- The battlefield spawns a new forest that campaign data did not contain.
- An entire field becomes muddy instantly because a rain animation started.
- Decorative rocks block troops without corresponding physical obstacles, or true rocks are invisible while still blocking them.
- Water, forests, shadows or vegetation make selected armies impossible to read.

## Acceptance examples

Inspect river valley, high Alps-like ridge, river delta, dry plateau, temperate woodland, winter mountain, floodplain and harvested fields. For each, compare gameplay coordinate queries with expected water edges, ground class, vegetation footprint, landform, road/crossing. Switch campaign ↔ battle and reload without shape changes. Observe a season/weather transition with before/after field queries and matching graphic changes.
