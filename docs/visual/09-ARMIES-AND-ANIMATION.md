# 09 — Armies, regiments, soldiers and animation

**Game authority:** §§19–24, 26. **Direction:** masses of disciplined historical-feeling troops shown from absolute overhead; unit types recognizable via silhouettes, formations and equipment rather than rich individual character art.

## Army visual philosophy

The *regiment* is the readable unit of warfare. Soldiers supply visual density and identity. Equipment, count, facing, depth, front and formation cohesion emerge from authoritative combat data. Don't sell the battle primarily with heroic single-character animation. Army visual quality should improve with 10,000–30,000 visible markers, not collapse under their density.

## 90° overhead soldier grammar

- Top of head/helmet, upper torso/shoulder footprint and equipment are the main visible shapes. No face-on chest portrait, no three-quarter bodies, no visible standing legs at standard tactical zoom, no fully articulated arm pose sheet as a requirement.
- Distinctive weapon tip/reach visible *in the plane* where it makes sense (spear, polearm, bow, shield edge, sword silhouette). Shields remain appropriately small, positioned alongside the body rather than as giant front-facing heraldic signs.
- Movement direction is inferred from helmet/body/equipment asymmetry, the formation-facing arrow and coherent group motion. A completely radial identical dot is insufficient for units needing orientation.
- Foot soldiers primarily require controlled rotation/variation and a few inexpensive meaningful states. Limbs are optional small marks, **not** a source of animation dependency or reason to change camera perspective.
- Horses are roof-view/back shapes with recognizable elongated body, head/neck direction and tack; no galloping side-view sprite. Cavalry group motion and spacing create the effect of speed.
- Archers show visible bow/profile and shooting direction; projectile launch/flight belongs to tactical simulation events. Longbowmen must not look like spearmen just because team color differs.
- Heavy/light unit differences stem from helmet/armor density, shield footprint, weapon silhouette, formation spacing and speed cues. Do not overuse faction color as the only identifier.

## Visual classes

Infantry: spear/pike, shielded spear, sword, polearm, militia/light infantry. Ranged: bow, crossbow, thrown weapons where gameplay defines them. Mounted: light and heavy cavalry. Support: commanders, baggage and siege teams. Siege assets use accurate top-plan footprints and movement ranges. The actual available classes depend on simulation equipment, culture and historical progression—not an art-only technology tree.

## Formation-scale marks

At battle overview, regimental footprint and frontage dominate. Use clean banner/crest symbols, line depth, shields at perimeter, weapon points at the front and morale/cohesion indicators outside the dense soldier mass. The selected regiment gets an outline that respects its true front/side contour, not a giant artificial solid rectangle hiding the ground. Routing/destruction changes the occupied visual area, gaps and order of movement.

## Animation and motion

**Primary:** coordinated translation, turning, spacing adjustment, charge acceleration, contact deformation, orderly withdrawal and rout; all driven by tactical snapshots/interpolation. **Secondary:** tiny shield/weapon movement, banner flutter, limited local fighting variation, fall/death markers where simulation reports casualties. **Tertiary:** dust and footsteps (only if ground/weather supports them).

Most soldiers should not require walking leg cycles or eight hand-painted directional frames. Silhouettes must look correct at arbitrary rotations (texture/normal conventions and weapon orientation tested). Avoid cartoony squash/stretch, exaggerated bouncing, giant impact flashes and pseudo-side-view limb movement.

## Faction identity

Different cultures evolve distinct military traditions and equipment but graphics come from curated composable equipment families, materials, colors, shields, banners and unit behavior. One kingdom's soldiers cannot all be individualized bespoke artwork. The same pike silhouette can support many factions with heraldry and material variation, provided cultural differentiation is still visible at group scale.

## Facing, morale and visibility

Facing remains visible under crowding. Morale/panic communicates through coherent breakup, movement and status marks, not cartoon emoji. Hidden formations remain hidden from the player; smoke and darkness do not give the renderer authority to reveal them. All relevant status is also available through roster UI for accessibility.

## Test matrix

1. One regiment facing eight headings; detect its front from near and far zoom.
2. Infantry/cavalry/archer simultaneously in same faction colors; identify each by silhouette.
3. 30,000 visible simplified soldiers, several overlapping formations, with UI selection and unit roster usable; profile on agreed target hardware.
4. Narrow bridge compression, forest movement, uphill charge, mud slowdown, orderly regroup, flank attack and mass rout.
5. Night/rain/snow/forest with selected regiments visible but undiscovered enemy formations still hidden.
6. Unit numbers decrease mid-battle and visible formation responds without sprite teleportation or total disappearance.

Production acceptance depends on integration with combat data and reproducible load/performance evidence, not just pretty sprite sheets.
