# 26. UX, information design and player interaction

## 26.1 The three core views

**World atlas/campaign view:** navigate the full continent; inspect geography, political control, armies, settlements, strategic construction, route planning, economy, diplomatic opportunities and history. **Kingdom management view:** treasury, obligations, policies, regional directives, populations, markets, projects, military readiness and major warnings. **Tactical battle view:** direct battlefield command with clear formations, terrain, line of sight, wind, morale, command and tactical time.

These views are different perspectives on shared state. A road segment should refer to the same route whether viewed in a kingdom trade overlay, army path preview or tactical battle.

## 26.2 Campaign camera and layers

The primary map uses a **2D orthographic** presentation with pan/zoom and readable geographic overlays. Visual framing, orientation rules, relief treatment and exact camera behavior are owned by the separate visual-direction document. Elevation, rivers, roads, political rights and military control must nevertheless remain legible and geographically consistent across zoom levels, without detached or invented terrain features.

At far zoom, show continents, dominant topography, kingdom names and major armies; middle zoom shows roads, rivers, cities, major forts, cultivated belts and borders; near zoom shows terrain details, local settlements, units and environmental state. Level-of-detail does not alter underlying gameplay values.

## 26.3 Information overlays

Player-selectable map layers: physical relief/slope; rivers/watersheds/flood risk; current weather/wind; climate/season; soils/fertility; crops/food security; forests/biomass; known resources; population/migration; markets/prices/trade routes; transport capacity/blocked links; political sovereignty/claims; military secure/contested/threatened status; military passage permissions and violations; encirclement and remaining viable supply routes; local loyalty/unrest; supply reach; visibility/scouting; army routes; fortification coverage; historical events. Layers should be explanatory, legible, mutually consistent and hide unnecessary detail at broad zoom.

A selected warning opens a **cause-and-options panel**. Example: “Ironhold grain prices rose because the pass road closed in snow; west-market imports are delayed; stores cover approximately 11 days at current demand.” Link to the road, market, stored grain and potential policy. No unexplained tooltip like “Supply: −23%.”

## 26.4 Settlement interaction

Selecting a settlement shows geography/access, population breakdown, food/security, production specialization, local markets, major inventories, institutions and political rights, fortifications, projects, and current pressure points. The primary actions are policy, grants, construction, procurement, recruitment and governor direction. Ordinary farmers and artisans act autonomously. Avoid twenty unrelated building-slot screens.

## 26.5 Kingdom screen

Lead with actionable summaries: royal treasury, next-turn commitments, food coverage and regional stress, workforce/military mobilization, major routes, diplomacy/war status, at-risk vassals and active construction. Show **trend and causality** rather than isolated figures. Let players compare last season, this season and expected outcomes under uncertainty. State-level warnings should not duplicate the same local problem dozens of times.

## 26.6 Orders and planning

Army move UI previews intended route, estimated arrival interval, terrain barriers, **passage permission and potential treaty violations**, supply feasibility, detection risk and fatigue. Secure Area and Blockade previews explain which sites and routes can plausibly be held and which remain outside control. Construction UI previews terrain footprint, access requirements, materials, ownership and political constraints. Diplomacy UI previews known costs and obligations. If an action is impossible, say why and how to make it possible. Never silently reject a valid command.

## 26.7 Battle interface

Default layout prioritizes battlefield over panels. Selection, order drawing, formations, facing, target arcs, hotkeys, unit groups, control groups, hold-fire toggles, reserves and battle speed/pause must feel immediately familiar to regiment-strategy players. Provide large, readable unit status flags and optional expanded diagnostics without forcing full HUD clutter.

Line-of-sight preview, elevation/contour overlay, ground firmness/wetness, wind vector and likely firing arcs are important. Wind and mud are shown as **conditions**, not merely hidden combat debuffs. Units waiting for orders, blocked routes and collapsing cohesion must be easy to distinguish.

## 26.8 Player-facing uncertainty

Separate **confirmed observation**, **inferred estimate** and **unknown**. Forecast weather with ranges/confidence, not guaranteed future rainfall. Enemy troop count may appear approximate if observed at distance. Hidden trade stocks shouldn't show exact figures. Player may inspect precise quantities for well-administered crown inventories and units under direct command, with local errors possible where thematically justified.

## 26.9 Accessibility and control

Desktop PC is the committed primary platform; mouse/keyboard-first design. Customizable keybindings, UI scaling, color-blind-safe patterns/markers, high-contrast map overlays, legible tooltips, speed control/pause, reduced visual noise/particles and adjustable battle camera zoom are required. Convey information through symbols and text as well as color. Allow selection of regiments by banners, groups or roster when sprites overlap.

## 26.10 Tutorials and learnability

Teach through an interactive kingdom with a recognizable problem and visible causes. Early experiences: explain terrain and road access; build a bridge; inspect food reserves; raise a modest army; defend a naturally advantageous crossing; see consequences persist. Tutorials should not fabricate alternate mechanics that disappear in the real campaign. Add a searchable in-game encyclopedia matching the bible's concepts, with concise explanations and drilldowns.

## 26.11 Notification priority

**Critical interrupts:** direct engagements, threatened loss of sovereign capacity, major decisions requiring approval, famine/crisis demanding action, diplomatic ultimatum. **Important but nonblocking:** route closure, shortage, siege progress, unrest, seasonal mobilization change. **Background feed:** normal market variation, routine construction advancement, small migrations. Alerts can be grouped by cause to prevent spam. Auto-advance stops according to configurable categories.

## 26.12 Save/load and player trust

Autosave at turn boundaries and before major manual battle, with safe recovery from interruptions. A campaign can be resumed mid-project, mid-siege or during a battle where technical state support allows; if mid-battle saving is not supported, the interface must disclose it clearly and allow a reliable pre-battle save. **Committed final behavior:** manual saving in campaign and battle; restoring a battle reconstitutes environmental and regimental state coherently rather than restarting a different map. Save format includes seed, generated state, ongoing events, orders and time.

## 26.13 The physical world must be inspectable and explainable

The campaign atlas provides meaningful overlays for **elevation/contours and slope, geology/material, watersheds and drainage, current/seasonal river levels, soil moisture and ground firmness, wind direction/speed, temperature, humidity, cloud and precipitation, snow/ice, climate normals, vegetation/biomass, crop suitability, and current/future access risk**. Aggregation and faction knowledge determine which details are shown, but the underlying map is always sampled from the authoritative state.

A location inspection answers what is here now, why it is here, what tends to happen across seasons, and how it affects movement, construction, food and battles. Show origin chains, such as `upstream rain → river discharge → flooded ford → delayed army` or `thin rocky soil + winter frost → poor crop viability`. The battle interface also exposes actionable wind, surface, daylight and visibility information. A scientific debug atlas with physical field layers and provenance is required for development, even if many fine-grained fields are hidden from normal players.

---
