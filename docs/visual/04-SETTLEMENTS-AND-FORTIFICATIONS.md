# 04 — Settlements, infrastructure and fortifications

**Authority boundary:** buildings and culture are visualized from established physical objects, materials, local development, resource access and generated history (§§7, 10, 15, 23–24). Art must not create free castles or economically productive buildings.

## Settlement hierarchy

**Rural:** hamlets, farmyards, barns, fields, local roads and riverside mills. Small footprints derived from households and land use, not equal-size decorative city icons. **Town:** concentrated buildings, workshops, marketplaces, walls where built, civic/religious structures where present. **City:** density, street structure, warehouses, port/mills/works, defensible edges and distinctive landmarks from its real economic and historical role.

Far zoom uses proportional settlement markers. Regional zoom resolves footprint, roads and land parcels. Near zoom resolves individual rooftop symbols and courtyards. A settlement's visual size broadly tracks physically represented extent and significance; do not hide a tiny poor village under a gigantic ornate icon.

## Orthographic architecture

- **Roof-plan only**: buildings are viewed from directly overhead. No side facades, three-quarter perspective walls or impossible roof projections.
- Visual form is created using plan shape, roof materials, roof edges, chimneys seen from above, courtyards, vegetation and contained roof shadowing consistent with sun.
- Wall segments appear as footprint/top-surface forms with battlement rhythm, walkway and gateways indicated through planar shapes and cast-shadow cues. Tall buildings do not grow into pseudo-isometric towers.
- Terrain-conforming fortifications follow actual contours, defensible positions, wall layouts, gate positions, siege modifications and damage states.
- Architectural variation is modular and constrained by culture, technology, materials, available resources, economy and climate; do not create arbitrary “forest kingdom timber / mountain kingdom stone” hard rules.

## Procedural coherent settlements

Streets connect actual roads, river crossings and gates. Building positions cannot overlap navigable canals, flooded channels or impassable cliffs. Plot boundaries, terraces, defensive perimeter and docks follow world geography. Generate rooftop/style variants deterministically using persistent building IDs and authored shape grammars, with reviewable fallback if unusual footprints occur.

## Construction and aging

Building work visibly progresses according to actual construction state, with scaffolding and partially built shapes if represented. Burnt, damaged, flooded and abandoned states emerge from persistent state; don't fake damage at random each camera entry. Changed bridges, raised walls, cleared forests and new roads remain consistent between campaign and battle. Reuse a small controlled material language (timber, thatch, tile, stone) rather than many disconnected texture packs.

## Distinctive landmarks

Castles, bridges, market squares, ports and major civic buildings should be identifiable through plan silhouettes and spatial role rather than gratuitous size. Gates/bridges remain selectable at high density; building selection highlights must not depend on exact sprite pixel hit-testing.

## Quality gates

Test villages to cities at four zoom levels; three different generated cultures without kitsch; hilltop fortress with real relief and defensible gate; siege damage and repair; flooded riverside settlement; snowy roofs and summer dry town. Confirm structure footprints and route access match authoritative simulation for all cases.
