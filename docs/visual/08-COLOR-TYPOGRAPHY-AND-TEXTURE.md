# 08 — Color, typography, texture and iconography

**Direction:** serious, natural, composed. Terrain is organically varied but never too loud; military control/selection is highly legible; UI uses disciplined cartographic information styling.

## Palette principles

Natural land colors are produced through terrain/climate/material rendering, *not* hardcoded to a biome label. Named tokens in `tokens/palette.json` are for the interface and baseline look-dev tuning, not a physical simulation model. Colors should remain coherent across wet/dry/snow/night adjustments and never flatten terrain into uniform biome stripes.

- **Terrain:** earthy green, olive, stone gray, clay, sandy ochre, desaturated conifer/deciduous families; moderate value differences to reveal landforms.
- **Water:** dark muted blue-green with local turbidity, shallow depth variation and subtle highlight.
- **Atmosphere:** cool/desaturated shadows and warm light only when lighting/weather warrants it.
- **Faction distinction:** controlled banner colors with secondary pattern/crest/silhouette. Faction hues cannot make snow/land unreadable, and must pass color-vision checks in adjacency.
- **UI:** warm light surfaces and deep neutral ink; selected/critical states have intentional contrast. Red/destructive, gold/warning and green/positive are semantic accents, never the only signal.

## Current starter tokens

See machine-readable values in `tokens/palette.json`. These are adopted **starting art tokens** but individual values require scene/contrast testing and documented adjustment before final shipped color grading. Avoid random hex values in individual art files; name semantic roles and map them to contextual rendering.

## Value and texture hierarchy

At distant zoom, value separation describes geography and territory; microtextures must dissolve. At middle zoom, fields, forest masses, roads and water edges emerge. At close zoom, individual organic strokes and material motifs enrich the image. Never use a uniform heavy texture overlay to disguise repetition. World roughness is simulated variation, not just a noise layer.

Selectively use texture families: soft painted land transitions, fine terrain grain, mostly clean unit silhouettes and vector-sharp UI. Avoid simultaneously combining photographic grass, pixel sprites, parchment borders and glossy modern UI.

## Linework and outlines

Terrain boundaries may use subtle edge enhancement and hand-illustrated contours where requested, not black outlines around each tree or soldier. Operational lines (frontage, orders, military control, routes) use crisp display-space strokes with stable screen-width behavior. Distinguish an outline that is a UI operation from one that indicates a real physical edge.

## Typography roles

- Main data and controls: modern readable sans, medium weight; minimize ornamental caps and ensure tabular numerals.
- World place names and special historic headings: optional restrained serif or distinctive typographic accent, carefully limited.
- Tiny labels need increased contrast/background cues rather than a fragile hairline font.
- Specific font files/licenses require a later approved asset record. Never include unauthorized fonts in the repository.

## Symbols, heraldry and labels

Use a common icon grammar with consistent stroke and visual size. Kingdom heraldry is procedurally generated from a curated vocabulary and must remain distinguishable at low resolution; use tincture-like separation as an inspiration, not a rigid historic heraldry simulator. Avoid copyrighted motifs copied from an existing franchise. Selection, movement, supply, warning, rout and uncertain intelligence should work in monochrome and color-vision modes.

## Anti-examples

Neon grass, saturated red army outlines filling every pixel, decorative gold trim on all panels, giant national emblems obscuring settlements, paper texture covering all maps equally, noisy stone shader hiding roads, climate biome color stripes, fancy blackletter data labels, an all-brown low-contrast winter battlefield and seasonal palette swaps that do not follow simulation state.

## Acceptance

Provide scene comparisons in bright/dim lighting, rain, snow, autumn forests, desert/cold highland transition, large multi-faction army contact, political/military-control overlay and grayscale/color-vision simulation. Verify UI foreground/background contrast by actual measurements on rendered output; token colors alone do not prove final contrast.
