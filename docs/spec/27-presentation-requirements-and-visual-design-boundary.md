# 27. Presentation requirements and visual design boundary

## 27.1 Committed presentation medium

*Sovereigns* is a **fully 2D orthographic** strategy game. Both the strategic campaign and tactical battle renderer must communicate the same authoritative geography, geography-based movement, army state, weather and structural changes. No gameplay system may require perspective imagery, simulated 3D assets or a particular illustration aesthetic to function.

## 27.2 Visual direction is a separate authoritative companion

The **exact visual language is not specified in this game-design bible**. Camera framing choices within the 2D orthographic commitment, art style, colors, textures, sprite design, character abstractions, animation techniques, environmental effects, UI visual identity, audio style and asset-production rules are to be settled in `SOVEREIGNS_VISUAL_DIRECTION.md`. Art agents must not extrapolate a final style from illustrative examples in this bible.

The companion may define appearance and readability, but may not change simulated mechanics, world continuity, army control rules, actual cover or weather effects. If a visual decision affects gameplay or input usability, it must also be reconciled with the relevant gameplay chapter here.

## 27.3 Required information regardless of aesthetic

The game must visually communicate relative terrain height and passability, waterways and crossings, roads and structures, political and military-control overlays, weather affecting play, formation facing, order feedback, uncertainty and tactical visibility. This is an **information contract**, not a palette, camera-angle or sprite-style prescription. Optional visual effects cannot obscure essential orders or contradict physical world state. Important information needs accessible non-color alternatives.

## 27.4 What is outside this bible

Do not establish a canonical palette, watercolor/paper texture, cutout treatment, icon family, font, soldier anatomy, number of sprite orientations, frame-by-frame animation policy, facade angle, lighting art style, particle vocabulary or audio score here. These are visual-direction decisions for the separate dedicated document. The absence of those decisions is intentional and does not permit an implementation agent to silently choose permanent production art.

---
