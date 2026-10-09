# Sovereigns — Visual direction charter

**Status:** Adopted design. **Visual concept:** naturalistic illustrated world × restrained modern stylization × dense, readable overhead armies. **Medium:** strict orthographic 2D. **Genre mood:** serious medieval, geographically credible, human-scale and atmospheric, never toy-like.

## 1. The image we want players to remember

A sprawling fictional continent is visible as a plausible shaded-relief landscape. Mountain ridges gather winter snow, rivers carve fertile valleys, forests form continuous ecological regions, towns develop beside access routes, and roads follow the land. Nothing resembles a set of disconnected biome stickers. From closer above, the same river, bridges, ridgelines and tree cover frame a battle between immense ordered formations. Soldiers are legible by mass and silhouette rather than facial detail. The player can read the tactical problem at a glance and still enjoy a convincing, inhabited world.

### Three visual pillars

**1 — Natural land.** Relief, hydrology, ecology, seasons and terrain transitions are the protagonists. Form comes from authoritative physical data, rendered with tasteful illustration rather than raw map debug colors or satellite-like photographic noise.

**2 — Disciplined abstraction.** Buildings, formations, symbols and UI are simplified purposefully. This makes massive scale believable and legible, not miniature/toy-like.

**3 — Cartographic clarity.** World-scale labels, tactical orders, military control, rivers, fords, material state and environmental hazards should be readable without requiring constant interface switching. Data overlays remain precise and honest.

## 2. Perspective and geometry

- Camera faces straight down the world vertical axis: **90° overhead**. No horizon, isometric skew, vertical facade photography, perspective foreshortening, projected 3D soldier sprites or 45° views.
- This applies to both campaign and tactical gameplay. One coherent map coordinate basis; zoom changes represented detail, not visual world orientation or shape.
- Elevation is conveyed via shading, locally varied material/vegetation, lighting, contours when needed, terrain geometry cues and contextual slope overlays. Distorted mountain icons or oversized 3D ridges cannot relocate a river or hide a pass.
- Buildings are rooftop/plan silhouettes. Wall footprints, gates, roads and towers must agree with their actual world footprints.
- Soldier silhouettes favor helmets/caps, shield shapes, top-visible weapons and team details. **Do not rely on animated legs, detailed faces or fully illustrated arms** to sell motion from overhead.

## 3. The visual hybrid

| Ingredient | Use | Do not import |
|---|---|---|
| Naturalistic illustrated strategy terrain | Spatially coherent relief, foliage, paths, soil, rivers; rich but controlled detail | Hyperreal photogrammetry, dense noisy textures, photoreal human models |
| Elegant minimalist strategy art | Economical silhouettes, sparse outlines, visual hierarchy, readable armies | Toy dioramas, colorful cartoon blobs, floating island perspective |
| High-density top-down army visual philosophy | Tight regimental formations, silhouette repetition, GPU-friendly variation | Pixel-art resolution as a compulsory style; individually detailed miniature sprites |

The atmosphere is grounded, grand and quiet between battles. It may become dramatic during storms and combat, but effects should remain subordinate to terrain and tactical information.

## 4. Visual hierarchy

1. **Direct player actions and dangerous events**: selected regiments, projected moves, contact, contested crossings, urgent environmental hazards.
2. **World geometry and tactical affordances**: slopes, vegetation, water, roads, bridges, walls, approach routes and deployment zones.
3. **Armies, settlements and political information**: formations and banners, ownership, control, accessibility and labels.
4. **Ambient atmosphere**: moving shadows, clouds, rain, wildlife, cosmetic particles, vegetation movement.
5. **Decorative detail**: rocks, individual shrubs, roof details and texture grain that must recede at scale.

Layer ordering must explicitly guard these priorities. For example, a rain effect must never hide an order destination or an enemy regiment's selected outline.

## 5. Color and texture language

- Land uses low-to-moderate saturation, nuanced climate/material colors and coherent region-to-region transitions. No harsh biome-boundary stripes.
- Forests are differentiated by coverage, canopy form and subtle tones, not neon greens. Stone, bare rock, soil and snow remain geographically plausible.
- Water is visibly water, not uniformly saturated cyan. Depth, sediment, shading, surface disturbance and seasonal state affect its rendering.
- Military colors are stronger than terrain and deliberately limited. A faction's identity combines colors **and** shape/pattern/banner motifs.
- Lines are selective and thin at high density. Heavy black cartoon outlines are disallowed as a default asset language.
- Microtexture should enrich broad surfaces at suitable zoom, never fill the whole map with equally strong noise.
- UI is restrained cartographic information design, with subtle medieval material/ornament cues, not a textured parchment sheet covering play.

The starter semantic palette resides in `tokens/palette.json`. Token values are a production look-dev baseline, subject to reviewed contrast and season tests, **not** independent world-state variables or calibrated physical surface colors.

## 6. Scale and army philosophy

At far zoom a regiment is a military symbol with orientation and strength. At tactical zoom it resolves into a readable block with actual frontage, depth, shields, weapon profile and banner. At close tactical zoom numerous inexpensive soldier depictions provide detail. These are all views of the same simulation formation; visual count/spacing should track represented manpower reasonably, with clearly stated compression at low zoom.

Large armies should feel formidable because formations move as organized masses and interact with terrain, not because the client renders high-poly characters or extensive individual animation cycles. Details are most visible at boundaries: front ranks, flanks, missiles, flags, casualties and disrupted groups.

## 7. Shared environmental reality

No cosmetic art layer may imply a state contradicted by the physical atlas. Rain layers follow authoritative precipitating areas; snow rendering follows snow and frost; wet fields depend on surface water/soil; forest canopy corresponds to persistent vegetation distribution; sun and shadows follow world time. A river's apparent width, banks and crossing cues must agree with its current discharge/stage at the relevant scale. Cosmetic adjustments may improve readability only without deceiving the player about consequential ground conditions.

## 8. Art deliverables and approval

Before final approval of a visual sub-system, produce: (a) a scene with terrain and armies together, (b) normal and selected states, (c) at least two seasons and weather conditions where relevant, (d) campaign-versus-battle continuity view, (e) visible evidence at near/far zoom, (f) a legibility and accessibility assessment, (g) integration with live or deterministic simulation data, (h) asset provenance and cost measurements where applicable.

An attractive isolated screenshot is insufficient. The accepted unit of quality is **the operational scene with actual UI and authoritative world state**.

## 9. Explicit boundaries

- **Not a pixel-art mandate.** Pixel-perfect scaling and low-res tiles are not required. Hand-authored painterly sprites, vector-like cut shapes and procedural shading may coexist when consistent.
- **Not watercolor as a mandatory filter.** Avoid painting an identical wash over all surfaces or paper-cutout white borders around every unit.
- **Not a 3D/2.5D game.** Sprite rotations, flat normal maps, local 2D lighting or screen-space shading are presentation techniques only and must not reintroduce angled projection.
- **Not a city builder's close camera.** The main tactical experience is readable from real army-command distance.
- **Not fantasy spectacle.** No magic glows, huge exaggerated explosions or fantasy architecture unless separately authorized by gameplay/specs.

## 10. Decision log

| ID | Decision | Status |
|---|---|---|
| VIS-001 | Illustrated naturalism + restrained stylization + mass-soldier scalability | Adopted |
| VIS-002 | True 90° overhead 2D at strategy and tactical scale | Adopted |
| VIS-003 | Terrain/season/weather render from authoritative simulation | Adopted |
| VIS-004 | No mandatory pixel-art, paper-cutout, watercolor or isometric treatment | Adopted |
| VIS-005 | Soldier graphics prioritize formation silhouettes and low animation burden | Adopted |
| VIS-006 | Cartographic UI and non-color tactical differentiation | Adopted |
| VIS-007 | Final asset acceptance requires integrated zoom/weather/density/accessibility tests | Adopted |

Style changes require reference scene comparison and explicit reviewed decision; do not silently alter this file through unrelated feature tasks.
