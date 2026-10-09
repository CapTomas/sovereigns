# 06 — UI, cartography and information design

**Game authority:** §26 for behavior, faction knowledge and UI requirements, §21 for political vs military control. This file governs appearance, visual hierarchy, label treatment, icons and analytical map layers.

## UI character

A modern, rigorous field atlas for a medieval sovereign. Calm, legible, editorial and lightly material. It should feel like a command desk with impeccable maps, not a cluttered parchment fantasy interface. Use predominantly neutral ink, slate, ivory/paper surfaces and a disciplined accent system. Decoration is subtle: a fine rule, restrained crest, metal-like embossing only in signature screens if readable, never opaque ornaments over battlefield action.

**UI families:** Campaign command ribbon, settlement inspector, army roster, diplomatic screens, construction/economic controls, tactical regiment controls, world atlas overlays, notifications and chronicle. Components must remain consistent in spacing, focus, typography, backgrounds, separators, icon stroke and interaction states.

## Typography

- UI primary: a highly legible modern humanist sans for data, labels and controls; secondary display may use a restrained historical serif for kingdom titles, chronology and headings. No elaborate blackletter in dense data grids.
- Numerical information uses tabular numerals where alignment helps (prices, years, distances, soldier counts). Unit labels and certainty qualifiers are explicit.
- UI must be legible at native 1080p and reasonable scaling; do not solve lack of space by shrinking critical text below a tested comfortable minimum.
- Fonts require legal licensing. Specify families by functional role until approved font files and redistribution rights are selected; never assume a commercial font is available.

## Information hierarchy

1. Player selection and current actionable order.
2. Immediate threats, imminent battle, blocked route or failing supply.
3. Available constraints/reasons: terrain, weather, access, diplomacy, logistics.
4. Secondary numbers, historical context and optional overlays.

Use progressive disclosure: short reason first, drilldown for full causal chain. Example: `Ford closed — water depth too high` → detail: upstream rain, river stage, crossing depth, predicted recovery. Avoid showing ten raw physical metrics in every army tooltip.

## Map overlays

World atlas overlays cover terrain elevation, slope, materials, drainage, river levels, soil moisture, snow/ice, temperature, humidity, wind, precipitation, vegetation, agricultural potential, population, production, transport, sovereignty, military control, supply, visibility and uncertainty. Each overlay must:

- Sample authoritative state and label whether it is actual, estimated, historical, forecast or unknown to the current faction.
- Include a legend with real units and clear scale, where quantitative data is involved.
- Distinguish unavailable data from zero and distinguish unknown/unobserved from none.
- Retain borders and critical geography at appropriate opacity; avoid full-screen overlays that erase river crossings and selection.
- Be navigable and understandable without hue alone. Add edge patterns, hatching, symbols, opacity and labels. Never rely on red/green distinction as a sole indicator.
- Show spatial resolution/uncertainty when a coarse climate cell underlies a detailed terrain image. Do not falsely imply high precision.

### Three frequently confused map layers

**Political ownership:** claims, lawful rulers and administrative borders, generally crisp with contextual ambiguity.

**Military control:** current physical area of influence/occupation, gradient or zone boundaries aligned with access and security; contested control remains contested.

**Passage rights:** allowed, forbidden or disputed route traversal. Draw permission as a route/legal condition, not captured land.

A visualization may combine these layers only with a clearly explained legend.

## Tactical feedback

Regiments need selectable outlines, banners, formation edges, facing indicators, order ghost and concise statuses. Distinguish selecting a formation, ordering movement, holding, engaging, routing and withdrawing with a non-color-only language. Ghost move previews follow the authorized simulation command; no promise of an impossible formation in dense terrain. UI remains readable when soldiers fill the view, and group/roster selection replaces tiny hit targets.

## Notification and focus

Critical battles and political ultimatums can interrupt time according to gameplay policy; routine changes are grouped with causal summaries. Group related symptoms under one upstream cause when possible, e.g., `Northern road closure: grain imports delayed; forge charcoal shortage` rather than unrelated pop-ups.

The UI should not animate every number in a large economy; use subtle transitions for material changes and strong deliberate focus for urgent threats.

## Accessibility and interaction

- Keyboard navigation and visible focus states; scalable text/UI; motion and effect reduction; predictable gamepad handling if supported by gameplay.
- Color-vision checks and grayscale review for faction colors, military control/contested map, selected units, hazard flags and economic change indicators.
- Tooltip and icon text alternatives. Legends are understandable without specialized cartographic knowledge.
- High-contrast UI option separate from light/night world simulation; tactical visibility remains honest.
- UI element min hit areas should be validated across target monitor resolutions and scaling; do not assume pixel-perfect soldier clicking.

## Visual QA

Test: 20 settlement labels in a river corridor, 10 adjacent faction borders, winter/snow + light beige faction, red/green color vision simulation, fog-of-war uncertain control, 30 overlapping regiment banners, high wind rain, narrow road with blocked movement, large-number treasury panel and narrow UI scaling. The player must answer: where, whose, what, why and what can I do?
