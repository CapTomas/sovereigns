# Visual agents — working contract

1. Read repository `AGENTS.md` and `docs/visual/README.md`. Load *only* the visual module(s) corresponding to the task plus the relevant gameplay chapter and upstream/downstream contracts. Do not load the entire design bible and visual folder by default.
2. `docs/spec/` controls **what is physically or mechanically true**; `docs/visual/` controls **how the true state looks**. If appearance would alter gameplay visibility, selection, camera semantics or spatial interpretation, review the gameplay spec first and escalate a conflict rather than invent a rule.
3. A generated image, art reference or screenshot is *visual evidence*, not a delivered asset. To mark an asset done it must match its specified angle, materials, required views, dimensions/scale, transparency/export conventions, animation/LOD variants, accessibility, license/provenance, and actual game integration. Never ship placeholder art as finished work.
4. True overhead means no visible faces/facades due to camera tilt, no angled bodies and no perspective vanishing point. Heads, shields, helmets, roofs, horse backs and weapon tips must make sense viewed vertically from above.
5. Avoid decorative noise and over-detailed assets that compromise tactical scale. Test visuals as a full scene with dense regiments, not in isolated hero renders.
6. World and battle renderer must sample authoritative terrain/vegetation/weather/buildings; artwork cannot inject inconsistent data. Never create independent random weather, water levels or field locations in the client.
7. Any visual algorithm requiring seeds uses stable world-location/asset identities. Camera movement, zoom, reload and battle entry must not regenerate an inconsistent visual landscape.
8. Review against `13-LOOKDEV-AND-VISUAL-QA.md` and representative reference scenes. Record the test device, viewport, performance, screenshots, zoom levels, color-vision and small-text checks. Do not claim measured results you haven't obtained.
9. Do not add unlicensed fonts, textures, visual assets or copied proprietary artwork. Keep an explicit license/provenance record.
10. Finish with an asset handoff: task ID, files changed, original sources, export recipe, affected layers/LODs, reference renders, acceptance matrix, performance evidence, review status and any required ADR/spec reconciliation.
