# Sovereigns — canonical visual reference scenes

These briefs tell agents what **identical scenarios** must look good and be readable in. They do not contain generated assets, pretend simulation results or implementation screenshots. Once fixture generation exists, bind each to a reproducible world seed and scenario/snapshot ID. Use true overhead camera, same viewport/zoom baselines and canonical visual tokens. Comparison renders must share the same physical scene where so specified.

## VIS-F01 — The inhabited river valley

Medium green valley with winding navigable river, tributaries, ford, one road bridge, cultivated fields, varied forest cover, a river town and distant exposed ridgeline. Clear late spring midday. Deliver: continental/regional/campaign/tactical views of consistent landmarks. **Question:** Is the geography plausible and equally legible at different scales?

## VIS-F02 — The defended bridge

Use the same coordinates and current state as a real campaign engagement from F01. Two armies approach a bridge from their actual directions; defending spearmen control one bank, archers on slightly higher ground, cavalry behind. Deliver overview, selection, movement ghost, elevation/water overlay. **Question:** Can a player understand frontage, routes, obstacles and the tactical plan in three seconds?

## VIS-F03 — Heavy autumn rain

Same valley as F01 after meaningful antecedent rain; soil wetness varies, river stage higher, ford unusable where simulation says so, weather front moving. Deliver before/during/after and debug state references. **Question:** Are changing water and ground conditions spatially and temporally believable without obscuring orders?

## VIS-F04 — Snow and thaw

Mountain pass, evergreen forest, low snowline, partly frozen creek, spring melt and water-stage changes. Display winter, thaw and late spring frames. **Question:** Do snow and vegetation follow physical elevation/local climate rather than one biome color filter?

## VIS-F05 — Dense forest contact

Infantry formations enter mixed woodland and fight near a clearing; camera centered on selected troops. Deliver full canopy, selected troops, obscured enemies and tactical visibility overlays. **Question:** Does forest visibly exist without concealing selected friendly troops or leaking hidden enemies?

## VIS-F06 — Mass regiment stress

Flat/sloped mixed battlefield with two large armies, infantry lines, archers and cavalry, many simultaneous regimental orders and casualties. Target tens of thousands of drawn soldier marks for stress benchmark. Deliver overview, mid/near zoom, selected/rostered units and p95/p99 frame-time evidence. **Question:** Do 30k marks still look like armies and allow orders rather than a colored snowstorm?

## VIS-F07 — Mountain fortress and siege

Actual hilltop fortification controlling a narrow road, walls on contours, water source, siege camp and roads. Deliver campaign and tactical views, wall damage, bridge destruction and subsequent persistence. **Question:** Can architecture look grand from overhead without becoming isometric? Is siege geometry faithful to data?

## VIS-F08 — Political control and allied passage

Three neighbors sharing river/mountain border. One army lawfully marches through an ally; another occupies contested enemy corridor around a defending fortress. Deliver political, military-control, passage-rights and supply overlays. **Question:** Can a player tell lawful sovereignty, occupation, transit and encirclement apart?

## VIS-F09 — Dark battle at dusk

Same tactical river valley as F02 at sunset/night with light fog. Deliver natural view, clarity mode and high contrast UI. **Question:** Does darkness change mood without losing ability to command or revealing units unknown to faction?

## VIS-F10 — A functioning medieval economy in landscape

Fertile floodplain village with farms, granary, mill and road; mining settlement in adjacent upland with logging and smelting. Deliver dry summer, harvest, wet autumn and wartime infrastructure-damage scenes. **Question:** Does the landscape show production, cultural variation and historical changes without unrelated decorative art?

## Capture metadata for every fixture

Fixture ID, spec version, scenario seed, lat/world coordinates, simulation tick/date/time, camera center/zoom, target screen dimensions and UI scale, weather fields, units/population context if relevant, faction knowledge, graphic settings, engine/runtime versions, capture hash and reviewer notes. Recording placeholders in metadata is not evidence that the fixture ran.
