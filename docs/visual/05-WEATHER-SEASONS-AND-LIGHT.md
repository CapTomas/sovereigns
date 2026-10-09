# 05 — Weather, seasons and lighting

**Game authority:** §§3–6 and §22. **Owner:** visual response to authoritative sunlight, temperature, precipitation, water, wind, snow and seasonal phenology. Do not create a second cosmetic weather model.

## Sun, daylight and shadow

Sun position comes from latitude, date, time and the world solar model. Daylight color/intensity may be artistically compressed to preserve information, but visual sunrise, sunset and shadow direction cannot systematically contradict true solar state. Provide a **cartographic clarity mode**: improve rendering contrast and readability while mechanics continue to use actual sunlight/visibility. The setting must never secretly grant battle visibility or scouting knowledge.

Use two clearly different notions:
- **World-lit presentation:** directional terrain/structure and cloud shadows reflect world sun state when visible.
- **Analytical relief overlay:** optional fixed-direction hillshade that reveals topography independent of current sunlight, explicitly identified as a map aid rather than current-world illumination.

Night battle is dim and different in atmosphere, not uniformly black. Unit outlines, commands and critical crossings remain usable. Unseen enemies remain hidden by *simulation-based* visibility; interface contrast does not reveal them.

## Atmosphere and cloud systems

Cloud coverage, motion and shadow respond to atmospheric moisture, wind field, condensation and region-wide precipitation state. At continental scale use high-level coverage shapes; at tactical scale use coherent local shadow, shade and light scattering cues. Clouds cannot independently spawn rain in an incompatible position. High winds should be perceptible through wind-aligned rain/vegetation and readable wind indicators when they affect arrows.

Do not display giant opaque cloud sprites that hide armies or entire political regions. The player can reduce cloud opacity while retaining cloud/weather information through readable indicators.

## Rain and wetness

Rain is spatial and temporal, with local density/intensity derived from simulation; don't cover the entire battlefield because one weather cell is rainy. Light drizzle, sustained rain and downpour differ primarily in field response and relatively restrained screen effects. Ground wetness, puddles, soil darkening, pooling and later runoff update in their causal cadence—not on the first rendered raindrop. Strong storm conditions may produce visibility loss according to simulated limits, but combat commands remain readable through optional visual intensity settings.

Wet roads, soil and mud reflect material, drainage, exposure, precipitation history and military traffic. Deep mud is a terrain state not a uniform brown decal. Flooding follows elevation and water level. Crossing visuals communicate passability only when querying actual state.

## Snow, frost, ice and seasonal change

- Accumulated snow appears where there is measurable snow cover and respects temperature, exposure, wind redistribution and melting processes as modeled. No simple “winter means white everywhere” world tint.
- Mountain ridges and sheltered valleys can retain snow differently. An early autumn cold front affects highlands before the nearby warm lowlands where supported by the model.
- Frozen ground and ice look distinct from snow; thin ice has an optional explicit hazard cue.
- Forest leaf color/coverage depends on vegetation type, phenology, local climate and weather history. Crops change visually with actual growth/maturity/harvest state.
- Rain, sun, mud and snow transitions blend meaningfully in time and space. The client can interpolate *visual presentation* but not interpolate across authoritative state changes in a way that conveys false hazards.

## Wind and terrain

Wind direction may be visible through water ripples, bending grass, canopy sway, dust, snow or precipitation angle if those surfaces exist. These cues are secondary, with an explicit vector/wind rose in tactical UI when accuracy matters. A forest canopy may attenuate wind visually relative to exposed ridge without modifying the authoritative local wind computation.

## Fire, smoke, destruction

Smoke follows simulated fire state, burnable material, wind and fire progression. Do not randomly ignite trees or buildings for atmosphere. A smoky region can affect combat visibility only if the simulation models that smoke obstruction. Cosmetic smoke particle density may be capped for performance without changing mechanical concealment or hazard indicators.

## Visual options

Cloud opacity, rain density, snow particle detail, screen shake, flash intensity, dark-night clarity, ambient animated foliage and reduced motion are adjustable. These control *rendering only*. Hazard text/icons and mechanical visibility remain independent of accessibility preferences.

## Validation fixtures

A. Clear midday summer valley; B. same location at dusk and moonless night; C. moving weather front across mountain rain shadow; D. rainfall-fed flooded ford; E. partial thaw with patchy snow and river rise; F. autumn deciduous forest next to evergreen stand; G. smoke blown away from burning fortification. Capture field values and render side by side at campaign and tactical scale; verify no independent weather seed or altered world clocks.
