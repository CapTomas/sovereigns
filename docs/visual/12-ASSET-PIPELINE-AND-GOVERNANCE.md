# 12 — Art asset pipeline and ownership

**Purpose:** allow independent art/rendering agents to deliver consistent, licensed, replaceable production assets and procedural rules in a highly modular repository.

## Art artifact categories

1. **Primary visual source:** original layered/vector/raster source, editable shader graph or parametric generator project. It is tracked and recoverable when feasible.
2. **Runtime assets:** compact exports (texture atlases, sprite sheets, masks, shader materials, geometry templates) generated from approved source.
3. **Procedural definitions:** palette tokens, stable variation rules, species/architecture visual mappings, asset variant catalogs and validated data files.
4. **Review renders:** comparable reference-scene screenshots/gifs/videos, named by fixture, timestamp, camera and viewport, used as evidence—not blindly bundled into shipping assets.
5. **Provenance record:** original author/agent, prompt or method where appropriate, source license/rights, generation seed if applicable, approval/revision and dependency metadata.

## Suggested source directory boundaries

Keep authoritative documents in `docs/visual/`. When game implementation exists, approved assets should live in organized folders under `game/assets/` (or reviewed equivalent): `terrain/`, `vegetation/`, `water/`, `buildings/`, `units/`, `banners/`, `effects/`, `icons/`, `ui/`, `shaders/`. Editable external source files can live in `art/source/` or versioned external art storage if size requires; exports have reproducible process notes. Do not create two conflicting palettes or duplicate art registries in different modules.

## Asset specification record (one per family)

Include: asset ID, task ID, visual module ownership, purpose, semantic simulation inputs, planned screen/zoom ranges, orthographic camera guarantee, scale, image/export dimensions, transparency and trim/pivot convention, world-unit anchoring, variation/animation states, LOD relationships, masking/layer order, atlas/batching contract, naming convention, material/shader requirements, accessibility and color differentiation, source/provenance/license, version, reproduction instructions, comparison captures and reviewer status.

**Sample asset ID:** `UNIT_FOOT_SPEAR_SHIELD_TOP_01`; not a final required filename convention. IDs must be stable across exported variants and not encode transient color selection unnecessarily.

## Composition and rotation

- Top-down objects are authored as if observed from a camera precisely above them. Avoid perspective shadows, fake tilt, visible eyes, front-facing shields and feet that imply a side view.
- Anchor at physically meaningful centers (soldier position/footprint, bridge end alignment, building roof footprint, tree trunk). Pivots are documented and tested across rotations.
- Sprite rotations must not change world-space hitboxes, LOS, collision or unit authority. Any pseudo-normal lighting must be consistent under arbitrary rotation.
- Procedural assets have stable random stream identities; a tree cannot change species/shape between two visits because a texture atlas changed ordering.

## Visual content generation with agents

A generative model may help explore **style or source art**, but accepting output requires inspection and cleanup. Do not commit an image simply because a prompt says “strict top-down”; visually verify actual geometry. Avoid uncontrolled output drift by specifying canonical references, camera, material, target viewing size and negative constraints. Preserve reproducibility information, use review sheets, and normalize color/scale/pivots before game export. Check licensing and training/reference restrictions; do not copy screenshots or recognizable assets from reference games.

## Stage gates

**Design-approved:** source/style approved under look-dev fixture.
**Export-ready:** source/export conventions and all required variants validated.
**Integrated:** asset binds to correct simulation fields and game client without violating input/authority.
**Production-complete:** tested across view scales, weather, backgrounds, dense scenes, color-vision modes and representative hardware; provenance and reviewer sign-off recorded.

A concept painting alone is not “integrated.” A programmatically generated placeholder alone is not “production-complete.” Early, limited asset families may still reach final quality for their actual scope; never mislabel temporary visuals as delivered art.

## Quality control and versioning

Asset family changes require a semantic reason, before/after reference captures and compatibility review with palettes, gameplay affordances and LODs. Keep stable asset IDs and correct backward compatibility for persistent visual identities. Avoid introducing a new material/light system in an isolated asset without scene-level approval.
